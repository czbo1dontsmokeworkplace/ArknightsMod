using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionPresentationRules
{
    internal const int BroodMoltDuration = 150, BroodMoltBurst = 108, BoundaryReady = 510, DeathDuration = 200, DeathFinale = 180;
    internal static float MorphColor(int age) => EvolutionCinematicFrame.Smooth(140, 390, age);
    internal static float BoundaryFade(int age) => EvolutionCinematicFrame.Smooth(390, BoundaryReady, age);
    internal static bool DeathBurst(int age) => age >= 0 && age < DeathDuration && (age < 120 ? age % 6 == 0 : age % 3 == 0);
    internal static Vector2 DeathPoint(Vector2 center, int age, int index) => center +
        new Vector2(MathF.Sin(age * 1.71f + index * 2.1f) * (65 + age * .18f), MathF.Cos(age * 2.13f + index) * (80 + age * .14f));
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
                EvolutionArsenalVisuals.Burst(NPC.Center, EvolutionCinematics.Reduced ? 2 : 3.4f);
                EvolutionImpactSystem.Emit(NPC.Center, 1100, 7, false, 28);
                SoundEngine.PlaySound(SoundID.DD2_ExplosiveTrapExplode with { Volume = .8f, Pitch = -.6f }, NPC.Center);
            }
        }
        if (!EvolutionPresentationRules.DeathBurst(Timer) || lastDeathBurst >= Timer) return;
        lastDeathBurst = Timer;
        float power = MathHelper.Lerp(1.5f, 2.8f, Timer / (float)EvolutionPresentationRules.DeathDuration);
        int count = Timer >= 165 && !EvolutionCinematics.Reduced ? 2 : 1;
        for (int i = 0; i < count; i++) EvolutionArsenalVisuals.Burst(EvolutionPresentationRules.DeathPoint(NPC.Center, Timer, i), power);
        if (Timer % 12 == 0)
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .65f, Pitch = -.5f, MaxInstances = 3 }, NPC.Center);
        if (Timer % 24 == 0) EvolutionImpactSystem.Emit(NPC.Center, 500 + Timer * 2, 3, false, 20);
    }
    private void DrawDeathDisplay()
    {
        int age = Timer;
        if (age >= EvolutionPresentationRules.DeathFinale)
            EvolutionArsenalVisuals.Blast(NPC.Center, age - EvolutionPresentationRules.DeathFinale, 260);
        for (int past = Math.Max(0, age - 30); past <= Math.Min(age, EvolutionPresentationRules.DeathDuration - 1); past++)
        {
            if (!EvolutionPresentationRules.DeathBurst(past)) continue;
            int elapsed = age - past;
            float fade = 1 - elapsed / 31f;
            for (int i = 0; i < (past >= 165 && !EvolutionCinematics.Reduced ? 2 : 1); i++)
            {
                Vector2 point = EvolutionPresentationRules.DeathPoint(NPC.Center, past, i);
                EvolutionArsenalVisuals.Blast(point, elapsed, 75 + past * .4f);
                EvolutionVisuals.Ring(point, 20 + elapsed * (4 + past * .012f), 0, EvolutionVisuals.Core * (.45f * fade * fade), 2, false);
            }
        }
        float ending = EvolutionCinematicFrame.Smooth(165, 194, age) * (1 - EvolutionCinematicFrame.Smooth(194, 200, age));
        EvolutionVisuals.Glow(NPC.Center, EvolutionVisuals.Core * (.8f * ending), new Vector2(540, 32));
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
