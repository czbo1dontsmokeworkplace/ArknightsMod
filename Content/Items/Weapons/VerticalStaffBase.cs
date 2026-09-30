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
    protected virtual Vector2 VerticalStaffTip => new(0.85f, 0.13f);
    internal Vector2 VerticalOffset => VerticalStaffOffset;
    internal float VerticalRotation => VerticalStaffRotation;
    internal Vector2 VerticalOrigin => VerticalStaffOrigin;
    internal Vector2 VerticalTip => VerticalStaffTip;

    protected void ApplyVerticalStaffPose() => Item.useStyle = ItemUseStyleID.HoldUp;

    // HoldUp selects this body frame during item animation. Persistent staves can use
    // the same frame while idle without keeping itemAnimation active and locking slots.
    public override void HoldItemFrame(Player player)
    {
        player.bodyFrame.Y = player.bodyFrame.Height * 2;
    }

    internal static Vector2 HeldStaffCenter(Player player, VerticalStaffBase staff) =>
        player.RotatedRelativePoint(player.MountedCenter, true) +
        new Vector2(staff.VerticalOffset.X * player.direction,
            staff.VerticalOffset.Y * player.gravDir);

    internal static Vector2 HeldStaffPoint(Player player, Texture2D texture, Vector2 center,
        Vector2 point, float rotation, Vector2 origin, float scale)
    {
        Vector2 local = new(texture.Width * (point.X - origin.X) * player.direction,
            texture.Height * (point.Y - origin.Y) * player.gravDir);
        return center + local.RotatedBy(rotation) * scale;
    }

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
