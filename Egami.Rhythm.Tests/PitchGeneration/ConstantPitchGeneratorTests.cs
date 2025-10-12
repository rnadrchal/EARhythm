using Egami.Pitch;

namespace Egami.Rhythm.Tests.PitchGeneration;

[TestClass]
public class ConstantPitchGeneratorTests
{
    [TestMethod]
    public void Generate_ShouldReturnConstantPitches()
    {
        // Arrange
        var generator = new ConstantPitchGenerator();
        byte basePitch = 60; // Middle C
        int length = 16;
        // Act
        var pitches = generator.Generate(basePitch, length);
        // Assert
        Assert.AreEqual(length, pitches.Length);
        foreach (var pitch in pitches)
        {
            Assert.AreEqual(basePitch, pitch);
        }
    }
}