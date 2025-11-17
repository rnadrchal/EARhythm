using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Permissions;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EnvironmentalSequencer.Services;

public record DeviceInfo(Guid Id, string Name, IPEndPoint EndPoint, string[] Capabilities, DateTime LastSeenUtc);
public record SensorReading(string Key, double? Value, string Unit);

public class SensorService : IAsyncDisposable
{
    private readonly IConfigurationRoot _config;
    private readonly int _sensorPort;
    private readonly int _discoveryIntervalSeconds;
    private readonly UdpClient _udp;
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<Guid, DeviceInfo> _devices = new();
    private readonly ConcurrentDictionary<Guid, List<SensorReading>> _readings = new();
    private readonly TimeSpan _broadcastInterval;


    public SensorService(IConfigurationRoot config)
    {
        _config = config;
        _sensorPort = int.Parse(_config.GetSection("Sensor")["Port"] ?? "4210");
        _discoveryIntervalSeconds = int.Parse(_config.GetSection("Sensor")["DiscoveryIntervalSeconds"] ?? "60");
        _broadcastInterval = TimeSpan.FromSeconds(_discoveryIntervalSeconds);
        _udp = new UdpClient(new IPEndPoint(IPAddress.Parse("192.168.99.50"), _sensorPort));
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.EnableBroadcast = true;
        _ = ReceiveLoop(_cts.Token);
        _ = BroadcastLoop(_cts.Token);
    }

    public DeviceInfo[] GetKnownDevices() => _devices.Values.ToArray();
    public List<SensorReading>? GetLatestReadings(Guid deviceId)
    {
        if (_readings.TryGetValue(deviceId, out var readings))
        {
            return readings;
        }
        return null;
    }

    public EventHandler DeviceAdded
    {
        get;
        set;
    }

    private async Task BroadcastLoop(CancellationToken ct)
    {
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, _sensorPort);
        while (!ct.IsCancellationRequested)
        {
            await DiscoverNodes(broadcastEndpoint);
            await Task.Delay(_broadcastInterval, ct).ContinueWith(_ => { });
        }
    }

    private async Task DiscoverNodes(IPEndPoint broadcastEndpoint)
    {
        var msg = new
        {
            type = "discovery_request",
            version = 1,
            timestampUtc = DateTime.UtcNow
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg);
        try
        {
            await _udp.SendAsync(bytes, bytes.Length, broadcastEndpoint);
        }
        catch (Exception ex)
        {
            // log error
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            UdpReceiveResult res;
            try
            {
                res = await _udp.ReceiveAsync(ct);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception) { continue; }

            Task.Run(() => ProcessMessage(res));
        }
    }

    private void ProcessMessage(UdpReceiveResult res)
    {
        try
        {
            var json = Encoding.UTF8.GetString(res.Buffer);
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("type", out var t)) return;
            var type = t.GetString();
            if (type == "discovery_response")
            {
                var id = Guid.Parse(doc.RootElement.GetProperty("id").GetString());
                var name = doc.RootElement.GetProperty("name").GetString();
                var caps = doc.RootElement.TryGetProperty("capabilities", out var c) && c.ValueKind == JsonValueKind.Array
                    ? c.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray()
                    : Array.Empty<string>();

                var dev = new DeviceInfo(id, name, res.RemoteEndPoint, caps, DateTime.UtcNow);
                var newDevice = !_devices.ContainsKey(id);
                _devices.AddOrUpdate(id, dev, (_, __) => dev);
                if (newDevice)
                {
                    DeviceAdded?.Invoke(this, EventArgs.Empty);
                }
            }

            if (type == "data_response")
            {
                if (!doc.RootElement.TryGetProperty("id", out var idEl)) return;
                if (!Guid.TryParse(idEl.GetString(), out var id)) return;

                // Update last seen (keep existing name/caps if present)
                _devices.AddOrUpdate(id,
                    (_) => new DeviceInfo(id, string.Empty, res.RemoteEndPoint, Array.Empty<string>(), DateTime.UtcNow),
                    (_, old) => new DeviceInfo(old.Id, old.Name, res.RemoteEndPoint, old.Capabilities, DateTime.UtcNow));

                if (!doc.RootElement.TryGetProperty("payload", out var readingsEl) || readingsEl.ValueKind != JsonValueKind.Array)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:O}] ProcessMessage: no payload or not an array for {id}");
                    return;
                }

                var readings = new List<SensorReading>();
                foreach (var item in readingsEl.EnumerateArray())
                {
                    // skip non-object or empty objects
                    if (item.ValueKind != JsonValueKind.Object || !item.EnumerateObject().Any())
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:O}] ProcessMessage: skipping empty/invalid payload item for {id}");
                        continue;
                    }

                    // key
                    if (!item.TryGetProperty("key", out var keyEl) || keyEl.ValueKind != JsonValueKind.String)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:O}] ProcessMessage: payload item missing 'key' for {id}");
                        continue;
                    }
                    var key = keyEl.GetString() ?? string.Empty;

                    // unit (optional)
                    var unit = item.TryGetProperty("unit", out var unitEl) && unitEl.ValueKind == JsonValueKind.String
                        ? unitEl.GetString() ?? string.Empty
                        : string.Empty;

                    // value -> try parse to double, otherwise null
                    double? value = null;
                    if (item.TryGetProperty("value", out var valEl))
                    {
                        switch (valEl.ValueKind)
                        {
                            case JsonValueKind.Number:
                                if (valEl.TryGetDouble(out var d)) value = d;
                                break;
                            case JsonValueKind.String:
                                if (double.TryParse(valEl.GetString(), System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
                                    value = parsed;
                                break;
                            case JsonValueKind.True:
                                value = 1.0;
                                break;
                            case JsonValueKind.False:
                                value = 0.0;
                                break;
                            default:
                                // unsupported kinds -> keep null
                                break;
                        }
                    }

                    readings.Add(new SensorReading(key, value, unit));
                }

                if (readings.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:O}] ProcessMessage: updating readings for {id}, count={readings.Count}");
                    _readings[id] = readings;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:O}] ProcessMessage: no valid readings parsed for {id}");
                }
            }
        }
        catch (Exception)
        {
            // ignore/ log
        }
    }

    public async Task SendNodeRequestAsync()
    {
        var broadcastEndpoint = new IPEndPoint(IPAddress.Broadcast, _sensorPort);
        await DiscoverNodes(broadcastEndpoint);
    }

    public async Task<RequestResult> SendDataRequestAsync(Guid deviceId, string[] sensors)
    {
        if (!_devices.TryGetValue(deviceId, out var dev)) return RequestResult.DeviceNotFound;
        var requestId = Guid.NewGuid().ToString();
        var msg = new
        {
            type = "data_request",
            version = 1,
            id = deviceId.ToString(),
            requestId,
            sensors,
            timestampUtc = DateTime.UtcNow
        };
        var bytes = JsonSerializer.SerializeToUtf8Bytes(msg);
        try
        {
            await _udp.SendAsync(bytes, bytes.Length, dev.EndPoint);
        }
        catch (Exception)
        {
            return RequestResult.SendFailed;
        }

        return RequestResult.Success;
    }

    public enum RequestResult { Success, Timeout, SendFailed, DeviceNotFound }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _udp.Dispose();
        _cts.Dispose();
        await Task.CompletedTask;
    }
}