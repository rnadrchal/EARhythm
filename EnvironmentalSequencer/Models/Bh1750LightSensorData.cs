using System.Collections.Generic;
using System.Linq;
using EnvironmentalSequencer.Services;
using Prism.Mvvm;

namespace EnvironmentalSequencer.Models;

public sealed class Bh1750LightSensorData : BindableBase, ISensorData
{
    private LuxMapping _lux;
    public LuxMapping Lux => _lux;
    public Bh1750LightSensorData(IEnumerable<SensorReading> readings)
    {
        var luxValue = readings.SingleOrDefault(r => r.Key == "Lux");
        _lux = new LuxMapping("Lux", luxValue?.Value ?? 0.0, "lx");
    }

    public void Update(IEnumerable<SensorReading> reading)
    {
        var luxValue = reading.SingleOrDefault(r => r.Key == "Lux")?.Value;
        if (luxValue.HasValue)
        {
            Lux.Value = luxValue.Value;
        }
    }

    public IEnumerable<ValueMapping> GetAllMappings()
    {
        yield return _lux;
    }
}