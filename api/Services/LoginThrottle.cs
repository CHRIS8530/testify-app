using System.Collections.Concurrent;

namespace Testify.Api.Services;

public interface ILoginThrottle
{
    bool IsBlocked(string email);
    void RecordFailure(string email);
    void Reset(string email);
}

// Counts failed sign ins per email. After MaxFailures within Window, that email is blocked until the oldest failure ages out.
// The counts live in memory, so they reset when the API restarts and are not shared between instances.
public class LoginThrottle : ILoginThrottle
{
    public const int MaxFailures = 5;
    public static readonly TimeSpan Window = TimeSpan.FromMinutes(15);
    private const int SweepThreshold = 10_000;

    private readonly ConcurrentDictionary<string, Queue<DateTimeOffset>> _failures = new();
    private readonly TimeProvider _time;

    public LoginThrottle(TimeProvider time)
    {
        _time = time;
    }

    // The email decides the key, ignoring case and surrounding spaces.
    private static string Key(string email) => email.Trim().ToLowerInvariant();

    public bool IsBlocked(string email)
    {
        if (!_failures.TryGetValue(Key(email), out var queue))
            return false;

        lock (queue)
        {
            Trim(queue, _time.GetUtcNow());
            return queue.Count >= MaxFailures;
        }
    }

    public void RecordFailure(string email)
    {
        var now = _time.GetUtcNow();
        var queue = _failures.GetOrAdd(Key(email), _ => new Queue<DateTimeOffset>());

        lock (queue)
        {
            Trim(queue, now);
            queue.Enqueue(now);
        }

        if (_failures.Count > SweepThreshold)
            Sweep(now);
    }

    public void Reset(string email)
    {
        _failures.TryRemove(Key(email), out _);
    }

    private static void Trim(Queue<DateTimeOffset> queue, DateTimeOffset now)
    {
        while (queue.Count > 0 && now - queue.Peek() >= Window)
            queue.Dequeue();
    }

    // Unknown emails are counted too, so the table could grow. Once it is large, drop the entries that have expired.
    private void Sweep(DateTimeOffset now)
    {
        foreach (var pair in _failures)
        {
            lock (pair.Value)
            {
                Trim(pair.Value, now);
                if (pair.Value.Count == 0)
                    _failures.TryRemove(pair.Key, out _);
            }
        }
    }
}