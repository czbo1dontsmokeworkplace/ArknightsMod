using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons;

/// <summary>Shared upright HoldUp pose and sprite placement for vertically held staves.</summary>
public abstract class VerticalStaffBase : ExpansionWeaponBase
{
    protected virtual Vector2 VerticalStaffOffset => new(6f, -8f);
    protected virtual float VerticalStaffRotation => -MathHelper.PiOver4;
    protected virtual Vector2 VerticalStaffOrigin => new(0.28f, 0.75f);
    internal Vector2 VerticalOffset => VerticalStaffOffset;
    internal float VerticalRotation => VerticalStaffRotation;
    internal Vector2 VerticalOrigin => VerticalStaffOrigin;

    protected void ApplyVerticalStaffPose() => Item.useStyle = ItemUseStyleID.HoldUp;

    internal static void DrawHeldStaff(Player player, Texture2D texture, Vector2 position,
        Color color, Vector2 offset, float rotation, Vector2 origin, float scale = 1f)
    {
        if (texture == null || player == null || !player.active || player.dead || player.noItems || player.CCed)
            return;

        Vector2 drawOrigin = new(texture.Width * (player.direction > 0 ? origin.X : 1f - origin.X),
            texture.Height * (player.gravDir > 0 ? origin.Y : 1f - origin.Y));
        SpriteEffects effects = player.direction < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (player.gravDir < 0) effects |= SpriteEffects.FlipVertically;
        Main.EntitySpriteDraw(texture, position - Main.screenPosition +
            new Vector2(offset.X * player.direction, offset.Y * player.gravDir), null, color,
            rotation, drawOrigin, scale, effects);
    }
}
