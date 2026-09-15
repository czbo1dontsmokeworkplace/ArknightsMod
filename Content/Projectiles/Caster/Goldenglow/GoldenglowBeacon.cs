using ArknightsMod.Content.Items.Weapons.Caster.Goldenglow;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

// ai[0]: formation slot; ai[1]: target index + 1; ai[2]: returning to owner.
public class GoldenglowBeacon : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Goldenglow/GoldenglowBeacon";
    public const int BaseMaxBeacons = 1;
    private float cooldown;
    private int consecutiveHits;
    private int explosionFailures;
    private int previousSkill = -1;
    private Item sourceItem;
    public static int GetMaxBeacons(Player player)
    {
        var mp = player.GetModPlayer<WeaponPlayer>();
        return BaseMaxBeacons + (mp.SkillActive ? mp.Skill == 2 ? 2 : 1 : 0);
    }

    internal static void EnsureDrones(Player owner)
    {
        if (owner.whoAmI != Main.myPlayer) return;
        int type = ModContent.ProjectileType<GoldenglowBeacon>();
        for (int slot = 0; slot < GetMaxBeacons(owner); slot++)
        {
            bool exists = false;
            foreach (Projectile proj in Main.ActiveProjectiles)
                if (proj.owner == owner.whoAmI && proj.type == type && (int)proj.ai[0] == slot)
                { exists = true; break; }
            if (!exists)
                Projectile.NewProjectile(owner.GetSource_ItemUse(owner.HeldItem), owner.Center,
                    Vector2.Zero, type, 0, 0, owner.whoAmI, slot);
        }
    }

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 24;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 120;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        bool authority = Projectile.owner == Main.myPlayer;
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        // Remote peers follow synchronized positions/targets, never read unsynchronized skill state.
        if (!authority)
        {
            Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.6f);
            return;
        }
        if (owner.HeldItem.ModItem is not GoldenglowWand ||
            (sourceItem != null && sourceItem != owner.HeldItem) || owner.noItems || owner.CCed)
        { Projectile.Kill(); return; }
        if (sourceItem == null)
        {
            sourceItem = owner.HeldItem;
            float initialSpeed = MathHelper.Clamp(owner.GetWeaponAttackSpeed(owner.HeldItem), 0.5f, 2f);
            var initialSkill = owner.GetModPlayer<WeaponPlayer>();
            if (initialSkill.SkillActive && initialSkill.Skill == 0) initialSpeed *= 1.5f;
            cooldown = Math.Max(3f, GoldenglowLightningBalance.NormalPulseTicks / initialSpeed)
                / GoldenglowLightningBalance.FrequencyMultiplier + 1f;
        }
        Projectile.timeLeft = 120;
        var mp = owner.GetModPlayer<WeaponPlayer>();
        int skill = mp.SkillActive ? mp.Skill : -1;
        GoldenglowHeldStaff held = null;
        foreach (Projectile proj in Main.ActiveProjectiles)
            if (proj.owner == owner.whoAmI && proj.ModProjectile is GoldenglowHeldStaff staff)
            { held = staff; break; }
        bool attacking = held != null || skill >= 0;
        bool retire = Projectile.ai[0] >= GetMaxBeacons(owner) || !attacking;
        if (skill != previousSkill)
        {
            if (previousSkill >= 0) ReturnHome();
            previousSkill = skill;
        }
        NPC target = GetTarget();
        if (Projectile.ai[1] > 0 && target == null) ReturnHome();
        if (retire) ReturnHome();
        Vector2 home = owner.Center + new Vector2((Projectile.ai[0] - 1f) * 28f, -55f);
        if (Projectile.ai[2] > 0)
        {
            MoveTo(home, 32f);
            if (Vector2.DistanceSquared(Projectile.Center, home) < 32f * 32f)
            {
                if (retire) { Projectile.Kill(); return; }
                Projectile.ai[2] = 0;
                Projectile.netUpdate = true;
            }
        }
        else
        {
            // Released units keep their target even after it leaves the original acquisition range.
            if (skill < 0 || target == null)
            {
                NPC selected = Acquire(owner, held?.Aim ?? owner.Center, skill);
                int index = selected == null ? 0 : selected.whoAmI + 1;
                if ((int)Projectile.ai[1] != index)
                {
                    Projectile.ai[1] = index;
                    consecutiveHits = 0;
                    if (selected != null)
                        TeleportTo(OrbitPosition(selected, owner));
                    Projectile.netUpdate = true;
                }
                target = selected;
            }
            if (target == null) MoveTo(home, 24f);
            else
            {
                float radius = Math.Max(target.width, target.height) * 0.5f + 48f;
                MoveTo(OrbitPosition(target, owner), 6f);
                if (--cooldown <= 0 && Projectile.Distance(target.Center) <= radius + 100f)
                {
                    Attack(owner, target, skill);
                    float speed = MathHelper.Clamp(owner.GetWeaponAttackSpeed(owner.HeldItem), 0.5f, 2f);
                    if (skill == 0) speed *= 1.5f;
                    cooldown = Math.Max(-1f, cooldown) + Math.Max(3f, GoldenglowLightningBalance.NormalPulseTicks / speed)
                        / GoldenglowLightningBalance.FrequencyMultiplier;
                }
            }
        }
        if (Main.GameUpdateCount % 6 == 0) Projectile.netUpdate = true;
        Lighting.AddLight(Projectile.Center, 0.15f, 0.3f, 0.6f);
    }

    private Vector2 OrbitPosition(NPC target, Player owner)
    {
        float angle = (float)Main.GameUpdateCount * 0.015f + Projectile.ai[0] * MathHelper.TwoPi / GetMaxBeacons(owner);
        float radius = Math.Max(target.width, target.height) * 0.5f + 48f;
        return target.Center + new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius;
    }

    private void TeleportTo(Vector2 destination)
    {
        // Two synchronized visual-only pulses also show departure/arrival to remote players.
        int effect = ModContent.ProjectileType<GoldenglowTeleportEffect>();
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
            effect, 0, 0f, Projectile.owner, 0f);
        Projectile.Center = destination;
        Projectile.velocity = Vector2.Zero;
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), destination, Vector2.Zero,
            effect, 0, 0f, Projectile.owner, 1f);
        Projectile.netUpdate = true;
    }

    private NPC GetTarget()
    {
        int index = (int)Projectile.ai[1] - 1;
        return index >= 0 && index < Main.maxNPCs && Main.npc[index].CanBeChasedBy()
            ? Main.npc[index] : null;
    }
    private static NPC Acquire(Player owner, Vector2 aim, int skill)
    {
        NPC best = null;
        float distance = float.MaxValue;
        float range = skill == 1 ? 1300f : 1000f;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy() || (skill != 2 && npc.DistanceSQ(owner.Center) > range * range)) continue;
            float candidate = Vector2.DistanceSquared(npc.Center, aim);
            if (candidate < distance) { distance = candidate; best = npc; }
        }
        return best;
    }
    private void ReturnHome()
    {
        Projectile.ai[1] = 0;
        Projectile.ai[2] = 1;
        consecutiveHits = 0;
        Projectile.netUpdate = true;
    }
    private void MoveTo(Vector2 destination, float speed)
    {
        Vector2 delta = destination - Projectile.Center;
        Projectile.velocity = delta.SafeNormalize(Vector2.Zero) * Math.Min(speed, delta.Length() * 0.25f);
        Projectile.rotation = Projectile.velocity.X * 0.015f;
    }
    private void Attack(Player owner, NPC target, int skill)
    {
        int damage = owner.GetWeaponDamage(owner.HeldItem);
        var weapon = (GoldenglowWand)owner.HeldItem.ModItem;
        bool explode = skill >= 0 && weapon.EliteStage >= 1 &&
            (explosionFailures >= 40 || Main.rand.NextFloat() < (explosionFailures + 1) * 0.015f);
        float multiplier = explode ? (weapon.EliteStage >= 2 ? 3f : 2f) : Math.Min(1.1f, 0.2f + consecutiveHits * 0.15f);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
            ModContent.ProjectileType<GoldenglowDroneHit>(), (int)(damage * multiplier), 0f,
            owner.whoAmI, target.whoAmI, explode ? 1 : 0, skill == 2 ? 1 : 0);
        // Reuse the established textured lightning as a visual; damage belongs to the targeted hit.
        GoldenglowLightningStrike.Spawn(Projectile.GetSource_FromThis(), Projectile.Center,
            target.Center, owner.whoAmI, 0, 0f, 4);
        if (explode)
        {
            explosionFailures = 0;
            ReturnHome();
        }
        else
        {
            consecutiveHits = Math.Min(6, consecutiveHits + 1);
            if (skill >= 0 && weapon.EliteStage >= 1) explosionFailures++;
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, lightColor,
            Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
        GoldenglowLightningRenderer.DrawFlare(Projectile.Center, new Color(80, 155, 255), 0.18f, 0.8f);
        return false;
    }
}
