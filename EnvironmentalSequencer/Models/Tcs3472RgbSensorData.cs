using System.Collections.Generic;
using System.Linq;
using EnvironmentalSequencer.Services;
using Prism.Mvvm;

namespace EnvironmentalSequencer.Models;

public class Tcs3472RgbSensorData : BindableBase, ISensorData
{
    private PercentMapping _rRaw;
    private PercentMapping _gRaw;
    private PercentMapping _bRaw;
    private PercentMapping _rComp;
    private PercentMapping _gComp;
    private PercentMapping _bComp;
    private PercentMapping _sat;
    private PercentMapping _sat75;
    private PercentMapping _ir;
    private LuxMapping _lux;
    private ColorTemperatureMapping _ct;
    public PercentMapping RRaw => _rRaw;
    public PercentMapping GRaw => _gRaw;
    public PercentMapping BRaw => _bRaw;
    public PercentMapping RComp => _rComp;
    public PercentMapping GComp => _gComp;
    public PercentMapping BComp => _bComp;
    public PercentMapping Saturation => _sat;
    public PercentMapping Saturation75 => _sat75;
    public PercentMapping IR => _ir;
    public LuxMapping Lux => _lux;
    public ColorTemperatureMapping ColorTemperature => _ct;

    public Tcs3472RgbSensorData(IEnumerable<SensorReading> readings)
    {
        var rRawValue = readings.SingleOrDefault(r => r.Key == "Rraw")?.Value ?? 0.0;
        _rRaw = new PercentMapping("R", rRawValue, "%");
        var gRawValue = readings.SingleOrDefault(r => r.Key == "Graw")?.Value ?? 0.0;
        _gRaw = new PercentMapping("G", gRawValue, "%");
        var bRawValue = readings.SingleOrDefault(r => r.Key == "Braw")?.Value ?? 0.0;
        _bRaw = new PercentMapping("B", bRawValue, "%");
        var rCompValue = readings.SingleOrDefault(r => r.Key == "Rcomp")?.Value ?? 0.0;
        _rComp = new PercentMapping("R", rCompValue, "%");
        var gCompValue = readings.SingleOrDefault(r => r.Key == "Gcomp")?.Value ?? 0.0;
        _gComp = new PercentMapping("G", gCompValue, "%");
        var bCompValue = readings.SingleOrDefault(r => r.Key == "Bcomp")?.Value ?? 0.0;
        _bComp = new PercentMapping("B", bCompValue, "%");
        var satValue = readings.SingleOrDefault(r => r.Key == "Sat")?.Value ?? 0.0;
        _sat = new PercentMapping("Sat", satValue, "%");
        var sat75Value = readings.SingleOrDefault(r => r.Key == "Sat75")?.Value ?? 0.0;
        _sat75 = new PercentMapping("Sat 75", sat75Value, "%");
        var irValue = readings.SingleOrDefault(r => r.Key == "IR")?.Value ?? 0.0;
        _ir = new PercentMapping("IR", irValue, "%");
        var luxValue = readings.SingleOrDefault(r => r.Key == "Lux")?.Value ?? 0.0;
        _lux = new LuxMapping("Lux", luxValue);
        var ctValue = readings.SingleOrDefault(r => r.Key == "CT")?.Value ?? 0.0;
        _ct = new ColorTemperatureMapping("CT", ctValue,"K");
    }

    public void Update(IEnumerable<SensorReading> reading)
    {
        _rRaw.Value = reading.SingleOrDefault(r => r.Key == "Rraw")?.Value ?? 0.0;
        _gRaw.Value = reading.SingleOrDefault(r => r.Key == "Graw")?.Value ?? 0.0;
        _bRaw.Value = reading.SingleOrDefault(r => r.Key == "Braw")?.Value ?? 0.0;
        _rComp.Value = reading.SingleOrDefault(r => r.Key == "Rcomp")?.Value ?? 0.0;
        _gComp.Value = reading.SingleOrDefault(r => r.Key == "Gcomp")?.Value ?? 0.0;
        _bComp.Value = reading.SingleOrDefault(r => r.Key == "Bcomp")?.Value ?? 0.0;
        _sat.Value = reading.SingleOrDefault(r => r.Key == "Sat")?.Value ?? 0.0;
        _sat75.Value = reading.SingleOrDefault(r => r.Key == "Sat75")?.Value ?? 0.0;
        _ir.Value = reading.SingleOrDefault(r => r.Key == "IR")?.Value ?? 0.0;
        _lux.Value = reading.SingleOrDefault(r => r.Key == "Lux")?.Value ?? 0.0;
        _ct.Value = reading.SingleOrDefault(r => r.Key == "CT")?.Value ?? 0.0;
    }

    public IEnumerable<ValueMapping> GetAllMappings()
    {
        yield return _rRaw;
        yield return _gRaw;
        yield return _bRaw;
        yield return _rComp;
        yield return _gComp;
        yield return _bComp;
        yield return _sat;
        yield return _sat75;
        yield return _ir;
        yield return _lux;
        yield return _ct;
    }
}