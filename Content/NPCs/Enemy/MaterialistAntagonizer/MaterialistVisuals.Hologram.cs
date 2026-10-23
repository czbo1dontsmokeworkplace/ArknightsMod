using ArknightsMod.Content.Projectiles.Phalanx;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

internal static partial class MaterialistVisuals
{
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