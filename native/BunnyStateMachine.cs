using System;

public enum BunnyState
{
    Idle,
    Walk,
    Stand,
    Sleep,
    Happy,
    Drag
}

public sealed class BunnyStateMachine
{
    private DateTime lastInteraction;
    private bool paused;

    public BunnyStateMachine(DateTime now)
    {
        State = BunnyState.Idle;
        Direction = -1;
        lastInteraction = now;
    }

    public BunnyState State { get; private set; }

    public int Direction { get; private set; }

    public void SetState(BunnyState next)
    {
        if (!Enum.IsDefined(typeof(BunnyState), next))
            throw new ArgumentOutOfRangeException("next", next, "Unknown bunny state.");
        State = next;
    }

    public void SetPaused(bool value)
    {
        paused = value;
        if (paused) SetState(BunnyState.Sleep);
    }

    public void Touch(DateTime now)
    {
        lastInteraction = now;
        if (!paused && State == BunnyState.Sleep) SetState(BunnyState.Idle);
    }

    public int TurnAround()
    {
        Direction *= -1;
        return Direction;
    }

    public BunnyState ChooseNext(double roll, DateTime now)
    {
        if (paused || now - lastInteraction > TimeSpan.FromSeconds(120)) return BunnyState.Sleep;
        if (roll < 0.42) return BunnyState.Idle;
        if (roll < 0.72) return BunnyState.Walk;
        if (roll < 0.90) return BunnyState.Stand;
        return BunnyState.Sleep;
    }
}
