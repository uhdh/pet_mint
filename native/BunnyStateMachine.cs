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
    Confused,
    Wanted,
    RealAngry,
    PetReal,
    Angry2,
    Curious,
    Cheer,
    Chin,
    Paw,
    Stand2
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
    // ── 아이템 해금 점수 ──────────────────────────────────
    public const int AffinityHay   = 0;
    public const int AffinityChair = 10;
    public const int AffinityDoll  = 25;
    public const int AffinityBag   = 40;
    public const int AffinityHouse = 60;

    // ── 포즈 해금 점수 (등급별) ────────────────────────────
    // [기본] 처음부터 사용 가능
    public const int AffinityBeg   = 5;
    public const int AffinityWash  = 15;
    public const int AffinityAngry = 20;
    public const int AffinityCurious = 25;
    public const int AffinityPaw   = 30;
    public const int AffinityStand2 = 50;
    public const int AffinityAngry2 = 65;
    public const int AffinityCheer  = 68;
    public const int AffinityChin   = 75;
    public const int AffinityWanted = 85;

    // [희귀] ★★
    public const int AffinityIntro = 35;   // 천사 민트 등장
    public const int AffinityFlop  = 45;   // 발라당 눕기

    // [특별] ★★★
    public const int AffinityKiss  = 55;   // 래빗키스 뽀뽀
    public const int AffinityBinky = 63;   // 빙키 점프

    // [유니크] ★★★★
    public const int AffinityRealAngry = 72; // 찐 화난 민트
    public const int AffinityFront     = 80; // 똘망똘망 정면

    // [전설] ★★★★★
    public const int AffinityPetReal  = 88;  // 실사 쓰다듬기 영상
    public const int AffinityConfused = 94;  // 어리둥절 실사 영상

    public static int GetRequiredAffinity(BunnyItem item)
    {
        switch (item)
        {
            case BunnyItem.Chair: return AffinityChair;
            case BunnyItem.Doll:  return AffinityDoll;
            case BunnyItem.Bag:   return AffinityBag;
            case BunnyItem.House: return AffinityHouse;
            default: return 0;
        }
    }

    public static int GetRequiredAffinity(BunnyState state)
    {
        switch (state)
        {
            case BunnyState.Beg:      return AffinityBeg;
            case BunnyState.Wash:     return AffinityWash;
            case BunnyState.Angry:    return AffinityAngry;
            case BunnyState.Curious:  return AffinityCurious;
            case BunnyState.Paw:      return AffinityPaw;
            case BunnyState.Stand2:   return AffinityStand2;
            case BunnyState.Angry2:   return AffinityAngry2;
            case BunnyState.Cheer:    return AffinityCheer;
            case BunnyState.Chin:     return AffinityChin;
            case BunnyState.Wanted:   return AffinityWanted;
            // 희귀
            case BunnyState.Intro:    return AffinityIntro;
            case BunnyState.Flop:     return AffinityFlop;
            // 특별
            case BunnyState.Kiss:     return AffinityKiss;
            case BunnyState.Binky:    return AffinityBinky;
            // 유니크
            case BunnyState.RealAngry: return AffinityRealAngry;
            case BunnyState.Front:    return AffinityFront;
            // 전설
            case BunnyState.PetReal:  return AffinityPetReal;
            case BunnyState.Confused: return AffinityConfused;
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

    public static string GetRarityLabel(BunnyState state)
    {
        int req = GetRequiredAffinity(state);
        if (state == BunnyState.PetReal || state == BunnyState.Confused) return "⭐⭐⭐⭐⭐ 전설";
        if (state == BunnyState.RealAngry || state == BunnyState.Front)  return "⭐⭐⭐⭐ 유니크";
        if (state == BunnyState.Kiss || state == BunnyState.Binky)       return "⭐⭐⭐ 특별";
        if (state == BunnyState.Intro || state == BunnyState.Flop)       return "⭐⭐ 희귀";
        return req == 0 ? "기본" : "일반";
    }

    public static string GetLevelName(int affinity)
    {
        if (affinity >= 88) return "Lv.6 전설의 민트 친구 🌟";
        if (affinity >= 72) return "Lv.5 가족이 된 민트 💖";
        if (affinity >= 55) return "Lv.4 무한 애정 민트 💕";
        if (affinity >= 35) return "Lv.3 단짝 친구 민트 🥰";
        if (affinity >= 10) return "Lv.2 마음을 여는 민트 🌸";
        return "Lv.1 낯가리는 민트 🌱";
    }

    public static string GetNextUnlockDescription(int affinity)
    {
        if (affinity < 5)  return "다음 해금: 간식 내놔! (5점) 🌾";
        if (affinity < 10) return "다음 해금: 반성의자 (10점) 🪑";
        if (affinity < 15) return "다음 해금: 세수하기 (15점) 🧼";
        if (affinity < 20) return "다음 해금: 화났어! (20점) 💢";
        if (affinity < 25) return "다음 해금: 호기심 민트 (25점) 👀";
        if (affinity < 30) return "다음 해금: 앞발 올리기 민트 (30점) 🐾";
        if (affinity < 35) return "다음 해금: ⭐⭐ 희귀 천사 민트 등장! (35점) ✨";
        if (affinity < 40) return "다음 해금: 토끼 가방 (40점) 🎒";
        if (affinity < 45) return "다음 해금: ⭐⭐ 희귀 발라당 눕기 (45점) 🛌";
        if (affinity < 50) return "다음 해금: 두발 서기 민트 (50점) 🙏";
        if (affinity < 55) return "다음 해금: ⭐⭐⭐ 특별 래빗키스 뽀뽀 (55점) 💋";
        if (affinity < 60) return "다음 해금: 아늑한 집 (60점) 🏠";
        if (affinity < 63) return "다음 해금: ⭐⭐⭐ 특별 빙키 점프 (63점) 🤸";
        if (affinity < 65) return "다음 해금: 화났어2 민트 (65점) 😾";
        if (affinity < 68) return "다음 해금: 환호 민트 (68점) 🙌";
        if (affinity < 72) return "다음 해금: ⭐⭐⭐⭐ 유니크 찐 화난 민트 (72점) 👿";
        if (affinity < 75) return "다음 해금: 턱괴기 민트 (75점) 😴";
        if (affinity < 80) return "다음 해금: ⭐⭐⭐⭐ 유니크 똘망똘망 정면 (80점) 🐰";
        if (affinity < 85) return "다음 해금: 현상수배 민트 (85점) 📜";
        if (affinity < 88) return "다음 해금: ⭐⭐⭐⭐⭐ 전설 실사 쓰다듬기 영상 (88점) 🖐️";
        if (affinity < 94) return "다음 해금: ⭐⭐⭐⭐⭐ 전설 어리둥절 실사 영상 (94점) 👀";
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
