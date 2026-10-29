using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Earthshaker;

// Port of 1.4.5.8's projectile 301. Do not inherit the installed 1.4.4 boomerang
// AI: it does not have this windup, first-hit bonus or wall shockwave.
public sealed class EarthshakerReturningHammer : ModProjectile
{
    private bool shockwavePending, shockwaveDamage, impacted, impactPlayed;
    private Vector2 impactPosition;
    internal EarthshakerKind Kind => (EarthshakerKind)(int)Projectile.ai[2];
    public override string Texture => "Terraria/Images/Projectile_301";

    internal static EarthshakerReturningHammer Find(Player player)
    {
        foreach (Projectile shot in Main.ActiveProjectiles)
            if (shot.owner == player.whoAmI && shot.ModProjectile is EarthshakerReturningHammer hammer)
                return hammer;
        return null;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 38;
        Projectile.scale = 1.75f;
        Projectile.aiStyle = -1;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 1000;
        Projectile.extraUpdates = 2;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
        Projectile.manualDirectionChange = true;
    }

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        if (!player.active || player.dead)
        {
            Projectile.Kill();
            return;
        }

        if (Projectile.soundDelay == 0 && Projectile.ai[0] != 0f)
        {
            Projectile.soundDelay = 10 * (Projectile.extraUpdates + 1);
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item7 with { Pitch = -.33f }, Projectile.position);
        }

        if (Projectile.ai[0] == 0f)
        {
            Projectile.direction = Projectile.velocity.X < 0f ? -1 : 1;
            Projectile.ai[1]++;
            Projectile.scale = 1.75f;
            if (Projectile.ai[1] == 1f)
                Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver4 - MathHelper.PiOver2;
            if (Projectile.ai[1] == 20f && !Main.dedServ)
                SoundEngine.PlaySound(SoundID.Item1, Projectile.Center);
            if (Projectile.ai[1] < 30f)
            {
                Projectile.rotation += Projectile.direction * (MathHelper.Pi / 30f);
                Projectile.Center = player.MountedCenter - Projectile.velocity;
            }
            else Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            if (Projectile.ai[1] >= 60f)
            {
                Projectile.ai[0] = 1f;
                Projectile.ai[1] = 0f;
                Projectile.velocity = Vector2.Zero;
                Projectile.netUpdate = true;
            }
        }
        else
        {
            Projectile.tileCollide = false;
            float speed = 15f, acceleration = 3f;
            // Include generic attack speed and vanilla's pre-inversion 3x cap.
            float attackSpeed = Math.Min(3f, player.GetTotalAttackSpeed(DamageClass.Melee));
            float factor = Math.Clamp(1f + 1.5f * (attackSpeed - 1f), 1f, 10f);
            speed *= factor;
            acceleration *= factor;
            Vector2 offset = player.Center - Projectile.Center;
            float distance = offset.Length();
            if (distance > 6000f)
            {
                Projectile.Kill();
                return;
            }
            Vector2 wanted = offset.SafeNormalize(Vector2.Zero) * speed;
            Projectile.velocity.X = Approach(Projectile.velocity.X, wanted.X, acceleration);
            Projectile.velocity.Y = Approach(Projectile.velocity.Y, wanted.Y, acceleration);
            if (Projectile.owner == Main.myPlayer && Projectile.Hitbox.Intersects(player.Hitbox))
            {
                Projectile.Kill();
                return;
            }
        }

        if (Projectile.ai[0] == 0f) EarthshakerPaladinVisuals.Flight(Projectile, Kind);
        else Projectile.rotation += .4f * Projectile.direction;
    }

    private static float Approach(float current, float target, float acceleration)
    {
        if (current < target)
        {
            current += acceleration;
            if (current < 0f && target > 0f) current += acceleration;
        }
        else if (current > target)
        {
            current -= acceleration;
            if (current > 0f && target < 0f) current -= acceleration;
        }
        return current;
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        shockwavePending = impacted = true;
        impactPosition = Projectile.position;
        Projectile.ai[0] = 1f;
        Projectile.velocity = -oldVelocity;
        Projectile.netUpdate = true;
        PlayImpact();
        return false;
    }

    private void PlayImpact()
    {
        if (impactPlayed) return;
        impactPlayed = true;
        EarthshakerPaladinVisuals.Shockwave(Projectile, impactPosition, Kind);
        if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Dig, impactPosition);
    }

    public override void ModifyDamageHitbox(ref Rectangle hitbox)
    {
        // Consume once per damage pass, like vanilla localAI[0] = -1. All NPCs
        // in this pass share the shockwave, rather than consuming it on one hit.
        shockwaveDamage = shockwavePending;
        shockwavePending = false;
        if (shockwaveDamage)
        {
            hitbox.Inflate(-Projectile.width / 2, -Projectile.height / 2);
            hitbox.Inflate(100, 100);
        }
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        // 301 also expands its ordinary collision check by 22 pixels per side.
        projHitbox.Inflate(22, 22);
        return projHitbox.Intersects(targetHitbox);
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Projectile.penetrate == 1000) modifiers.SourceDamage *= 1.333f;
        if (shockwaveDamage) modifiers.Knockback *= 1.75f;
        modifiers.HitDirectionOverride = target.Center.X < Main.player[Projectile.owner].Center.X ? -1 : 1;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        target.AddBuff(BuffID.BrokenArmor, 90);
        Projectile.damage = (int)(Projectile.damage * .9f);
        Projectile.netUpdate = true;
        Main.player[Projectile.owner].GetModPlayer<EarthshakerPlayer>().CreditAttack();
    }

    public override void OnHitPlayer(Player target, Player.HurtInfo info) =>
        target.AddBuff(BuffID.BrokenArmor, 60 * Main.rand.Next(10, 21));

    public override bool PreDraw(ref Color lightColor)
    {
        EarthshakerPaladinVisuals.Draw(Projectile, Kind, lightColor);
        return false;
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Projectile.damage);
        writer.Write((sbyte)Projectile.direction);
        writer.Write(impacted);
        if (impacted) { writer.Write(impactPosition.X); writer.Write(impactPosition.Y); }
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.damage = reader.ReadInt32();
        Projectile.direction = reader.ReadSByte() < 0 ? -1 : 1;
        impacted = reader.ReadBoolean();
        if (impacted)
        {
            impactPosition = new Vector2(reader.ReadSingle(), reader.ReadSingle());
            PlayImpact();
        }
    }
}
