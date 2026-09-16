using System;
using ArknightsMod.Content.Items.Weapons.Specialist.Red;
using ArknightsMod.Content.Projectiles.Specialist.Phantom;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Specialist.Phantom;

public sealed class PhantomPlayer : ModPlayer
{
    internal int DeployCooldown { get; private set; }
    internal int EchoCooldown { get; private set; }
    private int previousSlot = -1;
    private bool previousWeapon, wasDashing, initialized, teleport, skillHeld, echoHeld;
    private Vector2 previousVelocity;
    private float fallSpeed;
    private int pendingAbsorb;
    private PhantomPerformer pendingGuard;
    private ulong clock = ulong.MaxValue;
    internal bool Holding => Player.HeldItem.ModItem is PhantomDaggers;
    internal PhantomPerformer Performer(bool echo)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Player.whoAmI && p.ModProjectile is PhantomPerformer performer && performer.IsEcho == echo)
                return performer;
        return null;
    }
    public override void Load() => On_Player.Teleport += ObserveTeleport;
    public override void Unload() => On_Player.Teleport -= ObserveTeleport;
    private static void ObserveTeleport(On_Player.orig_Teleport orig, Player player, Vector2 destination, int style, int extraInfo)
    {
        Vector2 before = player.position;
        orig(player, destination, style, extraInfo);
        if (player.whoAmI == Main.myPlayer && RedDeploymentState.IsCombatTeleport(style)
            && Vector2.DistanceSquared(before, player.position) > 1f)
        {
            var state = player.GetModPlayer<PhantomPlayer>();
            if (state.Holding) state.teleport = true;
        }
    }
    private void Tick()
    {
        if (clock == Main.GameUpdateCount) return;
        clock = Main.GameUpdateCount;
        DeployCooldown = Math.Max(0, DeployCooldown - 1);
        EchoCooldown = Math.Max(0, EchoCooldown - 1);
    }
    public override void PreUpdate()
    {
        Tick();
        fallSpeed = Math.Max(0, Player.velocity.Y * Player.gravDir);
    }
    public override void PostUpdateEquips()
    {
        if (!Holding || Player.dead) return;
        Player.moveSpeed *= 1.15f;
        Player.jumpSpeedBoost += (Terraria.Player.jumpSpeed + Player.jumpSpeedBoost) * .15f;
    }
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        bool key = ArknightsKeybinds.SkillActivatePressed(Player);
        bool right = Main.mouseRight && !Player.mouseInterface && !Main.blockMouse && !Main.mapFullscreen;
        bool dash = Player.dashDelay < 0;
        bool switched = Holding && previousWeapon && Player.selectedItem is >= 0 and < 10 && Player.selectedItem != previousSlot;
        bool dashStart = initialized && RedDeploymentState.IsDashStart(wasDashing, dash,
            previousVelocity.X, Player.velocity.X, Player.controlLeft || Player.controlRight,
            !Player.mount.Active && Player.grappling[0] < 0 && Player.immuneTime <= 0);
        bool landed = initialized && Math.Max(fallSpeed, previousVelocity.Y * Player.gravDir) >= 8f
            && Math.Abs(Player.velocity.Y) < .1f
            && Math.Abs(Collision.TileCollision(Player.position, new Vector2(0, 2 * Player.gravDir),
                Player.width, Player.height, false, false, (int)Player.gravDir).Y) < 1f;
        if (Holding)
        {
            if ((key && !skillHeld) || switched || dashStart || landed || teleport) TryDeploy(false);
            if (right && !echoHeld) TryDeploy(true);
            UpdateDisplay();
        }
        teleport = false;
        skillHeld = key;
        echoHeld = right;
        previousSlot = Player.selectedItem;
        previousWeapon = !Holding && Player.selectedItem is >= 0 and < 10 && Player.HeldItem.damage > 0;
        previousVelocity = Player.velocity;
        wasDashing = dash;
        initialized = !Player.dead;
    }
    internal bool TryDeploy(bool echo)
    {
        if (Player.whoAmI != Main.myPlayer || !Holding || Player.dead || Player.CCed || Player.noItems
            || (echo ? EchoCooldown : DeployCooldown) > 0 || Performer(echo) != null) return false;
        var skill = Player.GetModPlayer<WeaponPlayer>();
        if (skill.CurrentSkill?.Key.Item != nameof(PhantomDaggers) || skill.Skill is < 0 or > 2) return false;
        Vector2 position = Player.MountedCenter;
        if (echo)
        {
            Vector2 offset = Main.MouseWorld - position;
            if (offset.Length() > PhantomBalance.DeployRange)
                offset = offset.SafeNormalize(Vector2.UnitX) * PhantomBalance.DeployRange;
            // 沿射线逐步找到最后一个可达位置，不能隔墙部署。
            for (float step = 8; step <= offset.Length(); step += 8)
            {
                Vector2 candidate = Player.MountedCenter + offset.SafeNormalize(Vector2.UnitX) * step;
                if (Collision.SolidCollision(candidate - new Vector2(12, 24), 24, 48)
                    || !Collision.CanHitLine(Player.MountedCenter, 1, 1, candidate, 1, 1)) break;
                position = candidate;
            }
        }
        int rank = PhantomBalance.Rank((skill.CurrentSkill.ForceReplaceLevel ?? skill.CurrentSkill.Level) - 1);
        int damage = Player.GetWeaponDamage(Player.HeldItem);
        if (echo) damage = (int)MathF.Round(damage * PhantomBalance.EchoDamage);
        int index = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), position, Vector2.Zero,
            ModContent.ProjectileType<PhantomPerformer>(), damage, 2f, Player.whoAmI, echo ? 1 : 0, skill.Skill, rank);
        if (!Main.projectile.IndexInRange(index)) return false;
        Main.projectile[index].CritChance = Player.GetWeaponCrit(Player.HeldItem);
        if (echo) EchoCooldown = PhantomBalance.EchoCooldown;
        else DeployCooldown = PhantomBalance.Redeploy;
        return true;
    }
    private void UpdateDisplay()
    {
        var skill = Player.GetModPlayer<WeaponPlayer>();
        if (skill.CurrentSkill?.Key.Item != nameof(PhantomDaggers)) return;
        PhantomPerformer active = Performer(false);
        skill.SkillActive = active != null;
        skill.SkillTimer = active?.Age ?? 0;
        skill.Div = 60;
        skill.SkillChargeMax = PhantomBalance.Redeploy;
        skill.SkillCharge = PhantomBalance.Redeploy - DeployCooldown;
        skill.SP = skill.SkillCharge / 60;
        skill.StockCount = DeployCooldown == 0 ? 1 : 0;
    }
    private PhantomPerformer Guard(bool requireShield = true)
    {
        PhantomPerformer guard = Performer(false);
        if (guard?.Skill == 0 && (!requireShield || guard.Shield > 0)) return guard;
        guard = Performer(true);
        return guard?.Skill == 0 && (!requireShield || guard.Shield > 0)
            && Vector2.DistanceSquared(guard.Projectile.Center, Player.Center) <= 240f * 240f ? guard : null;
    }
    public override bool FreeDodge(Player.HurtInfo info)
    {
        if (Player.whoAmI != Main.myPlayer || !Holding || !IsCombatHit(info)) return false;
        PhantomPerformer guard = Guard(false);
        if (guard == null || Main.rand.NextFloat() >= PhantomBalance.Dodge(guard.Rank)) return false;
        Player.immune = true;
        Player.immuneTime = Math.Max(Player.immuneTime, 20);
        PhantomVisuals.Burst(Player.Center, 10);
        return true;
    }
    private static bool IsCombatHit(Player.HurtInfo info) =>
        info.DamageSource != null && (info.DamageSource.SourceNPCIndex >= 0 || info.DamageSource.SourceProjectileLocalIndex >= 0);
    public override void ModifyHurt(ref Player.HurtModifiers modifiers)
    {
        pendingAbsorb = 0;
        pendingGuard = null;
        if (Player.whoAmI != Main.myPlayer || !Holding) return;
        modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
        {
            PhantomPerformer guard = Guard();
            if (guard == null || info.Cancelled || !IsCombatHit(info) || info.Damage <= guard.Shield) return;
            // 部分吸收仅预扣伤害，真正命中后才扣盾；闪避不会消耗护盾。
            pendingGuard = guard;
            pendingAbsorb = guard.Shield;
            info.Damage -= pendingAbsorb;
        };
    }
    public override bool ConsumableDodge(Player.HurtInfo info)
    {
        if (Player.whoAmI != Main.myPlayer || !Holding || pendingAbsorb > 0 || !IsCombatHit(info)) return false;
        PhantomPerformer guard = Guard();
        if (guard == null || info.Damage <= 0 || guard.Shield < info.Damage) return false;
        guard.Shield -= info.Damage;
        guard.Projectile.netUpdate = true;
        Player.immune = true;
        Player.immuneTime = Math.Max(Player.immuneTime, 20);
        PhantomVisuals.Burst(Player.Center, 6);
        return true;
    }
    public override void OnHurt(Player.HurtInfo info)
    {
        if (Player.whoAmI == Main.myPlayer && pendingGuard != null && pendingAbsorb > 0)
        {
            pendingGuard.Shield = Math.Max(0, pendingGuard.Shield - pendingAbsorb);
            pendingGuard.Projectile.netUpdate = true;
            PhantomVisuals.Burst(Player.Center, 6);
        }
        pendingGuard = null;
        pendingAbsorb = 0;
    }
    internal void ApplyEmergencyOneSecondCharge()
    {
        if (!Holding) return;
        DeployCooldown = Math.Max(0, DeployCooldown - PhantomBalance.Redeploy / 60);
        EchoCooldown = Math.Max(0, EchoCooldown - PhantomBalance.EchoCooldown / 60);
    }
    public override void UpdateDead()
    {
        Tick();
        initialized = previousWeapon = teleport = wasDashing = false;
        previousSlot = -1;
    }
}
