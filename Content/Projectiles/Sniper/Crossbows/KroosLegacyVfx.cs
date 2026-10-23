using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Sniper.KroosAlter;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

// Shared legacy textured trail and rings. Branch variants change only the palette; projectile AI stays separate.
internal static class KroosLegacyVfx
{
    internal static bool Handles(CrossbowKind kind) => (uint)kind <= (uint)CrossbowKind.BluePoison;

    // Normal Kroos inherits the orange skill; normal Keen Glint inherits the yellow/green skill.
    // Use the shot snapshot so switching weapons or skills cannot recolour arrows in flight.
    internal static int Style(Projectile p, CrossbowKind kind)
    {
        if (p.ModProjectile is CrossbowPulse) return (int)p.ai[2];
        if (kind is not (CrossbowKind.Kroos or CrossbowKind.KroosAlter)) return (int)kind + 3;
        int skill = p.GetGlobalProjectile<CrossbowShotData>().Skill;
        return kind == CrossbowKind.Kroos || skill == 1 ? 1 : 2;
    }

    // IDs 1/2 retain Kroos's original skills; 3+ encode a fixed weapon palette.
    internal static Color EffectColor(int style) => style switch {
        1 => new(204, 126, 57), 2 => new(9, 161, 130),
        (int)CrossbowKind.Melanite + 3 => new(171, 99, 240),
        (int)CrossbowKind.Provence + 3 => new(240, 157, 69),
        (int)CrossbowKind.Schwarz + 3 => new(139, 69, 210),
        (int)CrossbowKind.GreyThroat + 3 => new(145, 205, 240),
        (int)CrossbowKind.BluePoison + 3 => new(60, 210, 153),
        (int)CrossbowKind.Pozemka + 3 => new(235, 101, 170),
        _ => Color.DarkOrange
    };

    internal static void Draw(Projectile p, CrossbowKind kind, Vector2[] history, bool drawArrow)
    {
        if (Main.dedServ) return;
        int style = Style(p, kind);
        bool green = style != 1;
        Vector2[] positions = history ?? p.oldPos;
        Vector2 offset = history == null ? p.Size * .5f : Vector2.Zero;
        int length = System.Math.Min(15, positions.Length);
        var vertices = new List<VertexData>(length * 2);
        float time = Main.GlobalTimeWrappedHourly % 1f;
        Vector2 previous = p.Center;
        for (int i = 0; i < length; i++)
        {
            if (positions[i] == Vector2.Zero) break;
            Vector2 center = positions[i] + offset;
            if (Vector2.DistanceSquared(previous, center) > 160f * 160f) break;
            Vector2 tangent = (previous - center).SafeNormalize(p.velocity.SafeNormalize(Vector2.UnitX));
            Vector2 side = tangent.RotatedBy(MathHelper.PiOver2) * (green ? 32f : 16f);
            float uv = i / 14f;
            Color tail = EffectColor(style);
            Color head = style == 2 ? new Color(255, 237, 0) : Color.Lerp(tail, Color.White, .65f);
            Color color = green ? Color.Lerp(head, tail,
                MathHelper.Clamp(i / 4f, 0f, 1f)) : Color.DarkOrange;
            float alpha = green && i < 3 ? i / 2f : 1f - i / 15f;
            color *= alpha * p.Opacity;
            float u = green ? (uv - time * 2f) % 1f : uv;
            vertices.Add(new VertexData(center - Main.screenPosition + side, new Vector3(u, 0, 1), color));
            vertices.Add(new VertexData(center - Main.screenPosition - side, new Vector3(u, 1, 1), color));
            previous = center;
        }
        if (vertices.Count >= 4)
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicWrap,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            try
            {
                Main.graphics.GraphicsDevice.Textures[0] = TextureAssets.Extra[196].Value;
                VertexData[] strip = vertices.ToArray();
                for (int pass = 0; pass < 2; pass++)
                    Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, strip, 0, strip.Length - 2);
            }
            finally
            {
                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
                    DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            }
        }
        if (!drawArrow) return;
        if (kind is not (CrossbowKind.Kroos or CrossbowKind.KroosAlter))
        {
            CrossbowSignatureVfx.DrawNativeCore(p, kind);
            return;
        }
        Texture2D arrow = ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Projectiles/Sniper/KroosAlter/KroosAlterCrossbow_Arrow").Value;
        float rotation = p.velocity.ToRotation() + MathHelper.PiOver2;
        Main.EntitySpriteDraw(arrow, p.Center - Main.screenPosition, null, Color.White * p.Opacity,
            rotation, new Vector2(arrow.Width / 2f, 0), p.scale, SpriteEffects.None);
        if (green)
        {
            Texture2D glow = ModContent.Request<Texture2D>(
                "ArknightsMod/Content/Projectiles/Sniper/KroosAlter/KroosAlterCrossbow_ArrowGlow").Value;
            Main.EntitySpriteDraw(glow, p.Center - Main.screenPosition, null, Color.White * p.Opacity,
                rotation, new Vector2(glow.Width / 2f, 0), p.scale, SpriteEffects.None);
        }
    }

    internal static void Wake(Projectile p, CrossbowKind kind)
    {
        if (Main.dedServ || p.numUpdates != 0) return;
        int style = Style(p, kind);
        bool green = style != 1;
        if (green || Main.rand.NextBool(2))
        {
            Dust dust = Dust.NewDustDirect(p.position, p.width, p.height, style > 2 ? DustID.TintableDustLighted : DustID.YellowTorch);
            dust.noGravity = true;
            if (style > 2) dust.color = EffectColor(style);
        }
        if (green && Main.rand.NextBool(4))
        {
            Dust dust = Dust.NewDustDirect(p.position, p.width, p.height, style > 2 ? DustID.TintableDustLighted : DustID.CoralTorch);
            if (style > 2) dust.color = EffectColor(style);
            dust.scale = 1.5f;
            dust.noGravity = true;
        }
    }

    internal static void Burst(Projectile p, CrossbowKind kind, bool muzzle)
    {
        // Only the owner spawns networked visual projectiles.
        if (Main.dedServ || p.owner != Main.myPlayer) return;
        int style = Style(p, kind);
        Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX);
        int count = muzzle ? 6 : 5;
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity;
            if (muzzle)
            {
                float angle = Main.rand.NextFloat(-45f + i % 3 * 30f, -15f + i % 3 * 30f);
                velocity = aim.RotatedBy(MathHelper.ToRadians(angle))
                    * (.5f + System.Math.Abs(angle) / 90f) * Main.rand.NextFloat(.5f, 1.5f) * 8f;
            }
            else velocity = Vector2.UnitX.RotatedBy(MathHelper.ToRadians(i * 72f + Main.rand.NextFloat(-24f, 24f)))
                * Main.rand.NextFloat(2f, 4f);
            Projectile.NewProjectile(p.GetSource_FromThis(), p.Center, velocity,
                ModContent.ProjectileType<KroosAlterCrossbow_Effect>(), 0, 0f, p.owner, 0f, 0f, style);
        }
        Projectile.NewProjectile(p.GetSource_FromThis(), p.Center, muzzle ? aim : Vector2.Zero,
            muzzle ? ModContent.ProjectileType<KroostheKeenGlint_Crossbow_Circle1>()
                : ModContent.ProjectileType<KroostheKeenGlint_Crossbow_Circle2>(),
            0, 0f, p.owner, 0f, 0f, style);
    }
}
