using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class EvolutionHazard
{
    // 保留弹种编号和伤害驾驶舱入口；原来的五组弧线已替换为五条瞬发激光。
    private void UpdateRadiation(Evolution boss)
    {
        if (boss.Phase != 1) { Projectile.Kill(); return; }
        if (Age == FireAge && !Main.dedServ)
        {
            SoundEngine.PlaySound(SoundID.Zombie104 with { Volume = .6f, Pitch = .15f }, Projectile.Center);
            EvolutionVisuals.Burst(Projectile.Center, 1.15f, false, EvolutionVisuals.Sand);
            EvolutionImpactSystem.Emit(Projectile.Center, 260, 2);
        }
    }

    internal bool RadiationCollides(Rectangle box)
    {
        for (int ray = 0; ray < 5; ray++)
        {
            Vector2 end = Projectile.Center + EvolutionNestRules.Direction((int)Parameter, ray).ToRotationVector2() * EvolutionNestRules.RadiationReach;
            float hit = 0;
            if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), Projectile.Center, end,
                EvolutionNestRules.LaserWidth, ref hit)) return true;
        }
        return false;
    }

    private void DrawRadiation()
    {
        float warning = MathHelper.SmoothStep(0, 1, Math.Clamp(Age / 20f, 0, 1)) *
            (1 - MathHelper.SmoothStep(0, 1, Math.Clamp((Age - Delay) / 16f, 0, 1)));
        int fired = Age - FireAge;
        float flash = fired < 0 ? 0 : MathF.Pow(Math.Clamp(1 - fired / (float)EvolutionNestRules.LaserFadeTicks, 0, 1), .7f);
        for (int ray = 0; ray < 5; ray++)
        {
            Vector2 direction = EvolutionNestRules.Direction((int)Parameter, ray).ToRotationVector2();
            Vector2 end = Projectile.Center + direction * EvolutionNestRules.RadiationReach;
            if (warning > .001f)
            {
                Vector2 edge = direction.RotatedBy(MathHelper.PiOver2) * (EvolutionNestRules.LaserWidth * .5f);
                EvolutionVisuals.Line(Projectile.Center, end, EvolutionVisuals.Sand * (.8f * warning), 2);
                EvolutionVisuals.Line(Projectile.Center + edge, end + edge, EvolutionVisuals.Sand * (.3f * warning), 1, false);
                EvolutionVisuals.Line(Projectile.Center - edge, end - edge, EvolutionVisuals.Sand * (.3f * warning), 1, false);
            }
            if (flash > 0)
                EvolutionProjectileVisuals.DrawBeam(Projectile.Center, end, fired, flash,
                    widthScale: 4.4f, tint: EvolutionVisuals.Sand, hotTint: EvolutionVisuals.SandCore);
        }
        if (flash > 0) EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.SandCore * flash, new Vector2(160));
    }
}
