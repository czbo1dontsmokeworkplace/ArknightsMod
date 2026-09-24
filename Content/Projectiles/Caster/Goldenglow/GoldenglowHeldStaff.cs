using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Items.Weapons.Caster.Goldenglow;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

public sealed class GoldenglowHeldStaff : ModProjectile
{
    private Vector2 aim;
    private float pulseCooldown;
    private float visualCharge;
    private float visualBurst;
    internal NPC LockedTarget
    {
        get
        {
            Player player = Main.player[Projectile.owner];
            var mp = player.GetModPlayer<WeaponPlayer>();
            float range = mp.SkillActive && mp.Skill == 1 ? 1300f : 1000f;
            return FindTargetNPC(aim, player.Center, range, mp.SkillActive ? 345f : 225f);
        }
    }
    private Item channeledItem;
    internal bool Bursting => GoldenglowLightningBalance.IsBurst((int)Projectile.ai[0]);
    internal Vector2 Aim => aim;
    // Jewel center in the existing 72px sprite, transformed by the same -45-degree upright rotation.
    internal Vector2 Tip => Projectile.Center + new Vector2(-6.6f * Projectile.spriteDirection,
        -37.2f * Main.player[Projectile.owner].gravDir);
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Goldenglow/GoldenglowWand";

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.netImportant = true;
    }

    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(aim.X);
        writer.Write(aim.Y);
    }

    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Vector2 incoming = new(reader.ReadSingle(), reader.ReadSingle());
        if (float.IsFinite(incoming.X) && float.IsFinite(incoming.Y))
            aim = incoming;
    }

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        bool owned = Projectile.owner == Main.myPlayer;
        if (!player.active || player.dead || player.noItems || player.CCed ||
            player.HeldItem.ModItem is not GoldenglowWand || player.selectedItem != (int)Projectile.ai[1] ||
            (owned && (!player.channel || (channeledItem != null && channeledItem != player.HeldItem))))
        {
            Projectile.Kill();
            return;
        }
        channeledItem ??= player.HeldItem;
        Projectile.timeLeft = 2;
        var mp = player.GetModPlayer<WeaponPlayer>();
        float range = mp.SkillActive ? mp.Skill switch { 1 => 1300f, 2 => 1800f, _ => 1000f } : 1000f;
        if (mp.SkillActive && mp.Skill == 2)
        {
            Projectile.Kill();
            return;
        }
        if (owned)
            GoldenglowBeacon.EnsureDrones(player);
        int tick = (int)Projectile.ai[0];
        if (owned)
        {
            Vector2 offset = Main.MouseWorld - player.MountedCenter;
            if (offset.LengthSquared() > range * range)
                offset = offset.SafeNormalize(Vector2.UnitX) * range;
            aim = player.MountedCenter + offset;
            player.ChangeDir(aim.X >= player.Center.X ? 1 : -1);
            if (tick % 6 == 0)
                Projectile.netUpdate = true;
            // The first payment belongs to Item use; all later payments use Terraria's mana hooks.
            if (tick > 0 && tick % GoldenglowLightningBalance.ManaInterval == 0 &&
                !player.CheckMana(player.HeldItem, -1, true))
            {
                player.channel = false;
                Projectile.Kill();
                return;
            }
            player.manaRegenDelay = player.maxRegenDelay;
        }

        // TeslaCoil_Proj: keep a separate held projectile above MountedCenter and lock HoldUp.
        Projectile.spriteDirection = player.direction;
        Projectile.Center = player.RotatedRelativePoint(player.MountedCenter, true) +
            new Vector2(6f * player.direction, -8f * player.gravDir);
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        // ItemUseStyleID.HoldUp supplies the raised arm, as in TeslaCoil; do not override it with a shooting arm.

        bool burst = Bursting;
        bool burstStart = tick % (GoldenglowLightningBalance.ChargeTicks + GoldenglowLightningBalance.BurstTicks)
            == GoldenglowLightningBalance.ChargeTicks;
        if (owned)
        {
            if (tick == 0)
            {
                float initialSpeed = MathHelper.Clamp(player.GetWeaponAttackSpeed(player.HeldItem), 0.5f, 2f);
                if (mp.SkillActive && mp.Skill == 0) initialSpeed *= 1.5f;
                pulseCooldown = Math.Max(3f, GoldenglowLightningBalance.NormalPulseTicks / initialSpeed)
                    / GoldenglowLightningBalance.FrequencyMultiplier;
            }
            else pulseCooldown--;
        }
        if (owned && (pulseCooldown <= 0 || burstStart))
        {
            Vector2 target = LockedTarget?.Center ?? aim;
            int damage = player.GetWeaponDamage(player.HeldItem);
            GoldenglowLightningStrike.Spawn(Projectile.GetSource_FromThis(), Tip, target, player.whoAmI,
                (int)(damage * (burst ? GoldenglowLightningBalance.BurstDamage : 1f)),
                player.GetWeaponKnockback(player.HeldItem), burst ? 1 : 0);
            if (burst)
            {
                // Extend the sky origin by 60%; lightning is an instantaneous path, not a falling projectile.
                Vector2 sky = target - Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-MathHelper.Pi / 36f,
                    MathHelper.Pi / 36f)) * 1200f;
                GoldenglowLightningStrike.Spawn(Projectile.GetSource_FromThis(), sky, target,
                    player.whoAmI, (int)(damage * GoldenglowLightningBalance.SkyDamage), 8f, 2);
            }
            float speed = MathHelper.Clamp(player.GetWeaponAttackSpeed(player.HeldItem), 0.5f, 2f);
            if (mp.SkillActive && mp.Skill == 0)
                speed *= 1.5f;
            // Keep fractional ticks so +30% is not rounded to a different attack speed.
            float interval = Math.Max(3f, (burst ? GoldenglowLightningBalance.BurstPulseTicks :
                GoldenglowLightningBalance.NormalPulseTicks) / speed) / GoldenglowLightningBalance.FrequencyMultiplier;
            pulseCooldown = (burstStart ? 0f : Math.Max(-1f, pulseCooldown)) + interval;
        }
        if (!Main.dedServ)
        {
            visualCharge = MathHelper.Lerp(visualCharge, GoldenglowLightningBalance.Charge(tick), 0.12f);
            visualBurst = MathHelper.Lerp(visualBurst, burst ? 1f : 0f, 0.12f);
            float charge = visualCharge;
            Lighting.AddLight(Tip, new Vector3(0.25f, 0.5f, 1f) * (0.7f + charge));
            if (tick % 3 == 0)
            {
                Vector2 radial = Main.rand.NextVector2Unit();
                Dust dust = Dust.NewDustPerfect(Tip + radial * (18f + 20f * charge), DustID.Electric,
                    -radial * (2f + charge * 3f), 100, Color.LightSkyBlue, MathHelper.Lerp(0.65f, 1.2f, visualBurst));
                dust.noGravity = true;
            }
            if (burstStart)
                SoundEngine.PlaySound(SoundID.Thunder with { Volume = 0.55f, Pitch = 0.15f,
                    MaxInstances = 2 }, Tip);
        }
        Projectile.ai[0]++;
    }

    internal static NPC FindTargetNPC(Vector2 cursor, Vector2 playerCenter, float range, float snap)
    {
        NPC best = null;
        float distance = snap * snap;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy() || npc.DistanceSQ(playerCenter) > range * range)
                continue;
            float current = Vector2.DistanceSquared(cursor, npc.Hitbox.ClosestPointInRect(cursor));
            if (current < distance)
            {
                distance = current;
                best = npc;
            }
        }
        return best;
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        Texture2D texture = TextureAssets.Projectile[Type].Value;
        // Existing art runs bottom-left to top-right; -45 degrees makes its staff axis upright.
        float rotation = -MathHelper.PiOver4 * player.direction * player.gravDir;
        VerticalStaffBase.DrawHeldStaff(player, texture, Projectile.Center, lightColor,
            new Vector2(0f), rotation, new Vector2(0.28f, 0.75f));
        float charge = visualCharge;
        GoldenglowLightningRenderer.DrawFlare(Tip, new Color(80, 160, 255), 0.24f + charge * 0.28f,
            MathHelper.Lerp(0.7f, 1f, visualBurst));
        return false;
    }
}
