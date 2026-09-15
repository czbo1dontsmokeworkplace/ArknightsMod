using System;
using ArknightsMod.Content.Items.Weapons.Sniper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

public sealed class CrossbowHoldout : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Sniper/KroosAlter/KroosAlterCrossbow_Hold";
    private float recoil;
    private float lastShotSerial;
    private CrossbowWeaponBase boundWeapon;
    private CrossbowKind Kind => (CrossbowKind)(int)Projectile.ai[0];
    public override void SetDefaults()
    {
        Projectile.width = 60;
        Projectile.height = 28;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.noItems || player.CCed
            || player.HeldItem.type != (int)Projectile.ai[1]
            || player.HeldItem.ModItem is not CrossbowWeaponBase weapon)
        { Projectile.Kill(); return; }
        bool owner = Projectile.owner == Main.myPlayer;
        if (owner && ((!player.channel && weapon.ForcedShots == 0) || !player.HasAmmo(player.HeldItem)))
        { Projectile.Kill(); return; }
        boundWeapon = weapon;

        if (owner)
        {
            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            if (Vector2.DistanceSquared(aim, Projectile.velocity) > .0001f)
            { Projectile.velocity = aim; Projectile.netUpdate = true; }
        }
        Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
        Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, true);
        Projectile.rotation = direction.ToRotation();
        Projectile.direction = Projectile.spriteDirection = direction.X >= 0 ? 1 : -1;
        player.ChangeDir(Projectile.direction);
        player.heldProj = Projectile.whoAmI;
        player.itemRotation = (direction * player.direction).ToRotation();
        player.itemTime = player.itemAnimation = 2;
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        Projectile.timeLeft = 2;
        if (owner && weapon.Cadence.Ready(Main.GameUpdateCount))
        {
            // Stop the muzzle at solid tiles so holding a crossbow against a wall cannot shoot through it.
            Vector2 center = player.MountedCenter;
            Vector2 muzzle = center + Collision.TileCollision(center - new Vector2(4f), direction * 30f, 8, 8, true, true);
            if (weapon.Fire(player, muzzle, direction))
            { Projectile.ai[2]++; Projectile.netUpdate = true; }
        }
        if (Projectile.ai[2] != lastShotSerial)
        {
            lastShotSerial = Projectile.ai[2];
            recoil = Kind == CrossbowKind.Schwarz ? 8f : 4f;
            if (!Main.dedServ)
                SoundEngine.PlaySound(new SoundStyle(SoundPath()) { Volume = Kind == CrossbowKind.Schwarz ? .65f : .4f,
                    MaxInstances = 4, PitchVariance = .08f }, Projectile.Center);
        }
        recoil *= .78f;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner != Main.myPlayer || boundWeapon == null) return;
        boundWeapon.ForcedShots = 0;
        boundWeapon.Cadence.Interrupt(Main.GameUpdateCount, Kind);
    }

    private string SoundPath() => Kind switch {
        CrossbowKind.Kroos => "ArknightsMod/Sounds/KroosCrossbowS1",
        CrossbowKind.KroosAlter => "ArknightsMod/Sounds/p_atk_krossbow_d",
        CrossbowKind.Schwarz => "ArknightsMod/Content/Items/Weapons/Sniper/Schwarz/SchwarzAttackSound",
        _ => "ArknightsMod/Sounds/PozemkaCrossbowS0" };

    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        int itemType = (int)Projectile.ai[1];
        if (itemType <= 0 || itemType >= TextureAssets.Item.Length) return false;
        Main.instance.LoadItem(itemType);
        Texture2D texture = Kind == CrossbowKind.KroosAlter ? TextureAssets.Projectile[Type].Value : TextureAssets.Item[itemType].Value;
        float width = Kind switch { CrossbowKind.Kroos => 54f, CrossbowKind.KroosAlter => 60f,
            CrossbowKind.Schwarz => 64f, _ => 66f };
        float scale = width / texture.Width;
        Vector2 aim = Projectile.velocity.SafeNormalize(Vector2.UnitX * player.direction);
        Vector2 position = Projectile.Center + aim * (12f - recoil);
        SpriteEffects flip = Projectile.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Main.EntitySpriteDraw(texture, position - Main.screenPosition, null, lightColor, Projectile.rotation,
            texture.Size() * .5f, scale, flip);
        if (recoil > .4f)
            CrossbowVisuals.DrawMuzzle(position + aim * width * .4f, aim, Kind, recoil / 8f);
        return false;
    }
}
