using Argos.Engine;

namespace Argos.Tests;

/// <summary>When the engine counts as losing frames: a minute's share past the start, kept until the pipeline is rebuilt.</summary>
public static class FrameHealthTests
{
    static readonly DateTime T0 = new(2026, 10, 21, 20, 0, 0, DateTimeKind.Utc);

    /// <summary>Looks every 5 s from <paramref name="from"/> to <paramref name="to"/>: 300 frames each time, some lost.</summary>
    static (long Total, long Lost) Run(FrameHealth health, int from, int to, long total, long lost, long lostEachTime)
    {
        for (int second = from; second <= to; second += 5)
        {
            health.Sample(T0.AddSeconds(second), true, total, lost);
            total += 300;
            lost += lostEachTime;
        }
        return (total, lost);
    }

    [Test]
    public static void A_few_lost_frames_are_normal()
    {
        var health = new FrameHealth();
        Run(health, 0, 300, 0, 0, 1); // 1 in 300
        Assert.True(!health.Losing, "a third of a percent");
    }

    [Test]
    public static void Losing_over_two_percent_of_a_minutes_frames_counts_and_stays()
    {
        var health = new FrameHealth();
        var (total, lost) = Run(health, 0, 120, 0, 0, 15); // 15 in 300: 5%
        Assert.True(health.Losing, "5% lost");
        Run(health, 125, 400, total, lost, 0);
        Assert.True(health.Losing, "still shown after the game calms down");
    }

    [Test]
    public static void The_first_half_minute_after_a_start_does_not_count()
    {
        var health = new FrameHealth();
        var (total, lost) = Run(health, 0, 25, 0, 0, 200); // a heavy start
        Run(health, 30, 200, total, lost, 0);
        Assert.True(!health.Losing, "the lag of the start is left out");
    }

    [Test]
    public static void Paused_time_does_not_count_and_a_resume_settles_again()
    {
        var health = new FrameHealth();
        var (total, lost) = Run(health, 0, 60, 0, 0, 0);
        health.Sample(T0.AddSeconds(65), false, total, lost); // paused: nothing is encoded
        (total, lost) = Run(health, 70, 95, total, lost, 200); // a heavy half minute after resuming
        Run(health, 100, 200, total, lost, 0);
        Assert.True(!health.Losing, "only the steady part after the resume counts");
    }

    [Test]
    public static void Counters_that_start_over_begin_a_new_look()
    {
        var health = new FrameHealth();
        Run(health, 0, 100, 5000, 400, 0);
        Run(health, 105, 300, 0, 0, 3); // the counters went back to zero; then 1% lost
        Assert.True(!health.Losing, "1% after the restart, and no negative jump");
    }
}
