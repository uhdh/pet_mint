using System;
using BunnyPet;

internal static class StateMachineTests
{
    private static void Require(bool value, string name) { if (!value) { Console.WriteLine("TEST FAILED: " + name); Environment.Exit(1); } }

    public static int Main()
    {
        var now = new DateTime(2026, 9, 4, 0, 0, 0, DateTimeKind.Utc);
        var bunny = new BunnyStateMachine(now);
        Require(bunny.State == BunnyState.Idle && bunny.Direction == -1, "initial state");
        Require(!bunny.PlayMode, "default is stopped mode");
        Require(bunny.ChooseNext(0.50, now) == BunnyState.Idle, "stopped mode stays idle");
        bunny.SetPlayMode(true);
        Require(bunny.PlayMode, "play mode enabled");
        Require(bunny.ChooseNext(0.50, now) == BunnyState.Walk, "walk range in play mode");
        bunny.SetPlayMode(false);
        Require(!bunny.PlayMode && bunny.State == BunnyState.Idle, "stopped mode reset to idle");
        bunny.SetPaused(true);
        Require(bunny.State == BunnyState.Sleep && bunny.Paused, "pause sleeps");
        bunny.SetPaused(false);
        Require(!bunny.Paused, "pause cleared");
        bunny.Touch(now);
        Require(bunny.ChooseNext(0.10, now.AddSeconds(121)) == BunnyState.Sleep, "inactivity sleeps");
        Require(bunny.TurnAround() == 1, "turn around");
        bunny.SetDirection(-1);
        Require(bunny.Direction == -1, "set direction left");
        bunny.SetDirection(1);
        Require(bunny.Direction == 1, "set direction right");
        var missing = AppSettings.Parse(null);
        Require(!missing.AlwaysOnTop && !missing.AutoStart, "missing settings defaults");
        Require(missing.EmojiFrequency == 0 && missing.PurrFrequency == 0, "missing emoji/purr frequency defaults 0 (rare)");
        var omittedMembers = AppSettings.Parse("{}");
        Require(!omittedMembers.AlwaysOnTop && !omittedMembers.AutoStart, "omitted settings merge defaults");
        Require(omittedMembers.EmojiFrequency == 0 && omittedMembers.PurrFrequency == 0, "omitted emoji/purr frequency defaults 0");
        var customFreq = AppSettings.Parse("{\"emojiFrequency\":2,\"purrFrequency\":3}");
        Require(customFreq.EmojiFrequency == 2 && customFreq.PurrFrequency == 3, "custom emoji/purr frequency parsed");
        var autoStartOnly = AppSettings.Parse("{\"autoStart\":true}");
        Require(!autoStartOnly.AlwaysOnTop && autoStartOnly.AutoStart, "partial settings merge defaults");
        var malformed = AppSettings.Parse("{");
        Require(!malformed.AlwaysOnTop && !malformed.AutoStart, "malformed settings defaults");
        var original = new AppSettings { AlwaysOnTop = true, AutoStart = true, RestRemindersEnabled = false, EmojiFrequency = 1, PurrFrequency = 2 };
        var parsed = AppSettings.Parse(AppSettings.Serialize(original));
        Require(parsed.AlwaysOnTop == original.AlwaysOnTop && parsed.AutoStart == original.AutoStart, "settings round trip");
        Require(parsed.RestRemindersEnabled == original.RestRemindersEnabled, "rest reminder setting round trip");
        Require(parsed.EmojiFrequency == original.EmojiFrequency && parsed.PurrFrequency == original.PurrFrequency, "frequency setting round trip");
        Require(AppSettings.Parse(null).RestRemindersEnabled, "rest reminders default on");

        var exportPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bunny-settings-test-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            original.SaveTo(exportPath);
            var imported = AppSettings.LoadFrom(exportPath);
            Require(imported.AlwaysOnTop == original.AlwaysOnTop
                && imported.AutoStart == original.AutoStart
                && imported.RestRemindersEnabled == original.RestRemindersEnabled
                && imported.EmojiFrequency == original.EmojiFrequency
                && imported.PurrFrequency == original.PurrFrequency, "settings export/import round trip");
        }
        finally
        {
            if (System.IO.File.Exists(exportPath)) System.IO.File.Delete(exportPath);
        }

        Require(ClimbMath.Clamp(-10, 0, 50) == 0, "climb clamp lower bound");
        Require(ClimbMath.Clamp(60, 0, 50) == 50, "climb clamp upper bound");
        Require(ClimbMath.Clamp(25, 0, 50) == 25, "climb clamp within range");
        Require(ClimbMath.ShouldClimb(0.1) && !ClimbMath.ShouldClimb(0.9), "climb chance threshold");

        Require((int)BunnyItem.None == 0, "item None");
        Require(BunnyItem.Doll != BunnyItem.None, "item Doll");
        Require(BunnyItem.Hay != BunnyItem.None, "item Hay");
        Require(BunnyItem.Bag != BunnyItem.None, "item Bag");
        Require(BunnyItem.House != BunnyItem.None, "item House");
        Require(BunnyItem.Chair != BunnyItem.None, "item Chair");

        Console.WriteLine("Native state tests passed.");
        return 0;
    }
}
