using System;
using System.IO;
using ArknightsMod.Content.Items.Developer;
using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Developer;

// ai[0]: inventory slot; ai[1]: shot serial; ai[2]: right-button scanning state.
public sealed class SR25Holdout : ModProjectile
{
    private Item boundItem;
    private int seenSerial, heartbeat;
    private float recoil, flash;
    private Vector2 cursor;
    private ulong lastInputTick;
    public override string Texture => "ArknightsMod/Content/Items/Developer/SR25";
    internal Vector2 Aim => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    internal Vector2 Cursor => cursor;
    internal bool Scanning => Projectile.ai[2] == 1 && Main.GameUpdateCount - lastInputTick <= 45;
    private float DrawRotation => Aim.ToRotation() - recoil * .012f * Main.player[Projectile.owner].direction;
    private Vector2 Muzzle => Projectile.Center + DrawRotation.ToRotationVector2() * 64f;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(cursor.X); writer.Write(cursor.Y); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Vector2 next = new(reader.ReadSingle(), reader.ReadSingle());
        if (!float.IsFinite(next.X) || !float.IsFinite(next.Y)) { Projectile.ai[2] = 0; return; }
        cursor = next;
        lastInputTick = Main.GameUpdateCount;
    }

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.noItems || owner.CCed || owner.HeldItem.ModItem is not SR25 gun
            || owner.selectedItem != (int)Projectile.ai[0]) { Projectile.Kill(); return; }
        bool local = Projectile.owner == Main.myPlayer;
        if (local && boundItem != null && !ReferenceEquals(boundItem, owner.HeldItem)) { Projectile.Kill(); return; }
        boundItem ??= owner.HeldItem;
        recoil *= .73f;
        flash *= .45f;
        if (local)
        {
            bool world = ChargedOperatorWeapon.WorldInputAllowed(owner);
            Vector2 nextCursor = Main.MouseWorld;
            Vector2 next = (nextCursor - owner.MountedCenter).SafeNormalize(Aim);
            int scan = world && Main.mouseRight ? 1 : 0;
            if (Vector2.DistanceSquared(next, Aim) > .0002f || scan != (int)Projectile.ai[2] || ++heartbeat >= 10)
            {
                cursor = nextCursor;
                Projectile.velocity = next;
                Projectile.ai[2] = scan;
                Projectile.netUpdate = true;
                heartbeat = 0;
            }
            // Cursor distance matters even when its angle has not changed.
            cursor = nextCursor;
            lastInputTick = Main.GameUpdateCount;
            var state = owner.GetModPlayer<SR25Player>();
            if (world && Main.mouseLeft && state.CanFire && gun.Fire(owner, SafeMuzzle(owner), Aim))
            {
                state.Fired();
                Projectile.ai[1]++;
                Projectile.netUpdate = true;
            }
        }
        owner.ChangeDir(Aim.X < 0 ? -1 : 1);
        Projectile.rotation = DrawRotation;
        Projectile.Center = owner.MountedCenter + Aim * (16 - recoil);
        owner.heldProj = Projectile.whoAmI;
        owner.itemRotation = (Aim * owner.direction).ToRotation();
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.rotation - MathHelper.PiOver2 + .25f * owner.direction);
        if ((int)Projectile.ai[1] != seenSerial)
        {
            seenSerial = (int)Projectile.ai[1];
            recoil = 5f;
            flash = 1;
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item40 with { Volume = .65f, Pitch = -.12f, MaxInstances = 5 }, owner.Center);
                Vector2 eject = Projectile.Center - Aim * 12;
                Vector2 velocity = -Aim * Main.rand.NextFloat(1.8f, 3.1f) - Vector2.UnitY * Main.rand.NextFloat(2.5f, 4);
                Gore.NewGore(Projectile.GetSource_FromAI(), eject, velocity, ModContent.GoreType<SR25EjectedCasing>(), .8f);
                Dust smoke = Dust.NewDustPerfect(Muzzle, DustID.Smoke, Aim * 1.5f - Vector2.UnitY * .5f, 150, default, .65f);
                smoke.noGravity = true;
            }
        }
        Projectile.timeLeft = 2;
    }

    private Vector2 SafeMuzzle(Player owner)
    {
        Vector2 desired = owner.MountedCenter + Aim * 80;
        return owner.MountedCenter + Collision.TileCollision(owner.MountedCenter - new Vector2(2),
            desired - owner.MountedCenter, 4, 4, true, true);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Scanning && Projectile.owner == Main.myPlayer && !Main.player[Projectile.owner].GetModPlayer<SR25Player>().RadarEquipped)
        {
            Player owner = Main.player[Projectile.owner];
            SR25Visuals.DrawScan(owner.MountedCenter, Aim, owner.GetModPlayer<SR25Player>().Target?.Center);
        }
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation, texture.Size() / 2, 1,
            Aim.X < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
        SR25Visuals.DrawMuzzle(Muzzle, Projectile.rotation, flash);
        return false;
    }
}
