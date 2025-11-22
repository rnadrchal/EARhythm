using System.Collections.Generic;
using EnvironmentalSequencer.Services;

namespace EnvironmentalSequencer.Models;

public interface ISensorData
{
    void Update(IEnumerable<SensorReading> reading);
    IEnumerable<ValueMapping> GetAllMappings();
}