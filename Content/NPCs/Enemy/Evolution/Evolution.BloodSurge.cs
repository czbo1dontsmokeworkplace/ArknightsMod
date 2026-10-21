using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// Extra figures occupy their own turns. Existing attacks keep their original damage and timing.
internal static class EvolutionBloodSurgeRules
{
    internal const int Weave = 23, Echo = 24, Duration = 280;
    internal const int WeaveWarning = 52, EchoWarning = 48;
    internal const float WeaveSpacing = 176, EchoGap = .56f;
    internal static bool IsFigure(int attack) => attack is Weave or Echo;
    internal static int Wave(int attack, int time) => attack == Weave ? time is 18 or 112 ? (time - 18) / 94 : -1 :
        attack == Echo && time is 18 or 102 ? (time - 18) / 84 : -1;
    internal static bool WeaveLane(int lane) => Math.Abs(lane) is >= 2 and <= 5;
    internal static bool EchoLane(int lane) => MathF.Abs(MathHelper.WrapAngle(lane * MathHelper.TwoPi / 18)) >= EchoGap;
    internal static Vector2 EchoDirection(float gapAngle, int lane) => (gapAngle + lane * MathHelper.TwoPi / 18).ToRotationVector2();
}

public sealed partial class Evolution
{
    private void DoBloodSurge()
    {
        int wave = EvolutionBloodSurgeRules.Wave(Attack, Timer);
        if (wave < 0) return;
        if (Attack == EvolutionBloodSurgeRules.Weave)
        {
            // Both waves share a stationary 500px central passage. Bottom wave is delayed until
            // the first wave has cleared the middle of the flight band; no retargeting.
            bool bottom = wave == 1;
            Vector2 direction = bottom ? -Vector2.UnitY : Vector2.UnitY;
            for (int lane = -5; lane <= 5; lane++)
            {
                if (!EvolutionBloodSurgeRules.WeaveLane(lane)) continue;
                Vector2 origin = new(PatternOrigin.X + lane * EvolutionBloodSurgeRules.WeaveSpacing,
                    bottom ? CorridorBottom - 36 : CorridorTop + 36);
                Shoot(EvolutionShot.Lance, origin, direction * 10, EvolutionRules.CorridorHeight - 72,
                    EvolutionBloodSurgeRules.WeaveWarning, 168, blade: true, quietLaunch: true);
            }
            EvolutionBloodSpectacleSystem.Emit(NPC.Center, direction, EvolutionBloodCue.Weave, ThemeColor, 1.15f);
        }
        else
        {
            // Echoes originate well to the side, always leaving the same cone towards the
            // snapshotted player position. This is a pair of expanding rings, not a tracking cage.
            int side = (int)NPC.ai[2] % 2 == 0 ? -1 : 1;
            Vector2 origin = new(PatternOrigin.X + side * 600, Math.Clamp(PatternOrigin.Y, CorridorTop + 170, CorridorBottom - 170));
            float angle = (PatternOrigin - origin).ToRotation();
            for (int lane = 0; lane < 18; lane++)
            {
                if (!EvolutionBloodSurgeRules.EchoLane(lane)) continue;
                Vector2 direction = EvolutionBloodSurgeRules.EchoDirection(angle, lane);
                Shoot(EvolutionShot.Barrage, origin + direction * 72, direction * (wave == 0 ? 6.2f : 7.2f),
                    delay: EvolutionBloodSurgeRules.EchoWarning, lifetime: 150, quietLaunch: true);
            }
            EvolutionBloodSpectacleSystem.Emit(origin, angle.ToRotationVector2(), EvolutionBloodCue.Echo, ThemeColor, 1.2f);
        }
    }
}
