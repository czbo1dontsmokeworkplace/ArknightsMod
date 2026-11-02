using System;
using ArknightsMod.Content.Items.Weapons.Sniper.Catapult;
using ArknightsMod.Content.Items.Weapons.Sniper.Fiammetta;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Catapult;

public sealed class CatapultHoldout : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Sniper/Catapult/CatapultLauncher";
    internal static Vector2 Muzzle(Player owner, Vector2 aim) => owner.MountedCenter
        + Collision.TileCollision(owner.MountedCenter - new Vector2(4f), aim * 30f, 8, 8, true, true);

    public override void SetDefaults()
    {
        Projectile.width = 48; Projectile.height = 22;
        Projectile.timeLeft = CatapultLauncher.AttackInterval;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void OnSpawn(IEntitySource source)
    {
        Player owner = Main.player[Projectile.owner];
        Vector2 aim = Projectile.velocity.SafeNormalize(-Vector2.UnitY * owner.gravDir);
        ArtilleryMuzzleVisuals.Emit(Muzzle(owner, aim), aim, 0);
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.ModItem is not CatapultLauncher)
        { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer)
        {
            Vector2 target = FiammettaHandCannon.ClampTarget(owner, Main.MouseWorld, CatapultLauncher.MaximumRange);
            Vector2 direction = ArtilleryBallistics.LaunchDirection(owner, target);
            if (Vector2.DistanceSquared(direction, Projectile.velocity) > .0001f)
            {
                Projectile.velocity = direction;
                Projectile.netUpdate = true;
            }
        }
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
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        int age = CatapultLauncher.AttackInterval - Projectile.timeLeft;
        float recoil = age == 0 ? 0f : 5f * MathF.Pow(.80f, age);
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        // 原图炮口朝左，水平翻转后沿瞄准方向绘制；保持48px手持宽度与炮口位置。
        Main.EntitySpriteDraw(texture, Projectile.Center + aim * (8f - recoil) - Main.screenPosition,
            null, lightColor, Projectile.rotation, texture.Size() * .5f, 48f / texture.Width,
            SpriteEffects.FlipHorizontally | (Projectile.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None));
        return false;
    }
}
