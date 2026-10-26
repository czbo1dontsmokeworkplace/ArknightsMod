using System;
using System.IO;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

public sealed class VectorVisualLoader : ModSystem
{
    public override void Load()
    {
        if (!Main.dedServ) VectorArt.Assign(name => ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/" + name,
            ReLogic.Content.AssetRequestMode.ImmediateLoad).Value);
    }
    public override void Unload() => VectorArt.Clear();
}

// ai[0] bound inventory slot; ai[1] shot serial; ai[2] active visual mode.
public sealed class ExusiaiVectorHoldout : ModProjectile
{
    private Item boundItem;
    private int seenSerial, remoteReload, remoteRounds = 40;
    private float flash, recoil;
    private bool wasReloading;
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Sniper/Exusiai/ExusiaiVector";
    private Vector2 Aim => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    private Vector2 Muzzle => Projectile.Center + Aim * 25;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.penetrate = -1; Projectile.timeLeft = 2; Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        Player owner = Main.player[Projectile.owner];
        var gun = owner.HeldItem.ModItem as ExusiaiVector;
        writer.Write((byte)(gun?.Magazine.ReloadTicks ?? 0)); writer.Write((byte)(gun?.Magazine.Rounds ?? 40));
    }
    public override void ReceiveExtraAI(BinaryReader reader) { remoteReload = reader.ReadByte(); remoteRounds = reader.ReadByte(); }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.ModItem is not ExusiaiVector gun
            || owner.selectedItem != (int)Projectile.ai[0]) { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (local && boundItem != null && !ReferenceEquals(owner.HeldItem, boundItem)) { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        if (local)
        {
            Vector2 next = (Main.MouseWorld - owner.MountedCenter).SafeNormalize(Aim);
            if (Vector2.DistanceSquared(next, Aim) > .0002f) { Projectile.velocity = next; Projectile.netUpdate = true; }
            int mode = gun.Mode(owner);
            if ((int)Projectile.ai[2] != mode) { Projectile.ai[2] = mode; Projectile.netUpdate = true; }
            if (Main.mouseLeft && ChargedOperatorWeapon.WorldInputAllowed(owner)
                && !ArknightsKeybinds.SkillActivatePressed(owner) && gun.Magazine.CanFire
                && gun.PickRound(owner, mode, out int damage, out float knockback, out int ammo))
            {
                Vector2 axis = Aim, normal = axis.RotatedBy(MathHelper.PiOver2);
                // Main stays on the aim axis; secondaries diverge slightly like SHPC's beam chains.
                int lanes = VectorBalance.Lanes(mode);
                for (int i = 0; i < lanes; i++)
                {
                    Vector2 desired = owner.MountedCenter + axis * 51 + normal * VectorBalance.LaneOffset(mode, i);
                    Vector2 spawn = owner.MountedCenter + Collision.TileCollision(owner.MountedCenter - new Vector2(3),
                        desired - owner.MountedCenter, 6, 6, true, true);
                    float angle = VectorBalance.LaneAngle(mode, i, i == 0 ? 0 : Main.rand.NextFloat(-1, 1));
                    Projectile shot = Projectile.NewProjectileDirect(owner.GetSource_ItemUse_WithPotentialAmmo(owner.HeldItem, ammo),
                        spawn, axis.RotatedBy(angle) * 16, ModContent.ProjectileType<ExusiaiVector_Bullet>(), damage, knockback, owner.whoAmI, mode);
                    shot.CritChance = owner.GetWeaponCrit(owner.HeldItem);
                    shot.ArmorPenetration = owner.GetWeaponArmorPenetration(owner.HeldItem);
                }
                gun.Magazine.Fire();
                var skills = owner.GetModPlayer<WeaponPlayer>();
                if (skills.Skill == 0)
                {
                    if (mode == 0) skills.OffensiveRecovery();
                    else if (gun.Magazine.EmpoweredShots == 0) skills.SkillActive = false;
                }
                if (gun.Magazine.Reloading) owner.GetModPlayer<VectorReloadPlayer>().Bind(gun);
                Projectile.ai[1]++; Projectile.netUpdate = true;
            }
            if (gun.Magazine.Reloading && Main.GameUpdateCount % 6 == 0) Projectile.netUpdate = true;
        }
        bool reload = local ? gun.Magazine.Reloading : remoteReload > 0;
        if (!local && remoteReload > 0) remoteReload--;
        if (reload != wasReloading)
        {
            if (reload && !Main.dedServ) SoundEngine.PlaySound(SoundID.Item149 with { Volume = .4f, Pitch = -.3f }, owner.Center);
            wasReloading = reload; Projectile.netUpdate |= local;
        }
        flash *= .45f; recoil *= .65f;
        if ((int)Projectile.ai[1] != seenSerial)
        {
            seenSerial = (int)Projectile.ai[1]; flash = 1; recoil = 2.5f;
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(ExusiaiVector.FireSound, owner.Center);
                if (Main.GameUpdateCount % 2 == 0)
                {
                    Dust smoke = Dust.NewDustPerfect(Muzzle, DustID.Smoke, Aim * .7f - Vector2.UnitY * .6f, 160, default, .65f);
                    smoke.noGravity = true;
                }
            }
        }
        owner.ChangeDir(Aim.X < 0 ? -1 : 1);
        Projectile.rotation = Aim.ToRotation();
        Projectile.Center = owner.MountedCenter + Aim * (26 - recoil);
        owner.heldProj = Projectile.whoAmI;
        owner.itemRotation = (Aim * owner.direction).ToRotation();
        float tilt = reload ? MathF.Sin((1 - (local ? gun.Magazine.ReloadTicks : remoteReload) / 72f) * MathHelper.Pi) * -.45f * owner.direction : 0;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2 + tilt);
        owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation - MathHelper.PiOver2 + tilt + .3f * owner.direction);
        Projectile.timeLeft = 2;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player owner = Main.player[Projectile.owner];
        int mode = (int)Projectile.ai[2];
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        bool reload = Projectile.owner == Main.myPlayer && owner.HeldItem.ModItem is ExusiaiVector gun ? gun.Magazine.Reloading : remoteReload > 0;
        float rotation = Projectile.rotation + (reload ? -.25f * owner.direction : 0);
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor, rotation, texture.Size() / 2,
            1f, owner.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        if (!reload) VectorArt.Muzzle(Main.spriteBatch, Muzzle - Main.screenPosition, Projectile.rotation, flash, mode);
        return false;
    }
}
