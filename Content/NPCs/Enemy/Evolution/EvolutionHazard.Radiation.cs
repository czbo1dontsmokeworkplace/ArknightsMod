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
        UpdateContinuousLaserDust();
        if (Age == FireAge && !Main.dedServ)
        {
            SoundEngine.PlaySound(SoundID.Zombie104 with { Volume = .6f, Pitch = .15f }, Projectile.Center);
            EvolutionVisuals.Burst(Projectile.Center, 1.15f, false, EvolutionVisuals.Sand);
            EvolutionImpactSystem.Emit(Projectile.Center, 260, 2);
            // 实体能量冲击波随点亮推出；只有装饰波纹，原8帧伤害窗不延长。
            EvolutionImpactSystem.Emit(Projectile.Center, 760, 3.5f, false, 42);
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
        float flash = EvolutionRules.BeamEnvelope(fired,EvolutionNestRules.LaserActiveTicks,EvolutionNestRules.LaserFadeTicks,true);
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
        }
        if(!EvolutionLaserVisuals.WorldMaterialReady) DrawContinuousBodies();
        if (flash > 0)
        {
            EvolutionVisuals.Glow(Projectile.Center, EvolutionVisuals.SandCore * flash, new Vector2(160));
            EvolutionLaserVisuals.DrawMuzzle(Projectile.Center,-Vector2.UnitY,flash,60,fired,EvolutionVisuals.SandCore);
            if(fired>=0&&fired<42)
                EvolutionVisuals.Ring(Projectile.Center,65+fired*15,0,EvolutionVisuals.Sand*((1-fired/42f)*.65f),3,false);
        }
    }
}
