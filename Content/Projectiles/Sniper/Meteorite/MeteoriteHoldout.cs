using ArknightsMod.Content.Items.Weapons.Sniper.Meteorite;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Meteorite;

public sealed class MeteoriteHoldout : ModProjectile
{
    private float Recoil => 8f * System.MathF.Pow(.78f, 50 - Projectile.timeLeft);
    public override string Texture => "Terraria/Images/Item_" + ItemID.GrenadeLauncher;
    public override void SetDefaults()
    {
        Projectile.width = 64; Projectile.height = 28;
        Projectile.timeLeft = 50; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.noItems || player.CCed || player.HeldItem.ModItem is not MeteoriteCannon)
        { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer)
        {
            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0001f)
            { Projectile.velocity = aim; Projectile.netUpdate = true; }
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
        Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, true);
        Projectile.rotation = direction.ToRotation();
        Projectile.direction = direction.X >= 0f ? 1 : -1;
        player.ChangeDir(Projectile.direction);
        player.heldProj = Projectile.whoAmI;
        player.itemRotation = (direction * player.direction).ToRotation();
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Item[ItemID.GrenadeLauncher].Value;
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        Vector2 position = Projectile.Center + aim * (12f - Recoil);
        Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, lightColor, Projectile.rotation,
            texture.Size() * .5f, 64f / texture.Width, Projectile.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        return false;
    }
}
