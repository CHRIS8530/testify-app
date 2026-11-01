namespace Testify.Api.Services;

public static class Paging
{
    // The largest page the API will return. The web app asks for at most 200.
    public const int MaxLimit = 200;

    // Returns a message when the values are not allowed, or null when they are fine.
    // PostgreSQL rejects a negative LIMIT or OFFSET, so a bad value must be a 400 and never reach the database.
    public static string? Validate(int limit, int offset)
    {
        if (limit < 1 || limit > MaxLimit)
            return $"limit must be between 1 and {MaxLimit}";

        if (offset < 0)
            return "offset must be 0 or more";

        return null;
    }
}