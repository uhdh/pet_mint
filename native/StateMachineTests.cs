using System;

internal static class StateMachineTests
{
    private static void Require(bool value, string name) { if (!value) throw new Exception(name); }

    public static int Main()
    {
        var now = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);
        var bunny = new BunnyStateMachine(now);
        Require(bunny.State == BunnyState.Idle && bunny.Direction == -1, "initial state");
        Require(bunny.ChooseNext(0.50, now) == BunnyState.Walk, "walk range");
        bunny.SetPaused(true);
        Require(bunny.State == BunnyState.Sleep, "pause sleeps");
        bunny.SetPaused(false);
        bunny.Touch(now);
        Require(bunny.ChooseNext(0.10, now.AddSeconds(121)) == BunnyState.Sleep, "inactivity sleeps");
        Require(bunny.TurnAround() == 1, "turn around");
        Console.WriteLine("Native state tests passed.");
        return 0;
    }
}
