using Testify.Api.Services;
using Xunit;

namespace Testify.Api.Tests;

public class RunStatsTests
{
    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(0, 4, 0)]
    [InlineData(4, 4, 100)]
    [InlineData(1, 3, 33)]
    [InlineData(2, 3, 67)]
    [InlineData(1, 8, 13)]
    public void PassRate_IsPassedOverTotal_RoundedToAWholeNumber(int passed, int total, int expected)
    {
        Assert.Equal(expected, RunStats.PassRate(passed, total));
    }
}