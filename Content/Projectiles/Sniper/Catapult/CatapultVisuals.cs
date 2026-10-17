using System;
using ArknightsMod.Common.Particle.T;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Catapult;

internal static class CatapultVisuals
{
    // 仓库内现有遮罩：未预乘RGBA，普通层NonPremultiplied，发光层Additive且保留Alpha。
    private const string Bloom = "ArknightsMod/Content/Projectiles/Sniper/Fiammetta/Assets/FiammettaExplosionBloom";
    private const string Ring = "ArknightsMod/Content/Textures/circle_03";
    private static readonly Color Amber = new(239, 148, 49);
    private static readonly Rectangle Pixel = new(0, 0, 1, 1);

    private static bool Visible(Vector2 center, float radius) => !Main.dedServ
        && new Rectangle((int)(Main.screenPosition.X - radius - 80f), (int)(Main.screenPosition.Y - radius - 80f),
            Main.screenWidth + (int)(radius * 2f + 160f), Main.screenHeight + (int)(radius * 2f + 160f)).Contains(center.ToPoint());

    internal static void MuzzleFlash(Vector2 center, Vector2 aim)
    {
        if (!Visible(center, 32f)) return;
        for (int i = 0; i < 4; i++)
            new T_Catapult_Debris(center, aim.RotatedBy(Main.rand.NextFloat(-.25f, .25f)) * Main.rand.NextFloat(1f, 3f),
                Amber, 2f, 10, center, 30f, true).Spawn();
    }

    internal static void FlightTrail(Projectile shell)
    {
        for (int i = 1; i < shell.oldPos.Length; i++)
        {
            if (shell.oldPos[i] == Vector2.Zero || shell.oldPos[i - 1] == Vector2.Zero) continue;
            Vector2 delta = shell.oldPos[i - 1] - shell.oldPos[i];
            float length = delta.Length();
            if (length < .1f || length > 20f) continue;
            float fade = (1f - i / (float)shell.oldPos.Length) * .32f;
            Main.EntitySpriteDraw(TextureAssets.MagicPixel.Value, shell.oldPos[i] + shell.Size * .5f - Main.screenPosition,
                Pixel, Amber * fade, delta.ToRotation(), new Vector2(0f, .5f), new Vector2(length, 1.6f), SpriteEffects.None);
        }
    }

    internal static void Impact(Vector2 center, float radius)
    {
        if (!Visible(center, radius)) return;
        // 每次爆炸固定12片：6枚短命余火、6枚暗色弹壳；没有额外伤害判定。
        for (int i = 0; i < 12; i++)
        {
            Vector2 direction = (MathHelper.TwoPi * i / 12f + Main.rand.NextFloat(-.13f, .13f)).ToRotationVector2();
            bool ember = i % 2 == 0;
            new T_Catapult_Debris(center + direction * radius * .1f, direction * radius * Main.rand.NextFloat(.035f, .065f),
                ember ? Amber : new Color(92, 74, 55), ember ? 2.3f : 3.5f,
                ember ? 18 : 26, center, radius * .88f, ember).Spawn();
        }
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = Main.rand.NextVector2Circular(radius * .22f, radius * .22f);
            Dust dust = Dust.NewDustPerfect(center + offset, i % 2 == 0 ? DustID.Smoke : DustID.Torch,
                offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(.8f, 2.2f), 120, Amber, .8f);
            dust.noGravity = true;
        }
    }

    internal static void Explosion(Vector2 center, float diameter, float progress)
    {
        if (!Visible(center, diameter * .5f)) return;
        Texture2D bloom = ModContent.Request<Texture2D>(Bloom).Value;
        Texture2D ring = ModContent.Request<Texture2D>(Ring).Value;
        float expansion = 1f - MathF.Pow(1f - progress, 3f);
        float fade = MathF.Pow(1f - progress, 1.5f);
        Vector2 position = center - Main.screenPosition;
        Main.spriteBatch.End();
        try
        {
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            for (int i = 0; i < 4; i++)
            {
                float angle = i * MathHelper.PiOver2 + .3f;
                Vector2 offset = angle.ToRotationVector2() * diameter * .14f * expansion;
                Main.spriteBatch.Draw(bloom, position + offset, null, new Color(103, 82, 58) * (fade * .46f),
                    angle, bloom.Size() * .5f, new Vector2(diameter * (.25f + expansion * .30f)) / bloom.Size(), SpriteEffects.None, 0f);
            }
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
            Main.spriteBatch.Draw(bloom, position, null, Amber * (fade * .72f), progress * .3f,
                bloom.Size() * .5f, new Vector2(diameter * (.25f + .57f * expansion)) / bloom.Size(), SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(ring, position, null, new Color(255, 196, 100) * (fade * .42f), 0f,
                ring.Size() * .5f, new Vector2(diameter * (.25f + .73f * expansion)) / ring.Size(), SpriteEffects.None, 0f);
        }
        finally
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
    }
}
