using System;
using System.IO;
using ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

public sealed class MiniGatlingHoldout : ModProjectile
{
    private int visualSerial;
    private float flash, recoil;
    private bool started, stopped;
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.ChainGun}";
    private int Age => (int)Projectile.ai[0];
    private bool Releasing => Projectile.ai[1] > 0;
    private Vector2 Aim => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    private Vector2 Muzzle => Projectile.Center + Aim * 40;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.penetrate = -1; Projectile.timeLeft = 2;
        Projectile.netImportant = true;
        Projectile.DamageType = DamageClass.Ranged;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead || player.noItems || player.CCed || player.HeldItem.ModItem is not MiniGatling weapon)
        { Projectile.Kill(); return; }
        bool authority = Projectile.owner == Main.myPlayer;
        if (authority && (!player.channel || !player.HasAmmo(player.HeldItem)) && !Releasing)
        { Projectile.ai[1] = 1; Projectile.netUpdate = true; }
        if (Releasing)
        {
            Projectile.ai[1]++;
            if (Projectile.ai[1] >= MaterialistArsenalRules.GatlingSpinDown) { Projectile.Kill(); return; }
        }
        if (authority && !Releasing)
        {
            Vector2 next = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Aim);
            if (Vector2.DistanceSquared(next, Projectile.velocity) > .0001f)
            { Projectile.velocity = next; Projectile.netUpdate = true; }
        }
        Projectile.ai[0]++;
        Projectile.rotation = Aim.ToRotation();
        Projectile.spriteDirection = Aim.X >= 0 ? 1 : -1;
        player.ChangeDir(Projectile.spriteDirection);
        Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, true) + Aim * (18 - recoil);
        player.heldProj = Projectile.whoAmI;
        player.itemRotation = (Aim * player.direction).ToRotation();
        if (!Releasing) player.itemTime = player.itemAnimation = 2;
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation - MathHelper.PiOver2);
        Projectile.timeLeft = 2;
        if (!Main.dedServ && !started)
        {
            started = true;
            SoundEngine.PlaySound(SoundID.Item23 with { Volume = .4f, Pitch = -.45f, MaxInstances = 2 }, Projectile.Center);
        }
        if (!Main.dedServ && Releasing && !stopped)
        {
            stopped = true;
            SoundEngine.PlaySound(SoundID.Item23 with { Volume = .28f, Pitch = -.7f, MaxInstances = 2 }, Projectile.Center);
        }
        if (authority && !Releasing && Age >= MaterialistArsenalRules.GatlingSpinUp &&
            (Age - MaterialistArsenalRules.GatlingSpinUp) % MaterialistArsenalRules.GatlingInterval == 0)
        {
            if (!weapon.PickRound(player, out int damage, out float knockback))
            { Projectile.ai[1] = 1; Projectile.netUpdate = true; }
            else
            {
                Vector2 safeMuzzle = player.MountedCenter + Collision.TileCollision(player.MountedCenter - new Vector2(3),
                    Muzzle - player.MountedCenter, 6, 6, true, true);
                Vector2 direction = Aim;
                int shot = Projectile.NewProjectile(Projectile.GetSource_FromThis(), safeMuzzle, direction * 15,
                    ModContent.ProjectileType<GatlingPhysicalRound>(), damage, knockback, player.whoAmI);
                if (shot < Main.maxProjectiles)
                {
                    Main.projectile[shot].CritChance = player.GetWeaponCrit(player.HeldItem);
                    Main.projectile[shot].ArmorPenetration = player.GetWeaponArmorPenetration(player.HeldItem);
                }
                Projectile.ai[2]++; Projectile.netUpdate = true;
            }
        }
        flash *= .65f; recoil *= .72f;
        if ((int)Projectile.ai[2] != visualSerial)
        {
            visualSerial = (int)Projectile.ai[2]; flash = 1; recoil = 3.5f;
            if (!Main.dedServ)
            {
                if (visualSerial % 2 == 0)
                    SoundEngine.PlaySound(SoundID.Item40 with { Volume = .5f, Pitch = -.25f, PitchVariance = .06f, MaxInstances = 4 }, Muzzle);
                MaterialistBallistics.Muzzle(Muzzle, Aim * 15, -1);
                MaterialistVisuals.Nano(Muzzle, Aim * 5, new Color(255, 195, 75), .8f, 12);
            }
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        float scale = 62f / texture.Width, angle = Projectile.rotation;
        bool left = Projectile.spriteDirection < 0;
        SpriteEffects flip = left ? SpriteEffects.FlipVertically : SpriteEffects.None;
        float spin = Math.Clamp(Age / (float)MaterialistArsenalRules.GatlingSpinUp, 0, 1) *
            (Releasing ? 1 - Projectile.ai[1] / MaterialistArsenalRules.GatlingSpinDown : 1);
        Vector2 point = Projectile.Center - Aim * 3 + new Vector2(0, MathF.Sin(Age * .9f) * spin * .65f);
        MaterialistTechRenderer.ChassisMaterial(texture, point, null, angle, new Vector2(scale), flip,
            new Color(255, 198, 75) * (.3f + spin * .45f), 4);
        Main.spriteBatch.Draw(texture, point - Main.screenPosition, null, Color.Lerp(lightColor, Color.White, .4f),
            angle, texture.Size() * .5f, scale, flip, 0);
        MaterialistVisuals.Glow(Muzzle - Aim * 6, new Color(255, 157, 51) * (spin * .28f), new Vector2(24, 12), angle);
        if (flash > .01f)
        {
            MaterialistVisuals.Glow(Muzzle + Aim * 12, new Color(255, 191, 66) * flash, new Vector2(88, 31), angle);
            MaterialistVisuals.Glow(Muzzle + Aim * 7, Color.White * (.8f * flash), new Vector2(43, 12), angle);
        }
        return false;
    }
}

public sealed class GatlingPhysicalRound : ModProjectile
{
    private Vector2 launch;
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => "Terraria/Images/MagicPixel";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged; Projectile.penetrate = 1;
        Projectile.tileCollide = true; Projectile.ignoreWater = true;
        Projectile.extraUpdates = 15; Projectile.timeLeft = 18 * 16;
    }
    public override void OnSpawn(IEntitySource source) => launch = Projectile.Center;
    public override void AI()
    {
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ && Projectile.numUpdates % (MaterialistVisuals.Reduced ? 8 : 4) == 0)
            MaterialistBallistics.Wake(Projectile, true, launch);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        MaterialistBallistics.Impact(Projectile.Center, -Projectile.velocity.SafeNormalize(Vector2.UnitY), false, true);
        MaterialistImpactJets.Emit(Projectile.Center, Projectile.velocity);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        MaterialistBallistics.Impact(Projectile.Center, -oldVelocity.SafeNormalize(Vector2.UnitY),
            true, true, directional: true);
        MaterialistImpactJets.Emit(Projectile.Center, oldVelocity);
        return true;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(launch.X); writer.Write(launch.Y); }
    public override void ReceiveExtraAI(BinaryReader reader) => launch = new Vector2(reader.ReadSingle(), reader.ReadSingle());
    public override bool PreDraw(ref Color lightColor)
    { MaterialistBallistics.DrawRound(Projectile, launch, true, 2); return false; }
}
