using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

// Owner-only world-space HUD. Player draw transforms handle zoom and gravity; no UI-scale
// coordinates are mixed into the world position. Shadows/portraits must not duplicate the bar.
public sealed class VanguardReserveLayer : PlayerDrawLayer
{
    public override Position GetDefaultPosition() => PlayerDrawLayers.AfterLastVanillaLayer;
    protected override void Draw(ref PlayerDrawSet drawInfo)
    {
        Player player = drawInfo.drawPlayer;
        if (drawInfo.shadow != 0 || player.whoAmI != Main.myPlayer || player.dead
            || player.HeldItem.ModItem is not VanguardSpearWeapon weapon) return;
        VanguardSpearPlayer state = player.GetModPlayer<VanguardSpearPlayer>();
        Vector2 center = drawInfo.Position + new Vector2(player.width / 2f, player.gravDir > 0 ? -24 : player.height + 24)
            - Main.screenPosition;
        Color color = VanguardSpearRules.Tint(weapon.Kind);
        Add(ref drawInfo, center, new Vector2(62, 10), new Color(10, 14, 24, 225));
        Add(ref drawInfo, center, new Vector2(58, 6), new Color(38, 45, 58));
        float width = 56 * state.Reserve.Ratio;
        if (width > 0) Add(ref drawInfo, center + new Vector2((width - 56) / 2, 0), new Vector2(width, 4), color);
        // Three / four / five pips communicate the number of throws before the player spends it.
        int count = VanguardSpearRules.VolleyCount(weapon.Kind);
        for (int i = 0; i < count; i++)
            Add(ref drawInfo, center + new Vector2((i - (count - 1) / 2f) * 8, -8), new Vector2(4, 2),
                state.Reserve.Ready ? color : new Color(70, 78, 90));
        if (state.ReadyFlash > 0)
            Add(ref drawInfo, center + new Vector2(0, 7), new Vector2(58, 1), color * (state.ReadyFlash / 24f));
    }
    private static void Add(ref PlayerDrawSet info, Vector2 position, Vector2 size, Color color)
        => info.DrawDataCache.Add(new DrawData(TextureAssets.MagicPixel.Value, position, new Rectangle(0, 0, 1, 1),
            color, 0, new Vector2(.5f), size, SpriteEffects.None));
}
