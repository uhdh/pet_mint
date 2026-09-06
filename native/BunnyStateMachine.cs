using System;

public enum BunnyState
{
    Idle,
    Walk,
    Stand,
    Sleep,
    Happy,
    Drag,
    Beg,
    Angry,
    Front,
    Intro,
    Binky,
    Kiss,
    Wash,
    Flop,
    Confused
}

public enum BunnyItem
{
    None,
    Hay,
    Doll,
    Bag,
    House,
    Chair
}

public static class BunnyProgression
{
    public const int AffinityHay = 0;
    public const int AffinityChair = 10;
    public const int AffinityDoll = 25;
    public const int AffinityBag = 40;
    public const int AffinityHouse = 60;

    public const int AffinityIntro = 0;
    public const int AffinityFront = 0;
    public const int AffinityBeg = 5;
    public const int AffinityWash = 15;
    public const int AffinityAngry = 20;
    public const int AffinityKiss = 30;
    public const int AffinityBinky = 50;
    public const int AffinityConfused = 70;
    public const int AffinityFlop = 85;

    public static int GetRequiredAffinity(BunnyItem item)
    {
        switch (item)
        {
            case BunnyItem.Chair: return AffinityChair;
            case BunnyItem.Doll: return AffinityDoll;
            case BunnyItem.Bag: return AffinityBag;
            case BunnyItem.House: return AffinityHouse;
            default: return 0;
        }
    }

    public static int GetRequiredAffinity(BunnyState state)
    {
        switch (state)
        {
            case BunnyState.Beg: return AffinityBeg;
            case BunnyState.Wash: return AffinityWash;
            case BunnyState.Angry: return AffinityAngry;
            case BunnyState.Kiss: return AffinityKiss;
            case BunnyState.Binky: return AffinityBinky;
            case BunnyState.Confused: return AffinityConfused;
            case BunnyState.Flop: return AffinityFlop;
            default: return 0;
        }
    }

    public static bool IsUnlocked(BunnyItem item, int affinity)
    {
        return affinity >= GetRequiredAffinity(item);
    }

    public static bool IsUnlocked(BunnyState state, int affinity)
    {
        return affinity >= GetRequiredAffinity(state);
    }

    public static string GetLevelName(int affinity)
    {
        if (affinity >= 85) return "Lv.5 가족이 된 민트 💖";
        if (affinity >= 60) return "Lv.4 무한 애정 민트 💕";
        if (affinity >= 30) return "Lv.3 단짝 친구 민트 🥰";
        if (affinity >= 10) return "Lv.2 마음을 여는 민트 🌸";
        return "Lv.1 낯가리는 민트 🌱";
    }

    public static string GetNextUnlockDescription(int affinity)
    {
        if (affinity < 5) return "다음 해금: 간식 내놔! (5점) 🌾";
        if (affinity < 10) return "다음 해금: 반성의자 (10점) 🪑";
        if (affinity < 15) return "다음 해금: 세수하기 (15점) 🧼";
        if (affinity < 20) return "다음 해금: 화났어! (20점) 💢";
        if (affinity < 25) return "다음 해금: 토끼 인형 (25점) 🧸";
        if (affinity < 30) return "다음 해금: 래빗키스 뽀뽀 (30점) 💋";
        if (affinity < 40) return "다음 해금: 토끼 가방 (40점) 🎒";
        if (affinity < 50) return "다음 해금: 신나는 점프 빙키 (50점) 🤸";
        if (affinity < 60) return "다음 해금: 아늑한 집 (60점) 🏠";
        if (affinity < 70) return "다음 해금: 어리둥절 실사 영상 (70점) 👀";
        if (affinity < 85) return "다음 해금: 안심 벌러덩 눕기 (85점) 🛌";
        return "모든 특별 포즈와 선물 해금 완료! 👑";
    }
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

    public bool Paused
    {
        get { return paused; }
    }

    public bool PlayMode { get; private set; }

    public void SetPlayMode(bool value)
    {
        PlayMode = value;
        if (!PlayMode)
        {
            SetState(BunnyState.Idle);
        }
    }

    public void SetDirection(int value)
    {
        Direction = value < 0 ? -1 : 1;
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
        if (!PlayMode)
        {
            // 기본은 멈추기 자세 (Idle 또는 편히 쉬기)
            return roll < 0.85 ? BunnyState.Idle : BunnyState.Sleep;
        }
        if (roll < 0.42) return BunnyState.Idle;
        if (roll < 0.72) return BunnyState.Walk;
        if (roll < 0.90) return BunnyState.Stand;
        return BunnyState.Sleep;
    }
}

public static class ClimbMath
{
    // ponytail: reuses the existing walk sprite/animation for vertical movement
    // instead of new climbing artwork; only the position math needs a check.
    public static double Clamp(double value, double min, double max)
    {
        if (min > max) return min;
        return Math.Max(min, Math.Min(value, max));
    }

    public static bool ShouldClimb(double roll)
    {
        return roll < 0.3;
    }
}
