using System;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Terraria;
using ArknightsMod.Content.Projectiles.Guard.Blaze;

namespace ArknightsMod.Content.Projectiles.Guard;

internal static class CenturionHitVisuals
{
    internal const float SpecterIntensity = 0.8f;
    internal const float BlazeIntensity = 0.9f;
    internal const float GavialIntensity = 1f;
    internal static readonly Color SpecterColor = new(150, 150, 150);
    internal static readonly Color BlazeColor = new(224, 22, 38);
    internal static readonly Color GavialColor = new(20, 76, 38);

    internal static void BloodSpray(NPC target, Vector2 forward, float intensity, int hitIndex, Color tint, float spatialScale = 1)
    {
        if (Main.dedServ) return;
        // 沿世界上方抬高 25 度；朝左、朝右都往上翘，已经向上时不越过竖直方向。
        float angle = forward.SafeNormalize(Vector2.UnitX).ToRotation();
        float lift = MathHelper.Clamp(MathHelper.WrapAngle(-MathHelper.PiOver2 - angle),
            -MathHelper.ToRadians(25f), MathHelper.ToRadians(25f));
        Vector2 direction = (angle + lift).ToRotationVector2();
        // A 的短程亮屑和 C 的长程碎光统一运动参数，混在同一束中。
        int count = Math.Max(3, (int)MathF.Round(Math.Max(4, 12 - hitIndex * 2) * intensity));
        for (int i = 0; i < count; i++)
        {
            bool solidSpark = Main.rand.NextBool();
            Vector2 position = target.Center
                + Main.rand.NextVector2Circular(Math.Min(12f, target.width * 0.2f),
                    Math.Min(12f, target.height * 0.2f)) * spatialScale;
            Vector2 sparkVelocity = direction.RotatedByRandom(0.65f)
                * Main.rand.NextFloat(7f, 17f) * intensity * spatialScale;
            int lifetime = Main.rand.Next(18, 27);
            Color accent = tint == BlazeColor ? new Color(255, 155, 65)
                : tint == GavialColor ? new Color(117, 151, 67) : new Color(205, 205, 205);
            Color color = Main.rand.NextBool(3) ? accent : Color.Lerp(tint, tint * 0.65f, Main.rand.NextFloat());
            if (solidSpark)
                new T_Centurion_Blood_Spark_Particle(position, sparkVelocity,
                    lifetime, Main.rand.NextFloat(0.42f, 0.7f) * intensity * spatialScale, color).Spawn();
            else
                new T_Centurion_Blood_Line_Particle(position, sparkVelocity,
                    lifetime, Main.rand.NextFloat(1f, 1.5f) * intensity * spatialScale, color).Spawn();
        }
    }

    internal static void SawEjecta(Projectile saw, Vector2 muzzle, Vector2 forward,
        float damageRatio = 0.24f, float power = 1f)
    {
        BlazeScarletEjecta.SpawnCone(saw.GetSource_FromThis(), muzzle, forward, 1,
            MathHelper.ToRadians(21f), 8f, 12.5f,
            Math.Max(1, (int)MathF.Round(saw.damage * damageRatio)),
            saw.knockBack * 0.22f, saw.owner, power);
    }
}
