using Testify.Api.Services;
using Xunit;

namespace Testify.Api.Tests;

public class LoginThrottleUnitTests
{
    private class TestClock : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = new DateTimeOffset(2026, 11, 13, 10, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static void Fail(LoginThrottle throttle, string email, int times)
    {
        for (var i = 0; i < times; i++)
            throttle.RecordFailure(email);
    }

    [Fact]
    public void IsBlocked_IsFalseBeforeTheLimit()
    {
        var throttle = new LoginThrottle(new TestClock());
        Fail(throttle, "a@example.com", LoginThrottle.MaxFailures - 1);

        Assert.False(throttle.IsBlocked("a@example.com"));
    }

    [Fact]
    public void IsBlocked_IsTrueAtTheLimit()
    {
        var throttle = new LoginThrottle(new TestClock());
        Fail(throttle, "a@example.com", LoginThrottle.MaxFailures);

        Assert.True(throttle.IsBlocked("a@example.com"));
    }

    [Fact]
    public void IsBlocked_BecomesFalseOnceTheWindowHasPassed()
    {
        var clock = new TestClock();
        var throttle = new LoginThrottle(clock);
        Fail(throttle, "a@example.com", LoginThrottle.MaxFailures);

        clock.Now += LoginThrottle.Window;

        Assert.False(throttle.IsBlocked("a@example.com"));
    }

    [Fact]
    public void FailuresOutsideTheWindowDoNotCount()
    {
        var clock = new TestClock();
        var throttle = new LoginThrottle(clock);
        Fail(throttle, "a@example.com", LoginThrottle.MaxFailures - 1);

        clock.Now += LoginThrottle.Window + TimeSpan.FromMinutes(1);
        throttle.RecordFailure("a@example.com");

        Assert.False(throttle.IsBlocked("a@example.com"));
    }

    [Fact]
    public void Reset_ClearsTheCount()
    {
        var throttle = new LoginThrottle(new TestClock());
        Fail(throttle, "a@example.com", LoginThrottle.MaxFailures);

        throttle.Reset("a@example.com");

        Assert.False(throttle.IsBlocked("a@example.com"));
    }

    [Fact]
    public void Emails_AreComparedIgnoringCaseAndSpaces()
    {
        var throttle = new LoginThrottle(new TestClock());
        Fail(throttle, "A@Example.com", LoginThrottle.MaxFailures);

        Assert.True(throttle.IsBlocked("  a@example.com "));
    }
}