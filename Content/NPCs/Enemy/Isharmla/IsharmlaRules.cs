using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public enum TideStage { Cantor = 1, Leviathan, Covenant, LastSong, LastTide, Finale }
public enum TideShot { Pearl, Seeker, Column, RainCloud, Rain, Wave, Breath, Gate, Lattice, Mine, Bubble, Typhoon, Tornado, Shark,
    Note, CharmNote, StormSpark, Bomb, MistField,
    Canticle, EchoSinger, WakeMark, FoamArc, Current, TideHeart }

public enum TideMotion { Breach, Broadside, Spiral, Gate, Undertow, AtomicBreath }

// All progression and geometry rules are independent of rendering and networking.
public static class IsharmlaRules
{
    public const int DragonSegments = 19;
    public const int ArrivalWarning = 300;
    public const int TransitionTime = 150;
    public const int FinaleTime = 960;
    public const float HumanRadius = 46;
    public const int HazardBudget = 420;
    public static int HumanPhraseLength(bool fierce) => fierce ? 330 : 420;
    public static int MotionLength(int pattern, bool final) => pattern switch
    {
        0 => 420, 1 => final ? 390 : 450, 2 => 480, 3 => 420, 4 => 420, _ => 510
    };
    public static float Envelope(float age, float start, float end, float fade = 24) =>
        Smooth((age - start) / fade) * Smooth((end - age) / fade);
    public static Vector2 BoundedAim(Vector2 aim) => new(
        MathHelper.Clamp(aim.X, 320, Main.maxTilesX * 16 - 320),
        MathHelper.Clamp(aim.Y, 500, Main.maxTilesY * 16 - 640));
    public const float BodyArtAngle = .2692f;
    public const float HeadArtAngle = .43f;
    public const float DragonScale = .82f;
    public static float SegmentSpan(int order) => (order >= 18 ? 231.4f : order == 17 ? 184 : order == 16 ? 206.7f : 232.4f) * DragonScale;
    public static float SegmentDistance(int order)
    {
        float distance = 58;
        for (int i = 1; i <= order; i++) distance += (SegmentSpan(i - 1) + SegmentSpan(i)) * .5f - 72;
        return distance;
    }
    public static Vector2 BoltSize(TideShot kind) => kind switch
    {
        TideShot.Rain => new Vector2(9, 23), TideShot.Seeker => new Vector2(20, 24),
        TideShot.Note or TideShot.CharmNote => new Vector2(22, 26), TideShot.StormSpark => new Vector2(26),
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
    public static bool IsLineShot(TideShot kind) => kind is TideShot.Column or TideShot.Breath or TideShot.Gate or TideShot.Lattice;
    public static bool AllowsLineShot(int total, int lines) => (lines + 1) * 5 <= total + 1;
    public static float FinaleOpacity(int timer, int transition) => 1 - Smooth((timer - 800) / 150f);
    // Offsets are frozen around the target when a phrase begins. No zone follows the player.
    public static Vector2[] FinaleOffsets(int pattern, int wave)
    {
        float turn = wave * MathHelper.PiOver2;
        if (pattern == 0) return new[] { new Vector2(-430, -300), new Vector2(430, -300), new Vector2(-430, 300), new Vector2(430, 300) };
        if (pattern == 1) return new[] { new Vector2(380, -270).RotatedBy(turn), new Vector2(520, 50).RotatedBy(turn),
            new Vector2(380, 270).RotatedBy(turn) };
        if (pattern == 2)
        {
            var result = new Vector2[6];
            for (int i = 0; i < result.Length; i++) result[i] = (turn + (i + 1.5f) * MathHelper.TwoPi / 8).ToRotationVector2() * 510;
            return result;
        }
        return new[] { new Vector2(-600, -260), new Vector2(-300, 260), new Vector2(300, -260), new Vector2(600, 260) };
    }
    public static int HitLimit(int life, int floor) => Math.Max(1, life - floor);
}
