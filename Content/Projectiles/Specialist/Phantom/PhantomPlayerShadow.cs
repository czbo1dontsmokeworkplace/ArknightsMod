using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Specialist.Phantom;

// Uses the same native player-clone path as Calamity's Dark Master clone.
// The owner is never dyed or temporarily modified to draw the echo.
internal sealed class PhantomPlayerShadow
{
    private Player clone;
    internal static void CopyAppearance(Player clone, Player owner)
    {
        clone.CopyVisuals(owner);
        clone.skinColor = clone.shirtColor = clone.underShirtColor = Color.Black;
        clone.pantsColor = clone.shoeColor = clone.hairColor = clone.eyeColor = Color.Black;
        for (int i = 0; i < clone.dye.Length; i++)
            if (clone.dye[i].type != ItemID.ShadowDye) clone.dye[i].SetDefaults(ItemID.ShadowDye);
    }
    internal void Draw(Player owner, Vector2 center, float aim, float opacity)
    {
        if (Main.dedServ || opacity <= 0) return;
        clone ??= new Player();
        CopyAppearance(clone, owner);
        clone.ResetEffects();
        clone.ResetVisibleAccessories();
        clone.DisplayDollUpdate();
        clone.UpdateSocialShadow();
        clone.UpdateDyes();
        clone.direction = System.MathF.Cos(aim) < 0 ? -1 : 1;
        clone.gravDir = owner.gravDir;
        clone.PlayerFrame();
        clone.bodyFrame.Y = owner.ItemAnimationActive ? owner.bodyFrame.Y : 0;
        clone.legFrame.Y = 0;
        clone.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
        Main.PlayerRenderer.DrawPlayer(Main.Camera, clone, center - clone.Size * .5f,
            0f, Vector2.Zero, 1f - MathHelper.Clamp(opacity, 0, 1), 1f);
    }
}
