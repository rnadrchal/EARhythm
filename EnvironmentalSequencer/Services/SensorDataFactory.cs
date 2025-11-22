using System;
using System.Collections.Generic;
using EnvironmentalSequencer.Models;

namespace EnvironmentalSequencer.Services;

public class SensorDataFactory
{
    private Dictionary<string, ISensorData> _sensorData = new Dictionary<string, ISensorData>();


    public ISensorData GetOrCreate(string sensorName, IEnumerable<SensorReading> reading)
    {
        if (_sensorData.ContainsKey(sensorName))
        {
            _sensorData[sensorName].Update(reading);
            return _sensorData[sensorName];
        }
        else
        {
            if (sensorName.Contains("TCS3472"))
            {
                var sensorData = new Tcs3472RgbSensorData(reading);
                _sensorData[sensorName] = sensorData;
                return sensorData;
            }
        }

        throw new ArgumentException($"Unknown sensor '{sensorName}'.");
    }
}