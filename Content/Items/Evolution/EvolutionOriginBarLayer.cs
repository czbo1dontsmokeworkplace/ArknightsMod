using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution;

// 只读层数的头顶 UI；不参与饰品属性、击杀计数、衰减或网络同步。
public sealed class EvolutionOriginBarLayer : PlayerDrawLayer
{
    private const string TextureRoot = "ArknightsMod/Content/Items/Evolution/UI/";
    internal const float BarScale = 1.5f;
    private Asset<Texture2D> background, foreground;

    public override void Load()
    {
        if (Main.dedServ) return;
        background = ModContent.Request<Texture2D>(TextureRoot + "GenericBarBack", AssetRequestMode.ImmediateLoad);
        foreground = ModContent.Request<Texture2D>(TextureRoot + "GenericBarFront", AssetRequestMode.ImmediateLoad);
    }

    public override void Unload() { background = foreground = null; }
    public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

    public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        return !Main.dedServ && !Main.gameMenu && !Main.hideUI && drawInfo.shadow == 0f &&
            player != null && player.active && !player.dead && !player.ghost && player.whoAmI == Main.myPlayer &&
            player.GetModPlayer<EvolutionAccessoryPlayer>().OriginEquipped;
    }

    // 满层对应整张前景；裁切而非压缩贴图，保持边缘和像素比例。
    internal static Rectangle FillFrame(int stacks, int width, int height) =>
        new(0, 0, (int)(width * (Math.Clamp(stacks, 0, 25) / 25f)), height);

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        if (!GetDefaultVisibility(drawInfo) || background == null || foreground == null) return;
        Player player = drawInfo.drawPlayer;
        int stacks = player.GetModPlayer<EvolutionAccessoryPlayer>().Stacks;
        AppendBar(ref drawInfo, background.Value, foreground.Value, stacks);
    }

    internal static void AppendBar(ref PlayerDrawSet drawInfo, Texture2D back, Texture2D front, int stacks)
    {
        Player player = drawInfo.drawPlayer;
        // 与参考模组一样放在头部上方；使用绘制位置跟随玩家及其绘制插值。
        Vector2 center = drawInfo.Position + player.Size * .5f - Main.screenPosition +
            new Vector2(0, player.gfxOffY - 62f * player.gravDir);
        Vector2 topLeft = center - back.Size() * (BarScale * .5f);
        topLeft = new Vector2(MathF.Round(topLeft.X), MathF.Round(topLeft.Y));
        drawInfo.DrawDataCache.Add(new DrawData(back, topLeft, null, Color.White * .9f,
            0f, Vector2.Zero, BarScale, SpriteEffects.None, 0));

        Rectangle crop = FillFrame(stacks, front.Width, front.Height);
        if (crop.Width <= 0) return;
        Color fillColor = Color.Lerp(new Color(193, 39, 69), new Color(255, 159, 173), Math.Clamp(stacks / 25f, 0, 1));
        drawInfo.DrawDataCache.Add(new DrawData(front, topLeft, crop, fillColor,
            0f, Vector2.Zero, BarScale, SpriteEffects.None, 0));
    }
}
