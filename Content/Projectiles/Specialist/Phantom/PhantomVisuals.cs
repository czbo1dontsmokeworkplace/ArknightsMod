using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Content.Projectiles.BasePROJ;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Specialist.Phantom;

// 保留红/砾的刀光素材与三层结构，加入镜像错位刃、幕布剪影与血色乐章音符。
internal static class PhantomVisuals
{
    private static Texture2D Knife => ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Specialist/Red/RedDagger").Value;
    private static readonly Color Silver = new(170, 225, 245);
    private static readonly Color Shadow = new(75, 54, 105);
    private static readonly Color Crimson = new(190, 45, 74);

    internal static void DrawEmblem(SpriteBatch sb, Vector2 position, float scale)
    {
        Texture2D knife = Knife;
        sb.Draw(knife, position + new Vector2(-4, 1) * scale, null, new Color(180, 200, 215),
            -.35f, knife.Size() / 2, scale * .82f, SpriteEffects.FlipHorizontally, 0);
        sb.Draw(knife, position + new Vector2(4, -1) * scale, null, Color.White,
            .35f, knife.Size() / 2, scale * .82f, SpriteEffects.None, 0);
    }
    internal static void DrawKnife(Vector2 hand, float arm, float fade)
    {
        Texture2D knife = Knife;
        Vector2 grip = new(2, knife.Height - 2);
        float rotation = arm + MathHelper.PiOver2 + MathHelper.PiOver4;
        // 两道短残像贴着刃走，不形成遮挡角色的大光球。
        for (int i = 2; i >= 1; i--)
            Main.spriteBatch.Draw(knife, hand - Main.screenPosition, null, Shadow * (fade * .18f),
                rotation - .12f * i, grip, .68f, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(knife, hand - Main.screenPosition, null, Color.Lerp(Color.White, Silver, .25f) * fade,
            rotation, grip, .68f, SpriteEffects.None, 0);
    }
    internal static void DrawSlash(Vector2 center, float angle, float fade, bool reverse, bool echo)
    {
        Texture2D body = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Guard/Hellagur/HellagurSlashBody").Value;
        Texture2D edge = ModContent.Request<Texture2D>("ArknightsMod/Content/Projectiles/Guard/Hellagur/HellagurSlashEdge").Value;
        SpriteEffects flip = reverse ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Vector2 position = center - Main.screenPosition;
        Vector2 size = new(126, 86);
        Main.spriteBatch.Draw(body, position, null, new Color(22, 16, 36) * (fade * .8f), angle,
            body.Size() * .5f, size / body.Size(), flip, 0);
        BaseHeldMeleeSupport.BeginAdditive(Main.spriteBatch);
        for (int layer = 0; layer < 3; layer++)
        {
            Texture2D texture = layer == 0 ? edge : body;
            Color color = layer == 0 ? Silver : layer == 1 ? Shadow : Crimson;
            float strength = layer == 0 ? .9f : layer == 1 ? .65f : .45f;
            Main.spriteBatch.Draw(texture, position, null, color * (fade * strength * (echo ? .85f : 1f)), angle,
                texture.Size() * .5f, size / texture.Size() * (.72f + layer * .14f), flip, 0);
        }
        // 镜中错位刃：在主刀光后留一道更细、反向翻转的银蓝弧。
        Main.spriteBatch.Draw(edge, position - angle.ToRotationVector2() * 7, null, Silver * (fade * .5f),
            angle - .28f, edge.Size() * .5f, size / edge.Size() * new Vector2(.9f, .78f),
            reverse ? SpriteEffects.None : SpriteEffects.FlipVertically, 0);
        BaseHeldMeleeSupport.EndAdditive(Main.spriteBatch);
    }
    private static void Line(Vector2 from, Vector2 to, Color color, float width)
    {
        Vector2 delta = to - from;
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value, from - Main.screenPosition, null,
            color, delta.ToRotation(), new Vector2(0, .5f), new Vector2(delta.Length(), width), SpriteEffects.None, 0);
    }
    internal static void DrawEcho(Vector2 center, float aim, int age, float fade)
    {
        // 程序绘制的舞台斗篷剪影，不复制玩家装备；银蓝面具缝与红色领结便于识别。
        float sway = MathF.Sin(age * .075f) * 2f;
        for (int row = 0; row < 32; row++)
        {
            float halfWidth = 5 + row * .32f;
            float shift = sway * row / 32f;
            Line(center + new Vector2(-halfWidth + shift, row - 12),
                center + new Vector2(halfWidth + shift, row - 12), new Color(22, 18, 33) * (fade * .85f), 2);
        }
        Line(center + new Vector2(-7, -16), center + new Vector2(-4, -27), Shadow * fade, 4);
        Line(center + new Vector2(7, -16), center + new Vector2(4, -27), Shadow * fade, 4);
        Line(center + new Vector2(-5, -18), center + new Vector2(5, -18), new Color(22, 20, 32) * fade, 10);
        Line(center + new Vector2(-4, -19), center + new Vector2(-1, -18), Silver * fade, 1.5f);
        Line(center + new Vector2(1, -18), center + new Vector2(4, -19), Silver * fade, 1.5f);
        Line(center + new Vector2(-4, -9), center + new Vector2(4, -7), Crimson * fade, 3);
        float swing = MathF.Sin(age * .6f) * .9f;
        for (int side = -1; side <= 1; side += 2)
        {
            float arm = aim - MathHelper.PiOver2 + side * swing;
            Vector2 hand = center + (arm + MathHelper.PiOver2).ToRotationVector2() * 18;
            Line(center + new Vector2(side * 5, -6), hand, Shadow * fade, 5);
            DrawKnife(hand, arm, fade * .75f);
        }
    }
    internal static void DrawVeil(Vector2 center, int age, float fade)
    {
        for (int i = 0; i < 32; i++)
        {
            if (i % 8 == 0) continue;
            float a = i * MathHelper.TwoPi / 32 + MathF.Sin(age * .025f) * .08f;
            Vector2 p = new Vector2(MathF.Cos(a) * 25, MathF.Sin(a) * 35);
            float b = a + MathHelper.TwoPi / 32;
            Vector2 q = new Vector2(MathF.Cos(b) * 25, MathF.Sin(b) * 35);
            Line(center + p, center + q, Silver * (fade * .4f), 1.3f);
        }
    }
    internal static void DrawNotes(Vector2 center, int layers, int age, float fade)
    {
        for (int i = 0; i < layers; i++)
        {
            float a = -MathHelper.Pi + (i + .5f) * MathHelper.Pi / 10f;
            Vector2 p = center + new Vector2(MathF.Cos(a) * 34, MathF.Sin(a) * 35 - 5);
            Line(p, p + new Vector2(2, -5), Crimson * fade, 2.5f);
            Line(p + new Vector2(2, -5), p + new Vector2(5, -4), Silver * (fade * .65f), 1);
        }
    }
    internal static void DrawCurtain(Vector2 center, float progress)
    {
        float fade = MathF.Sin(progress * MathHelper.Pi);
        // 向两边展开的破碎幕帘；短促交叉斩，半径对应真实范围。
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 position = center + new Vector2(side * progress * 90, 0);
            DrawSlash(position, side * (.5f + progress), fade * .7f, side < 0, true);
        }
        for (int i = 0; i < 16; i++)
        {
            float angle = i * MathHelper.TwoPi / 16;
            Vector2 p = center + angle.ToRotationVector2() * (30 + progress * 98);
            Line(p, p + angle.ToRotationVector2() * 12, (i % 2 == 0 ? Silver : Crimson) * (fade * .55f), 1.5f);
        }
    }
    internal static void Burst(Vector2 center, int count)
    {
        if (Main.dedServ || Main.gameMenu) return;
        for (int i = 0; i < count; i++)
            new DefaultParticle(center + Main.rand.NextVector2Circular(5, 8), Main.rand.NextVector2Circular(3, 3),
                12 + i % 8, .24f, i % 4 == 0 ? Crimson : Silver, true)
                { Deformation = new Vector2(.25f, 1.6f) }.Spawn();
    }
}
