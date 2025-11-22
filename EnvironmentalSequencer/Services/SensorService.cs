using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace EnvironmentalSequencer.Services;

public record DeviceInfo(Guid Id, string Name, IPEndPoint EndPoint, string[] Capabilities, DateTime LastSeenUtc);
public record SensorReading(string Key, double? Value, string Unit);

public class DeviceEventArgs : EventArgs
{
    public DeviceInfo Device { get; }
    public DeviceEventArgs(DeviceInfo device) => Device = device;
}

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
    private readonly TimeSpan _deviceTimeout;

    // Event fired when a device is removed due to timeout
    public EventHandler<DeviceEventArgs>? DeviceRemoved { get; set; }

    public SensorService(IConfigurationRoot config)
    {
        _config = config;
        _sensorPort = int.Parse(_config.GetSection("Sensor")["Port"] ?? "4210");
        _discoveryIntervalSeconds = int.Parse(_config.GetSection("Sensor")["DiscoveryIntervalSeconds"] ?? "60");
        _broadcastInterval = TimeSpan.FromSeconds(_discoveryIntervalSeconds);
        // Timeout: z.B. 2x DiscoveryInterval
        _deviceTimeout = TimeSpan.FromSeconds(Math.Max(1, _discoveryIntervalSeconds) * 2);

        _udp = new UdpClient(new IPEndPoint(IPAddress.Parse("192.168.99.50"), _sensorPort));
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.EnableBroadcast = true;
        _ = ReceiveLoop(_cts.Token);
        _ = BroadcastLoop(_cts.Token);
        _ = CleanupLoop(_cts.Token);
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
            System.Diagnostics.Debug.WriteLine($"DiscoverNodes send failed: {ex}");
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

    // Periodic cleanup: entfernt Geräte, deren LastSeenUtc älter als _deviceTimeout ist
    private async Task CleanupLoop(CancellationToken ct)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(5, _discoveryIntervalSeconds / 2.0));
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var cutoff = DateTime.UtcNow - _deviceTimeout;
                foreach (var kv in _devices.ToArray())
                {
                    if (kv.Value.LastSeenUtc < cutoff)
                    {
                        if (_devices.TryRemove(kv.Key, out var removed))
                        {
                            _readings.TryRemove(kv.Key, out _);
                            System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:O}] SensorService: removed device {removed.Id} ({removed.Name}) due timeout");
                            DeviceRemoved?.Invoke(this, new DeviceEventArgs(removed));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CleanupLoop exception: {ex}");
            }

            await Task.Delay(interval, ct).ContinueWith(_ => { });
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
                if (!doc.RootElement.TryGetProperty("id", out var idEl)) return;
                if (!Guid.TryParse(idEl.GetString(), out var id)) return;
                var name = doc.RootElement.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : string.Empty;
                var caps = doc.RootElement.TryGetProperty("capabilities", out var c) && c.ValueKind == JsonValueKind.Array
                    ? c.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray()
                    : Array.Empty<string>();

                var dev = new DeviceInfo(id, name ?? string.Empty, res.RemoteEndPoint, caps, DateTime.UtcNow);
                var newDevice = !_devices.ContainsKey(id);
                _devices.AddOrUpdate(id, dev, (_, old) => new DeviceInfo(old.Id, old.Name, res.RemoteEndPoint, old.Capabilities, DateTime.UtcNow));
                if (newDevice)
                {
                    DeviceAdded?.Invoke(this, EventArgs.Empty);
                }

                return;
            }

            if (type == "data_response")
            {
                if (!doc.RootElement.TryGetProperty("id", out var idEl)) return;
                if (!Guid.TryParse(idEl.GetString(), out var id)) return;

                // Update last seen / endpoint for device
                _devices.AddOrUpdate(id,
                    (_) => new DeviceInfo(id, string.Empty, res.RemoteEndPoint, Array.Empty<string>(), DateTime.UtcNow),
                    (_, old) => new DeviceInfo(old.Id, old.Name, res.RemoteEndPoint, old.Capabilities, DateTime.UtcNow));

                if (doc.RootElement.TryGetProperty("payload", out var readingsEl) && readingsEl.ValueKind == JsonValueKind.Array)
                {
                    var readings = new List<SensorReading>();
                    foreach (var reading in readingsEl.EnumerateArray())
                    {
                        // defensive parsing (skip empty/invalid objects)
                        if (reading.ValueKind != JsonValueKind.Object || !reading.EnumerateObject().Any())
                            continue;

                        var key = reading.TryGetProperty("key", out var keyEl) && keyEl.ValueKind == JsonValueKind.String ? keyEl.GetString() : null;
                        double? value = null;
                        if (reading.TryGetProperty("value", out var valEl))
                        {
                            if (valEl.ValueKind == JsonValueKind.Number && valEl.TryGetDouble(out var d)) value = d;
                            else if (valEl.ValueKind == JsonValueKind.String && double.TryParse(valEl.GetString(), System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands, System.Globalization.CultureInfo.InvariantCulture, out var parsed)) value = parsed;
                            else if (valEl.ValueKind == JsonValueKind.True) value = 1.0;
                            else if (valEl.ValueKind == JsonValueKind.False) value = 0.0;
                        }
                        var unit = reading.TryGetProperty("unit", out var unitEl) && unitEl.ValueKind == JsonValueKind.String ? unitEl.GetString() : string.Empty;

                        if (key != null)
                            readings.Add(new SensorReading(key, value, unit ?? string.Empty));
                    }

                    if (readings.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"[{DateTime.UtcNow:O}] ProcessMessage: updating readings for {id}, count={readings.Count}");
                        _readings[id] = readings;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ProcessMessage exception: {ex}");
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