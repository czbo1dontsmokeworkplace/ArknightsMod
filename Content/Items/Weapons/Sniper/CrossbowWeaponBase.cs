using System;
using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using ArknightsMod.Content.Projectiles.Sniper.Pozemka;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper;

public abstract class CrossbowWeaponBase : UpgradeWeaponBase
{
    public abstract CrossbowKind Kind { get; }
    internal CrossbowCadence Cadence = new();
    internal int SkillShots;
    internal int ForcedShots;
    private bool consumingShot;
    private bool skillKeyWasDown;

    public override ModItem Clone(Item newEntity)
    {
        var clone = (CrossbowWeaponBase)base.Clone(newEntity);
        clone.Cadence = new CrossbowCadence();
        clone.ForcedShots = clone.SkillShots = 0;
        clone.consumingShot = clone.skillKeyWasDown = false;
        return clone;
    }

    protected void SetCrossbowDefaults()
    {
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = Item.channel = Item.autoReuse = true;
        Item.useAmmo = AmmoID.Arrow;
        // PickAmmo must see a wooden arrow here. The hand projectile is spawned explicitly.
        Item.shoot = ProjectileID.WoodenArrowFriendly;
        Item.useTime = Item.useAnimation = Kind == CrossbowKind.Schwarz ? 45 : 10;
        Item.reuseDelay = 0;
        Item.consumeAmmoOnLastShotOnly = false;
        Item.UseSound = null;
    }

    // Creating a holdout does not fire or spend an arrow. Each actual shot calls PickAmmo once.
    public override bool CanConsumeAmmo(Item ammo, Player player) => consumingShot;
    public override bool AltFunctionUse(Player player) => Kind == CrossbowKind.Pozemka;
    public override bool NeedsAmmo(Player player) => !(Kind == CrossbowKind.Pozemka && player.altFunctionUse == 2);

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        if (player.whoAmI != Main.myPlayer) return;
        bool pressed = ArknightsKeybinds.SkillActivatePressed(player);
        if (pressed && !skillKeyWasDown) TryActivateSkill(player);
        skillKeyWasDown = pressed;
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (Kind == CrossbowKind.Pozemka) skills.SummonMode = false;
        if (!skills.SkillActive) SkillShots = 0;
        if (Kind == CrossbowKind.KroosAlter && skills.Skill == 0 && skills.SkillActive)
            player.aggro -= 1250;
        if (ForcedShots > 0 && player.ownedProjectileCounts[ModContent.ProjectileType<CrossbowHoldout>()] == 0)
            SpawnHoldout(player, player.GetSource_ItemUse(Item));
    }

    public override bool CanUseItem(Player player)
    {
        if (Kind == CrossbowKind.Pozemka && player.altFunctionUse == 2)
            return base.CanUseItem(player);
        if (ArknightsKeybinds.SkillActivatePressed(player))
            return false; // HoldItem handles activation even while channeling.
        return player.ownedProjectileCounts[ModContent.ProjectileType<CrossbowHoldout>()] == 0
            && base.CanUseItem(player);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (Kind == CrossbowKind.Pozemka && player.altFunctionUse == 2)
        {
            // Deployment remains a separate action; the typewriter is not a hand projectile.
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.owner == player.whoAmI && p.type == ModContent.ProjectileType<PozemkaCrossbow_Sentry>()) p.Kill();
            Projectile.NewProjectile(source, Main.MouseWorld, Vector2.Zero,
                ModContent.ProjectileType<PozemkaCrossbow_Sentry>(), damage, knockback, player.whoAmI);
            skills.SummonMode = false;
            player.UpdateMaxTurrets();
            return false;
        }
        SpawnHoldout(player, source);
        return false;
    }

    private void SpawnHoldout(Player player, IEntitySource source)
    {
        if (player.dead || player.noItems || player.CCed || !player.HasAmmo(Item)) { ForcedShots = 0; return; }
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        Projectile.NewProjectile(source, player.MountedCenter, aim,
            ModContent.ProjectileType<CrossbowHoldout>(), 0, 0f, player.whoAmI, (int)Kind, Item.type);
    }

    private void TryActivateSkill(Player player)
    {
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (Kind == CrossbowKind.Kroos || (Kind != CrossbowKind.KroosAlter && skills.Skill == 0)
            || skills.SkillActive || skills.StockCount <= 0 || skills.SummonMode) return;
        if (Kind == CrossbowKind.Pozemka && skills.Skill == 1 && !player.HasAmmo(Item)) return;
        Activate(skills);
        if (Kind == CrossbowKind.Pozemka && skills.Skill == 1) ForcedShots = 3;
    }

    private static void Activate(WeaponPlayer skills)
    {
        skills.SkillActive = true;
        skills.SkillTimer = 0;
        skills.DelStockCount();
        if (!Main.dedServ) SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/SkillActive1")
            { Volume = .4f, MaxInstances = 3 }, skills.Player.Center);
    }

    internal bool Fire(Player player, Vector2 muzzle, Vector2 direction)
    {
        if (player.whoAmI != Main.myPlayer || !player.HasAmmo(Item)) return false;
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (skills.Skill == 0 && Kind != CrossbowKind.KroosAlter && !skills.SkillActive && skills.StockCount > 0)
            Activate(skills);
        int type, damage, ammo;
        float speed, knockback;
        consumingShot = true;
        bool picked;
        try { picked = player.PickAmmo(Item, out type, out speed, out damage, out knockback, out ammo); }
        finally { consumingShot = false; }
        if (!picked) return false;
        speed *= 1.35f;

        int skill = skills.SkillActive ? skills.Skill + 1 : 0;
        if (ForcedShots > 0) skill = 2; // Snapshot the whole S2 burst, even if its UI timer expires.
        float multiplier = Kind == CrossbowKind.Kroos ? .8f : 1f;
        if (Kind == CrossbowKind.Kroos && skill == 1) multiplier *= 1.4f;
        if (Kind == CrossbowKind.KroosAlter && skill == 1) multiplier *= 1.4f;
        if (Kind == CrossbowKind.Schwarz)
            multiplier *= skill switch {
                1 => 2.2f * (Main.rand.NextFloat() < .8f ? 1.6f : 1f),
                2 => 2.3f * (Main.rand.NextBool() ? 1.6f : 1f),
                3 => 2.8f * 1.6f, _ => 1f };
        if (Kind == CrossbowKind.Pozemka && skill == 1) multiplier *= 1.6f;
        if (Kind == CrossbowKind.Pozemka && skill == 2) multiplier *= 2.3f;

        // Only wooden-arrow projectile types are converted (also covers the Endless Quiver).
        bool converted = type == ProjectileID.WoodenArrowFriendly;
        if (converted) type = ModContent.ProjectileType<CrossbowBolt>();
        var shot = Projectile.NewProjectileDirect(player.GetSource_ItemUse_WithPotentialAmmo(Item, ammo),
            muzzle, direction * speed, type, Math.Max(1, (int)(damage * multiplier)), knockback,
            player.whoAmI, converted ? (int)Kind : 0f, converted ? skill : 0f);
        shot.CritChance = player.GetWeaponCrit(Item);
        var data = shot.GetGlobalProjectile<CrossbowShotData>();
        data.Kind = (int)Kind;
        data.Skill = skill;
        shot.netUpdate = true;

        float interval = 1f;
        if (Kind == CrossbowKind.Kroos && skill == 1) interval = .5f;
        if (Kind == CrossbowKind.KroosAlter && skill != 0)
            interval = skill == 2 && SkillShots >= 32 ? .25f : .5f;
        if (Kind == CrossbowKind.Pozemka && skill == 3) interval = 2f / 3f;
        Cadence.Fired(Main.GameUpdateCount, Kind, player.GetTotalAttackSpeed(DamageClass.Ranged), interval);
        if (Kind == CrossbowKind.KroosAlter && skill == 2) SkillShots++;
        if (ForcedShots > 0) ForcedShots--;
        if (skills.Skill == 0 && !skills.SkillActive) skills.OffensiveRecovery();
        CrossbowVisuals.SpawnPulse(shot.GetSource_FromThis(), muzzle, direction, Kind, true);
        return true;
    }

    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        string text = Language.GetTextValue("Mods.ArknightsMod.Crossbows." + Kind);
        int quote = tooltips.FindIndex(line => line.Mod == "Terraria" && line.Name == "Tooltip0");
        tooltips.Insert(quote >= 0 ? quote + 1 : tooltips.Count, new TooltipLine(Mod, "CrossbowMechanics", text));
    }
}
