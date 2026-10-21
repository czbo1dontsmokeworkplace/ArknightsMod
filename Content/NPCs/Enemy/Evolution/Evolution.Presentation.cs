using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionPresentationRules
{
    internal const int BroodMoltDuration = 150, BroodMoltBurst = 108, BoundaryReady = 510, DeathDuration = 200, DeathFinale = 180;
    internal const float DeathScatterScale = 2, DeathExpansionScale = 2;
    internal static float MorphColor(int age) => EvolutionCinematicFrame.Smooth(140, 390, age);
    internal static float BoundaryFade(int age) => EvolutionCinematicFrame.Smooth(390, BoundaryReady, age);
    internal static bool DeathBurst(int age) => age >= 0 && age < DeathDuration && (age < 120 ? age % 5 == 0 : age % 2 == 0);
    internal static int DeathBurstCount(int age, bool reduced) => reduced ? 1 : age >= 156 ? 3 : age >= 90 ? 2 : 1;
    internal static Vector2 DeathPoint(Vector2 center, int age, int index) => center +
        new Vector2(MathF.Sin(age * 1.71f + index * 2.1f) * (65 + age * .18f), MathF.Cos(age * 2.13f + index) * (80 + age * .14f)) * DeathScatterScale;
    internal static float DeathBlastRadius(int age) => (75 + age * .4f) * DeathExpansionScale;
    internal static float DeathRingRadius(int age, int elapsed) => (20 + elapsed * (4 + age * .012f)) * DeathExpansionScale;
}

public sealed partial class Evolution
{
    private int lastDeathBurst = -1;
    private bool deathFinalePlayed;
    private void UpdateDeathDisplay()
    {
        if (Timer >= EvolutionPresentationRules.DeathFinale && !deathFinalePlayed)
        {
            deathFinalePlayed = true;
            if (Timer < EvolutionPresentationRules.DeathFinale + 8)
            {
                EvolutionArsenalVisuals.Burst(NPC.Center, EvolutionCinematics.Reduced ? 2 : 4.4f, EvolutionPresentationRules.DeathExpansionScale);
                EvolutionImpactSystem.Emit(NPC.Center, 1450 * EvolutionPresentationRules.DeathExpansionScale, 9, false, 36);
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = .8f, Pitch = -.6f }, NPC.Center);
            }
        }
        if (!EvolutionPresentationRules.DeathBurst(Timer) || lastDeathBurst >= Timer) return;
        lastDeathBurst = Timer;
        float power = MathHelper.Lerp(1.7f, 3.6f, Timer / (float)EvolutionPresentationRules.DeathDuration);
        int count = EvolutionPresentationRules.DeathBurstCount(Timer, EvolutionCinematics.Reduced);
        for (int i = 0; i < count; i++) EvolutionArsenalVisuals.Burst(EvolutionPresentationRules.DeathPoint(NPC.Center, Timer, i), power, EvolutionPresentationRules.DeathExpansionScale);
        if (Timer % 12 == 0)
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .65f, Pitch = -.5f, MaxInstances = 3 }, NPC.Center);
        if (Timer % 24 == 0) EvolutionImpactSystem.Emit(NPC.Center, (500 + Timer * 2) * EvolutionPresentationRules.DeathExpansionScale, 3, false, 20);
    }
    private void DrawDeathDisplay()
    {
        int age = Timer;
        DrawTerminalVessels(age);
        if (age >= EvolutionPresentationRules.DeathFinale)
            EvolutionArsenalVisuals.Blast(NPC.Center, age - EvolutionPresentationRules.DeathFinale, 380 * EvolutionPresentationRules.DeathExpansionScale);
        for (int past = Math.Max(0, age - 30); past <= Math.Min(age, EvolutionPresentationRules.DeathDuration - 1); past++)
        {
            if (!EvolutionPresentationRules.DeathBurst(past)) continue;
            int elapsed = age - past;
            float fade = 1 - elapsed / 31f;
            for (int i = 0; i < EvolutionPresentationRules.DeathBurstCount(past, EvolutionCinematics.Reduced); i++)
            {
                Vector2 point = EvolutionPresentationRules.DeathPoint(NPC.Center, past, i);
                EvolutionArsenalVisuals.Blast(point, elapsed, EvolutionPresentationRules.DeathBlastRadius(past));
                EvolutionVisuals.Ring(point, EvolutionPresentationRules.DeathRingRadius(past, elapsed), 0, EvolutionVisuals.Core * (.45f * fade * fade), 2, false);
            }
        }
        float ending = EvolutionCinematicFrame.Smooth(165, 194, age) * (1 - EvolutionCinematicFrame.Smooth(194, 200, age));
        EvolutionVisuals.Glow(NPC.Center, EvolutionVisuals.Core * (.8f * ending), new Vector2(540, 32));
    }
    private void DrawTerminalVessels(int age)
    {
        float build = EvolutionCinematicFrame.Smooth(12, 176, age);
        float collapse = 1 - EvolutionCinematicFrame.Smooth(180, 196, age);
        float beat = .75f + .25f * MathF.Sin(age * (.11f + build * .14f));
        int veins = EvolutionCinematics.Reduced ? 4 : 12;
        for (int i = 0; i < veins; i++)
        {
            float angle = i * MathHelper.TwoPi / veins + MathF.Sin(age * .019f + i) * .19f;
            Vector2 d = angle.ToRotationVector2();
            Vector2 from = NPC.Center + d * (220 + (1 - build) * 260);
            Vector2 to = NPC.Center + d * (22 + (1 - build) * 80);
            EvolutionVisuals.Tendril(from, to, MathF.Sin(i * 2.3f + age * .08f) * (24 + build * 48), age + i * 19,
                5 + build * 12, build * collapse * (.35f + beat * .4f));
        }
        EvolutionVisuals.Glow(NPC.Center, EvolutionVisuals.Blood * (build * beat * collapse), new Vector2(110 + 180 * build));
        EvolutionVisuals.Glow(NPC.Center, EvolutionVisuals.Core * (build * build * collapse * .8f), new Vector2(28 + 45 * beat));
    }
}

public abstract partial class EvolutionBroodNPC
{
    internal bool Molting;
    private bool moltBurstPlayed;
    private int farFromPlayersTicks;
    internal int MoltAge => EvolutionPresentationRules.BroodMoltDuration + (int)NPC.ai[3];
    internal Color BroodOutlineColor => Parent?.ThemeColor ?? EvolutionVisuals.Sand;
    private bool RetireDistantBrood(Evolution boss)
    {
        if (SupportOnly || Retiring || boss.Phase != 3) return false;
        bool nearby = false;
        foreach (Player player in Main.ActivePlayers)
            if (!player.dead && NPC.DistanceSQ(player.Center) <= 1500 * 1500) { nearby = true; break; }
        farFromPlayersTicks = nearby ? 0 : farFromPlayersTicks + 1;
        if (farFromPlayersTicks < 45 || Main.netMode == NetmodeID.MultiplayerClient) return false;
        NPC.ai[3] = -24; NPC.damage = 0; NPC.dontTakeDamage = true; NPC.netUpdate = true;
        return true;
    }
    private void DoBroodMolt()
    {
        int age = MoltAge;
        NPC.dontTakeDamage = true; NPC.noGravity = NPC.noTileCollide = false;
        NPC.damage = 0; NPC.velocity.X *= .7f;
        NPC.alpha = age < EvolutionPresentationRules.BroodMoltBurst ? 0 : Math.Min(255, (age - EvolutionPresentationRules.BroodMoltBurst) * 14);
        if (!Main.dedServ && age >= EvolutionPresentationRules.BroodMoltBurst && !moltBurstPlayed)
        {
            moltBurstPlayed = true;
            if (age < EvolutionPresentationRules.BroodMoltBurst + 8)
            {
                EvolutionBroodDeathVisuals.Emit(NPC.Center, NPC.Size * 1.4f, BroodOutlineColor);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = .3f, Pitch = -.45f, MaxInstances = 3 }, NPC.Center);
            }
        }
        NPC.ai[3]++;
        if (NPC.ai[3] >= 0) NPC.active = false;
    }
}
