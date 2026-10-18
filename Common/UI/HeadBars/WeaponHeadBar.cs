using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Common.UI.HeadBars;

/// <summary>
/// Reusable weapon head-bar base. Derive once, implement TryGetBar, and return its
/// progress/colors. The shared layer discovers it automatically; no registry edits.
/// </summary>
public abstract class WeaponHeadBar : ModPlayer
{
    public HeadBarAnimation Animation { get; } = new();
    private HeadBarRequest lastRequest;

    protected abstract bool TryGetBar(out HeadBarRequest request);

    public sealed override void PostUpdate()
    {
        if (Main.dedServ || Player.whoAmI != Main.myPlayer) return;
        HeadBarRequest request = default;
        bool visible = Player.active && !Player.dead && TryGetBar(out request);
        if (visible) lastRequest = request;
        Animation.Update(visible, lastRequest.Progress, lastRequest.Style);
    }

    public sealed override void UpdateDead() => Animation.Clear();

    internal void Draw(ref PlayerDrawSet drawInfo)
    {
        float alpha = Animation.Opacity;
        if (alpha <= .001f) return;
        HeadBarStyle style = lastRequest.Style;
        Player player = drawInfo.drawPlayer;
        float side = player.gravDir >= 0 ? -1 : 1;
        Vector2 center = drawInfo.Position + new Vector2(player.width / 2f,
            player.height / 2f + side * (style.OffsetY - (1 - alpha) * style.SlidePixels))
            + new Vector2(0, player.gfxOffY) - Main.screenPosition;
        int first = drawInfo.DrawDataCache.Count;
        BoundedHeadBarRenderer.AddToPlayerDrawCache(drawInfo.DrawDataCache, center, Animation.Fill,
            style.Background, style.Start, style.End, style.Opacity * alpha, Animation.Flash * alpha,
            Main.GlobalTimeWrappedHourly + player.whoAmI * .17f);
        float scale = Math.Clamp(style.Scale, .25f, 3f);
        for (int i = first; i < drawInfo.DrawDataCache.Count; i++)
        {
            DrawData data = drawInfo.DrawDataCache[i];
            data.position = center + (data.position - center) * scale;
            data.scale *= scale;
            drawInfo.DrawDataCache[i] = data;
        }
    }
}

public sealed class WeaponHeadBarLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;

    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (drawInfo.shadow != 0 || Main.dedServ || !player.active || player.dead || player.whoAmI != Main.myPlayer) return;
        foreach (WeaponHeadBar prototype in ModContent.GetContent<WeaponHeadBar>())
            player.GetModPlayer(prototype).Draw(ref drawInfo);
    }
}
