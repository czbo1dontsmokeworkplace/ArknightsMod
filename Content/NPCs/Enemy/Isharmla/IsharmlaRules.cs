using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public enum TideStage { Cantor = 1, Leviathan, Covenant, LastSong, LastTide, Finale }
public enum TideShot { Pearl, Seeker, Column, RainCloud, Rain, Wave, Breath, Gate, Lattice, Mine }

// All progression and geometry rules are independent of rendering and networking.
public static class IsharmlaRules
{
    public const int DragonSegments = 19;
    public const int ArrivalWarning = 300;
    public const int TransitionTime = 150;
    public const int FinaleTime = 2160;
    public const float HumanRadius = 69;
    public const float BodyArtAngle = .2692f;
    public const float HeadArtAngle = .43f;
    public const float DragonScale = .82f;
    public static float SegmentSpan(int order) => (order >= 18 ? 231.4f : order == 17 ? 184 : order == 16 ? 206.7f : 232.4f) * DragonScale;
    public static float SegmentDistance(int order)
    {
        float distance = 58;
        for (int i = 1; i <= order; i++) distance += (SegmentSpan(i - 1) + SegmentSpan(i)) * .5f - 52;
        return distance;
    }
    public static Vector2 BoltSize(TideShot kind) => kind switch
    {
        TideShot.Rain => new Vector2(9, 23), TideShot.Seeker => new Vector2(20, 24),
        _ => new Vector2(18, 18)
    };
    public static int HumanFloor(int maximum, TideStage stage) => stage switch
    {
        TideStage.Cantor or TideStage.Leviathan => (int)(maximum * .8f),
        TideStage.Covenant => maximum / 2,
        _ => 1
    };
    public static int DragonFloor(int maximum, TideStage stage) => stage switch
    {
        TideStage.Cantor or TideStage.Leviathan => (int)(maximum * .8f),
        TideStage.Covenant or TideStage.LastSong => maximum / 2,
        _ => 0
    };
    public static bool HumanVulnerable(TideStage stage) => stage is TideStage.Cantor or TideStage.Covenant or TideStage.LastSong;
    public static bool DragonVulnerable(TideStage stage) => stage is TideStage.Leviathan or TideStage.Covenant or TideStage.LastTide;
    public static float BreathTurnRate(float distance) => MathHelper.Lerp(.003f, .018f, MathHelper.Clamp((distance - 260) / 1100, 0, 1));
    public static Vector2 Attach(Vector2 joint, float angle, Vector2 artAnchor, Vector2 origin, float scale) =>
        joint - (artAnchor - origin).RotatedBy(angle) * scale;
    public static float Smooth(float v) => MathHelper.SmoothStep(0, 1, MathHelper.Clamp(v, 0, 1));
    public static int FinalePattern(int timer) => Math.Clamp(timer / 540, 0, 3);
    public static int HitLimit(int life, int floor) => Math.Max(1, life - floor);
}
