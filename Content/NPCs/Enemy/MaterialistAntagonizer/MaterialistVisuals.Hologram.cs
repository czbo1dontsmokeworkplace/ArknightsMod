using ArknightsMod.Content.Projectiles.Phalanx;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
    internal static void DrawFrostShield(NPC npc, float progress)
    {
        float opacity = MaterialistFormation.Smooth(progress) * (1 - npc.alpha / 255f);
        Vector2 size = new Vector2(npc.width + 48, npc.height + 48) * npc.scale;
        Color color = new Color(112, 217, 255);
        if (!PhalanxVisuals.ShieldSurface(npc.Center, size, color, opacity))
            Glow(npc.Center, color * (.18f * opacity), size);
        MaterialistTechRenderer.LockShieldMaterial(npc.Center, size * .98f, 0, progress, color * (.27f * opacity));
    }

    private static void DrawLockShield(MaterialistAntagonizer boss)
    {
        float progress = boss.LockShieldProgress;
        if (progress <= .001f) return;
        float opacity = MaterialistFormation.Smooth(progress);
        Vector2 center = boss.NPC.Center, size = boss.LockShieldSize;
        Color color = Color.Lerp(new Color(94, 204, 255), new Color(165, 152, 255), (boss.Phase - 1) / 2f);
        // Reuse the spherical barrier surface; keep the carrier's own hologram as a restrained overlay.
        if (!PhalanxVisuals.ShieldSurface(center, size, color, opacity))
            Glow(center, color * (.18f * opacity), size);
        MaterialistTechRenderer.LockShieldMaterial(center, size * .98f, 0, progress, color * (.27f * opacity));
    }
}
