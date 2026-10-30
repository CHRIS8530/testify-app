namespace Testify.Api.Services;

public static class RunStats
{
    // Share of a run's results that passed, as a whole percentage.
    // A run with no results has a pass rate of 0. Halves round up, so 1 of 8 is 13, not 12.
    public static int PassRate(int passed, int total)
    {
        if (total <= 0)
            return 0;

        return (int)Math.Round(100.0 * passed / total, MidpointRounding.AwayFromZero);
    }
}