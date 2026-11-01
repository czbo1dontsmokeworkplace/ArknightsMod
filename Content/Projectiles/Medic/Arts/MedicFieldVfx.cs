using System;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.GameContent;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

// Life Jelly composition: expanding opening, steady circular boundary,
// independent rim/interior dust, custom motes, then a closing fade.
internal static class MedicFieldVfx
{
    internal const string BarrierPath = "ArknightsMod/Content/Textures/MedicVfx/MedicalBarrier";
    internal static float Radius(MedicHealingField field) => field.Profile.FieldRadius * Math.Min(1, field.Age / 14f);
    internal static void Draw(MedicHealingField field)
    {
        var p = field.Profile;
        Vector2 center = field.Projectile.Center;
        float radius = Radius(field), visibility = field.Visibility;
        var barrier = MedicArtsVfx.Texture(BarrierPath);
        // All spatial scales are circular, with no perspective squash or wings.
        Terraria.Main.EntitySpriteDraw(barrier, center - Main.screenPosition, null,
            MedicArtsVfx.Light(p.Body, .65f * visibility), 0, barrier.Size() * .5f,
            new Vector2(radius * 2) / barrier.Size(), Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
        Circle(center, radius, p.Body, 1.5f, .8f * visibility);
        float pulse = .5f + .5f * MathF.Sin(field.Age * .055f);
        Circle(center, radius * (.87f + pulse * .025f), p.Core, .8f, .24f * visibility);
        // Distinct radial motifs preserve the same circle at every angle.
        int petals = field.ProfileId switch { 10 => 6, 11 => 8, 12 => 12, _ => 16 };
        for (int i = 0; i < petals; i++)
        {
            float angle = i * MathHelper.TwoPi / petals + field.Age * (field.ProfileId == 11 ? .009f : -.002f);
            Vector2 radial = angle.ToRotationVector2();
            Vector2 at = center + radial * radius * .94f;
            MedicArtsVfx.Glow(at, new Vector2(field.ProfileId == 13 ? 7 : 5), p.Core, .4f * visibility);
            if (field.ProfileId == 12)
                MedicArtsVfx.Thread(center + radial * radius * .90f, center + radial * radius * .97f, p.Core, .8f, .35f * visibility);
        }
    }
    private static void Circle(Vector2 center, float radius, Color color, float width, float opacity)
    {
        const int segments = 240;
        Vector2 previous = center + Vector2.UnitX * radius;
        for (int i = 1; i <= segments; i++)
        {
            Vector2 point = center + (i * MathHelper.TwoPi / segments).ToRotationVector2() * radius;
            Vector2 delta = point - previous;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, previous - Main.screenPosition, new Rectangle(0, 0, 1, 1),
                MedicArtsVfx.Light(color, opacity), delta.ToRotation(), new Vector2(0, .5f),
                new Vector2(delta.Length() + .3f, width), SpriteEffects.None);
            previous = point;
        }
    }
    internal static void Ambient(MedicHealingField field)
    {
        if (Main.dedServ || field.Projectile.Center.DistanceSQ(Main.screenPosition + new Vector2(Main.screenWidth, Main.screenHeight) * .5f) > 2000 * 2000) return;
        var p = field.Profile;
        Vector2 center = field.Projectile.Center;
        float radius = Radius(field);
        if (field.Age % 3 == 0)
        {
            // Diametrically opposed pairs prevent a lopsided Nightingale boundary.
            Vector2 radial = Main.rand.NextFloat(MathHelper.TwoPi).ToRotationVector2();
            foreach (int sign in new[] { -1, 1 })
            {
                Vector2 at = center + radial * (radius * sign);
                Dust rim = Dust.NewDustPerfect(at, DustID.TintableDustLighted,
                    radial.RotatedBy(MathHelper.PiOver2) * (sign * .35f), 140, p.Body, 1.05f);
                rim.noGravity = true;
                MedicArtsVfx.Mote(at, radial.RotatedBy(MathHelper.PiOver2) * (sign * .3f), field.ProfileId,
                    MedicalMoteKind.Pearl, 4.5f, 34);
            }
        }
        if (field.Age % 5 == 0)
        {
            Vector2 offset = Main.rand.NextVector2Circular(radius * .85f, radius * .85f);
            foreach (int sign in new[] { -1, 1 })
            {
                Dust inner = Dust.NewDustPerfect(center + offset * sign, DustID.TintableDustLighted, -Vector2.UnitY * .2f, 180, p.Core, .65f);
                inner.noGravity = true;
                MedicArtsVfx.Mote(center + offset * sign, -Vector2.UnitY * .15f, field.ProfileId, MedicalMoteKind.Petal, 4, 45);
            }
        }
        if (field.Age is 1 or 14 || field.Age % 90 == 14)
            MedicArtsVfx.Mote(center, Vector2.Zero, field.ProfileId, MedicalMoteKind.Pulse, radius * .9f, 28);
    }
}
