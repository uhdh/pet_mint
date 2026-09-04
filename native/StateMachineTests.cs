using System;
using BunnyPet;

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
        var missing = AppSettings.Parse(null);
        Require(missing.AlwaysOnTop && !missing.AutoStart, "missing settings defaults");
        var malformed = AppSettings.Parse("{");
        Require(malformed.AlwaysOnTop && !malformed.AutoStart, "malformed settings defaults");
        var original = new AppSettings { AlwaysOnTop = false, AutoStart = true };
        var parsed = AppSettings.Parse(AppSettings.Serialize(original));
        Require(parsed.AlwaysOnTop == original.AlwaysOnTop && parsed.AutoStart == original.AutoStart, "settings round trip");
        Console.WriteLine("Native state tests passed.");
        return 0;
    }
}
