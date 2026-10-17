using System;
using ArknightsMod.Content.Items.Weapons.Sniper.Catapult;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Catapult;

public sealed class CatapultHoldout : ModProjectile
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.GrenadeLauncher;
    internal static Vector2 Muzzle(Player owner, Vector2 aim) => owner.MountedCenter
        + Collision.TileCollision(owner.MountedCenter - new Vector2(4f), aim * 30f, 8, 8, true, true);

    public override void SetDefaults()
    {
        Projectile.width = 48; Projectile.height = 22;
        Projectile.timeLeft = CatapultLauncher.AttackInterval;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.ModItem is not CatapultLauncher)
        { Projectile.Kill(); return; }
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
        Projectile.Center = owner.MountedCenter;
        Projectile.rotation = aim.ToRotation();
        Projectile.direction = aim.X >= 0f ? 1 : -1;
        owner.ChangeDir(Projectile.direction);
        owner.heldProj = Projectile.whoAmI;
        owner.itemRotation = (aim * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Item[ItemID.GrenadeLauncher].Value;
        int age = CatapultLauncher.AttackInterval - Projectile.timeLeft;
        float recoil = age == 0 ? 0f : 5f * MathF.Pow(.80f, age);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        // 占位图缩至48px宽：中心前移8px，炮口约在玩家前方30px。
        Main.EntitySpriteDraw(texture, Projectile.Center + aim * (8f - recoil) - Main.screenPosition,
            null, lightColor, Projectile.rotation, texture.Size() * .5f, 48f / texture.Width,
            Projectile.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        return false;
    }
}
