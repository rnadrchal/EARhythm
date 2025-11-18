using System.Collections.Generic;
using System.Linq;
using EnvironmentalSequencer.Services;
using Prism.Mvvm;

namespace EnvironmentalSequencer.Models;

public class Bme680EnvironmentalSensorData : BindableBase, ISensorData
{
    private TemperatureMapping _temperature;
    private PercentMapping _humidity;
    private PressureMapping _pressure;
    private GasResistanceMapping _gasResistance;
    private AltitudeMapping _altitude;

    public TemperatureMapping Temperature => _temperature;
    public PercentMapping Humidity => _humidity;
    public PressureMapping Pressure => _pressure;
    public GasResistanceMapping GasResistance => _gasResistance;
    public AltitudeMapping Altitude => _altitude;

    public Bme680EnvironmentalSensorData(IEnumerable<SensorReading> readings) 
    {
        var tempValue = readings.SingleOrDefault(r => r.Key == "Temperature")?.Value ?? 0.0;
        _temperature = new TemperatureMapping("Temp", tempValue, "°C");
        var humValue = readings.SingleOrDefault(r => r.Key == "Humidity")?.Value ?? 0.0;
        _humidity = new PercentMapping("Hum", humValue, "%");
        var presValue = readings.SingleOrDefault(r => r.Key == "Pressure")?.Value ?? 0.0;
        _pressure = new PressureMapping("Pres", presValue, "hPa");
        var gasValue = readings.SingleOrDefault(r => r.Key == "Gas")?.Value ?? 0.0;
        _gasResistance = new GasResistanceMapping("Gas", gasValue, "kΩ");
        var altValue = readings.SingleOrDefault(r => r.Key == "Altitude")?.Value ?? 0.0;
        _altitude = new AltitudeMapping("Alt", altValue, "m");
    }   

    public void Update(IEnumerable<SensorReading> reading)
    {
        var tempValue = reading.SingleOrDefault(r => r.Key == "Temperature")?.Value;
        if (tempValue.HasValue)
            Temperature.Value = tempValue.Value;
        var humValue = reading.SingleOrDefault(r => r.Key == "Humidity")?.Value;
        if (humValue.HasValue)
            Humidity.Value = humValue.Value;
        var presValue = reading.SingleOrDefault(r => r.Key == "Pressure")?.Value;
        if (presValue.HasValue)
            Pressure.Value = presValue.Value;
        var gasValue = reading.SingleOrDefault(r => r.Key == "Gas")?.Value;
        if (gasValue.HasValue)
            GasResistance.Value = gasValue.Value;
        var altValue = reading.SingleOrDefault(r => r.Key == "Altitude")?.Value;
        if (altValue.HasValue)
            Altitude.Value = altValue.Value;
    }

    public IEnumerable<ValueMapping> GetAllMappings()
    {
        yield return _temperature;
        yield return _humidity;
        yield return _pressure;
        yield return _gasResistance;
        yield return _altitude;
    }
}