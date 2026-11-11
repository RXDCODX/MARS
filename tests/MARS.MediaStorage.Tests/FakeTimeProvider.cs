using MARS.TestKit.Postgres;
using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

public sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset _now = now;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan delta) => _now = _now.Add(delta);
}
