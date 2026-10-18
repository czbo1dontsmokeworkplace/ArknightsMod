using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Blaze;

/// <summary>煌专用三帧锯身绘制，不修改共用近战绘制层或伤害判定。</summary>
internal static class BlazeChainsawSprite
{
    internal const string TexturePath = "ArknightsMod/Content/Items/Weapons/Guard/Blaze/BlazeGreatsword";
    internal const string ExtendedTexturePath = "ArknightsMod/Content/Items/Weapons/Guard/Blaze/BlazeExtendedChainsaw";
    internal const int FrameCount = 3;
    internal const int FrameWidth = 106;
    internal const int FrameHeight = 50;
    // 握点移至锯身中心稍上方，使锯身在手中略微下沉；左右翻转时保持握点稳定。
    private static readonly Vector2 Grip = new(24f, 20f);
    private static readonly Vector2 Tip = new(102f, 22f);

    internal static int FrameAt(int age, int ticksPerFrame)
        => Math.Max(0, age) / Math.Max(1, ticksPerFrame) % FrameCount;

    internal static Texture2D GetTexture(bool extended)
        => ModContent.Request<Texture2D>(extended ? ExtendedTexturePath : TexturePath).Value;

    internal static void DrawHeld(Texture2D sheet, Vector2 gripWorld, float angle,
        bool faceLeft, float reach, Color color, int frame)
    {
        bool extended = sheet.Width == 198 && sheet.Height == 162;
        int frameWidth = extended ? 198 : FrameWidth;
        int frameHeight = extended ? 54 : FrameHeight;
        // 延伸模块握住后端 20% 的长柄，不再抓在机身处。
        Vector2 grip = extended ? new Vector2(frameWidth * 0.2f, 12f) : Grip;
        Vector2 tip = extended ? new Vector2(192f, 20f) : Tip;
        Rectangle source = new(0, Math.Clamp(frame, 0, FrameCount - 1) * frameHeight, frameWidth, frameHeight);
        Vector2 axis = tip - grip;
        float nativeAngle = axis.ToRotation();
        Vector2 origin = grip;
        if (faceLeft)
            origin.Y = frameHeight - origin.Y;
        // 只调整视觉缩放，使锯尖对齐现有攻击端点，不从纹理反推或放大攻击范围。
        Main.spriteBatch.Draw(sheet, gripWorld - Main.screenPosition, source, color,
            faceLeft ? angle + nativeAngle : angle - nativeAngle, origin, reach / axis.Length(),
            faceLeft ? SpriteEffects.FlipVertically : SpriteEffects.None, 0f);
    }
}
