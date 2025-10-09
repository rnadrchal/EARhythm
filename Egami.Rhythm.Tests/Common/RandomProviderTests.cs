using Egami.Rhythm.Common;
using FluentAssertions;

namespace Egami.Rhythm.Tests.Common;

[TestClass]
public class RandomProviderTests
{
    [TestMethod]
    public void CanGetDeterministicValues()
    {
        int r1 = RandomProvider.Get(123).Next(0, 100);
        int r2 = RandomProvider.Get(123).Next(0, 100);
        int r3 = RandomProvider.Get(234).Next(0, 100);

        r1.Should().Be(r2);
        r3.Should().NotBe(r1);
    }

    [TestMethod]
    public void CanGetIndeterministicValues()
    {
        int r1 = RandomProvider.Get().Next(0, 100);
        int r2 = RandomProvider.Get().Next(0, 100);

        r1.Should().NotBe(r2);
    }
}