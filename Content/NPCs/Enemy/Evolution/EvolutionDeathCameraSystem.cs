using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionDeathCameraRules
{
    internal const int PanIn = 24, PanOut = 36;
    internal const int VisualEnd = EvolutionPresentationRules.DeathFinale + EvolutionBloodSpectacleRules.FinaleLife;
    internal static Vector2 FocusPosition(Vector2 center, Vector2 viewport, Vector2 worldSize) =>
        Vector2.Clamp(center - viewport * .5f, Vector2.Zero, Vector2.Max(Vector2.Zero, worldSize - viewport));
}

// Only a view target is retained. Return blends against the live player camera, so moving
// during the finale cannot send the view back to an obsolete position.
internal struct EvolutionDeathCameraPlayback
{
    internal Vector2 Center;
    internal float Weight;
    internal int LastAge;
    private int entrance, tail, returning;
    private float releaseWeight;
    private bool following;
    internal void Update(bool active, Vector2 center, int age, bool cancel = false, int finale = EvolutionPresentationRules.DeathFinale, int visualEnd = EvolutionDeathCameraRules.VisualEnd)
    {
        if (cancel) { this = default; return; }
        if (active)
        {
            following = true; Center = center; LastAge = Math.Max(LastAge, age);
            Weight = Math.Max(Weight, EvolutionCinematicFrame.Smooth(0, EvolutionDeathCameraRules.PanIn, ++entrance));
            tail = returning = 0;
            return;
        }
        if (following)
        {
            following = false; releaseWeight = Weight;
            tail = LastAge >= finale ? Math.Max(0, visualEnd - LastAge) : 0;
            returning = 0;
        }
        if (tail > 0)
        {
            // A late-arriving snapshot can still finish its pan during the explosion's aftershock.
            Weight = Math.Max(Weight, EvolutionCinematicFrame.Smooth(0, EvolutionDeathCameraRules.PanIn, ++entrance));
            releaseWeight = Weight; tail--; LastAge++; return;
        }
        Weight = releaseWeight * (1 - EvolutionCinematicFrame.Smooth(0, EvolutionDeathCameraRules.PanOut, ++returning));
        if (Weight <= .0001f) this = default;
    }
    internal readonly Vector2 Apply(Vector2 playerCamera, Vector2 viewport, Vector2 worldSize) =>
        Vector2.Lerp(playerCamera, EvolutionDeathCameraRules.FocusPosition(Center, viewport, worldSize), Weight);
}

public sealed class EvolutionDeathCameraSystem : ModSystem
{
    private EvolutionDeathCameraPlayback playback;
    private int encounter = -1;
    public override void PostUpdateEverything()
    {
        if (Main.dedServ) return;
        if (Main.gameMenu || !Main.LocalPlayer.active || Main.LocalPlayer.dead ||
            ModContent.GetInstance<EvolutionCinematicConfig>()?.DeathCamera == false)
        { playback = default; encounter = -1; return; }
        Evolution selected = null;
        float nearest = 2800 * 2800;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (npc.ModNPC is not Evolution boss || boss.Phase != 6) continue;
            if (boss.Encounter == encounter && playback.Weight > 0) { selected = boss; break; }
            float distance = npc.DistanceSQ(Main.LocalPlayer.Center);
            if (distance < nearest) { selected = boss; nearest = distance; }
        }
        if (selected != null && selected.Encounter != encounter) { playback = default; encounter = selected.Encounter; }
        playback.Update(selected != null, selected?.NPC.Center ?? playback.Center, selected?.Timer ?? playback.LastAge);
        if (playback.Weight == 0) encounter = -1;
    }
    public override void ModifyScreenPosition()
    {
        if (Main.dedServ || Main.gameMenu || Main.LocalPlayer.dead || playback.Weight <= 0) return;
        Main.screenPosition = playback.Apply(Main.screenPosition, new Vector2(Main.screenWidth, Main.screenHeight),
            new Vector2(Main.maxTilesX * 16, Main.maxTilesY * 16));
        if (!EvolutionCinematics.Reduced && EvolutionCinematics.ShakeEnabled)
        {
            float age = playback.LastAge;
            float force = age < EvolutionPresentationRules.DeathLead ? 0 : age < EvolutionPresentationRules.DeathFinale ? MathHelper.Lerp(1, 5, Math.Clamp((age - EvolutionPresentationRules.DeathLead) / 180f, 0, 1)) :
                9 * (1 - EvolutionCinematicFrame.Smooth(EvolutionPresentationRules.DeathFinale, EvolutionDeathCameraRules.VisualEnd, age));
            Main.screenPosition += new Vector2(MathF.Sin(age * 2.43f), MathF.Cos(age * 1.83f)) * (force * playback.Weight * EvolutionPresentationRules.DeathIntensity);
        }
    }
    public override void OnWorldUnload() { playback = default; encounter = -1; }
    public override void Unload() => OnWorldUnload();
}
