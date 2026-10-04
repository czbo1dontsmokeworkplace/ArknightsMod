using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class EvolutionHazard
{
    private void UpdateRadiation(Evolution boss)
    {
        if (boss.Phase != 1) { Projectile.Kill(); return; }
        if (Age == FireAge && !Main.dedServ)
        {
            SoundEngine.PlaySound(SoundID.Item20 with { Volume = .6f, Pitch = -.6f }, Projectile.Center);
            EvolutionImpactSystem.Emit(Projectile.Center, 220, 1.5f);
        }
    }

    internal bool RadiationCollides(Rectangle box)
    {
        for (int wave = 0; wave < EvolutionNestRules.RippleCount; wave++)
        {
            int age = Age - FireAge - wave * EvolutionNestRules.RippleSpacing;
            if (!EvolutionNestRules.RippleActive(age)) continue;
            float radius = EvolutionNestRules.Radius(age);
            for (int ray = 0; ray < 5; ray++)
            {
                float angle = EvolutionNestRules.Direction((int)Parameter, ray);
                // 与绘制使用同一段弧线；扇区内侧、扇区间隙和射程以外均无隐藏伤害。
                for (int segment = 0; segment < 8; segment++)
                {
                    float a = angle - EvolutionNestRules.HalfAngle + EvolutionNestRules.HalfAngle * 2 * segment / 8;
                    float b = a + EvolutionNestRules.HalfAngle * 2 / 8;
                    float hit = 0;
                    if (Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(),
                        Projectile.Center + a.ToRotationVector2() * radius,
                        Projectile.Center + b.ToRotationVector2() * radius, 10, ref hit)) return true;
                }
            }
        }
        return false;
    }

    private void DrawRadiation()
    {
        if (Age < FireAge)
        {
            float fade = MathHelper.SmoothStep(0, 1, Math.Clamp(Age / 25f, 0, 1));
            fade *= Math.Clamp((FireAge - Age) / 14f, 0, 1);
            for (int ray = 0; ray < 5; ray++)
                for (int ring = 0; ring < 3; ring++)
                    DrawRadiationArc(EvolutionNestRules.Direction((int)Parameter, ray), 180 + ring * 165, fade * .25f, true);
            return;
        }
        for (int wave = 0; wave < EvolutionNestRules.RippleCount; wave++)
        {
            int age = Age - FireAge - wave * EvolutionNestRules.RippleSpacing;
            if (age < 0 || age >= EvolutionNestRules.RippleTravel + 8) continue;
            float opacity = age < EvolutionNestRules.RippleTravel ? 1 : (EvolutionNestRules.RippleTravel + 8 - age) / 8f;
            for (int ray = 0; ray < 5; ray++)
                DrawRadiationArc(EvolutionNestRules.Direction((int)Parameter, ray), EvolutionNestRules.Radius(age), opacity, false);
        }
    }

    private void DrawRadiationArc(float angle, float radius, float opacity, bool warning)
    {
        // 柔光贴图沿弧线重叠，不画矩形激光或填满整个三角扇区。
        for (int i = 0; i <= 48; i++)
        {
            float offset = MathHelper.Lerp(-EvolutionNestRules.HalfAngle, EvolutionNestRules.HalfAngle, i / 48f);
            Vector2 point = Projectile.Center + (angle + offset).ToRotationVector2() * radius;
            float taper = .6f + .4f * MathF.Sin(i / 48f * MathHelper.Pi);
            float spacing = radius * EvolutionNestRules.HalfAngle * 2 / 48;
            EvolutionVisuals.Glow(point, EvolutionVisuals.Blood * (.20f * opacity * taper), new Vector2(warning ? 12 : 30, spacing * 7), angle + offset);
            if (!warning) EvolutionVisuals.Glow(point, EvolutionVisuals.Core * (.3f * opacity * taper), new Vector2(12, spacing * 5), angle + offset);
        }
    }
}
