using System.Collections.Generic;
using System.Linq;
using EnvironmentalSequencer.Services;
using Prism.Mvvm;

namespace EnvironmentalSequencer.Models;

public class Sph0645MicrophoneData : BindableBase, ISensorData
{
    private RmsMapping _rms;
    private RmsMapping _peak;

    public RmsMapping Rms => _rms;
    public RmsMapping Peak => _peak;

    public Sph0645MicrophoneData(IEnumerable<SensorReading> readings)
    {
        var rmsValue = readings.SingleOrDefault(r => r.Key == "RMS")?.Value ?? 0.0;
        _rms = new RmsMapping("RMS", rmsValue, "");
        var peakValue = readings.SingleOrDefault(r => r.Key == "Peak")?.Value ?? 0.0;
        _peak = new RmsMapping("Peak", peakValue, "");
    }

    public void Update(IEnumerable<SensorReading> reading)
    {
        var rmsValue = reading.SingleOrDefault(r => r.Key == "RMS")?.Value;
        if (rmsValue.HasValue)
        {
            Rms.Value = rmsValue.Value;
        }
        var peakValue = reading.SingleOrDefault(r => r.Key == "Peak")?.Value;
        if (peakValue.HasValue)
        {
            Peak.Value = peakValue.Value;
        }
    }

    public IEnumerable<ValueMapping> GetAllMappings()
    {
        yield return _rms;
        yield return _peak;
    }
}