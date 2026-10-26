using System;
using ArknightsMod.Content.Items.Weapons.Caster.Haze;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.CoreCasters;

// Ordinary held item staves. These never hide their item sprite or create a holdout.
public abstract class CoreCasterStaff : ExpansionWeaponBase
{
    public abstract CoreCasterKind Kind { get; }
    public override void SetStaticDefaults() => Item.staff[Type] = true;
    public override void SetDefaults()
    {
        Item.width = Item.height = 40;
        Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Magic;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.useTime = Item.useAnimation = Kind == CoreCasterKind.Nightmare ? 28 : 24;
        Item.mana = Kind == CoreCasterKind.Eyjafjalla ? 10 : 8;
        Item.noMelee = Item.autoReuse = true;
        Item.channel = Item.noUseGraphic = false;
        Item.knockBack = 2f;
        Item.shoot = ModContent.ProjectileType<CoreCasterBolt>(); Item.shootSpeed = 10;
        Item.rare = Kind == CoreCasterKind.Eyjafjalla ? ItemRarityID.Lime : ItemRarityID.Pink;
        Item.value = Item.sellPrice(gold: Kind == CoreCasterKind.Eyjafjalla ? 6 : 3);
        Item.UseSound = Kind == CoreCasterKind.Absinthe ? SoundID.Item33 with { Volume = .36f, Pitch = .2f }
            : Kind == CoreCasterKind.Eyjafjalla ? SoundID.Item20 with { Volume = .35f, Pitch = -.25f }
            : SoundID.Item8 with { Volume = .35f, Pitch = -.3f };
    }
    public override bool AltFunctionUse(Player player) => false;
    private bool VolcanoActive(Player player) => Kind == CoreCasterKind.Eyjafjalla
        && player.GetModPlayer<CoreCasterPlayer>().IsActive(this) && player.GetModPlayer<WeaponPlayer>().Skill == 2;
    public override void UseStyle(Player player, Rectangle heldItemFrame)
    {
        if (!VolcanoActive(player)) return;
        player.itemRotation = -MathHelper.PiOver2 * player.direction;
        player.itemLocation = player.MountedCenter + new Vector2(player.direction * 7, -10);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, -MathHelper.Pi);
    }
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        player.GetModPlayer<CoreCasterPlayer>().Handle(this);
    }
    public override bool CanUseItem(Player player)
    {
        var state = player.GetModPlayer<CoreCasterPlayer>();
        if (ArknightsKeybinds.SkillActivatePressed(player)) { state.TryActivate(this); return false; }
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        bool active = state.IsActive(this);
        bool burst = Kind == CoreCasterKind.Absinthe && active && skills.Skill == 1;
        if (burst && CoreCasterRules.Targets(player.MountedCenter, 1100, Main.MouseWorld, true, .5f).Count == 0)
            return false; // No legal target means no attack and no mana consumption.
        if (player.itemAnimation <= 0)
        {
            Item.useTime = burst ? 3 : Kind == CoreCasterKind.Nightmare ? 28 : Kind == CoreCasterKind.Eyjafjalla && active && skills.Skill == 2 ? 6 : 24;
            Item.useAnimation = burst ? 24 : Item.useTime;
            Item.useLimitPerAnimation = burst ? 4 : 1;
            Item.mana = burst ? 3 : Kind == CoreCasterKind.Eyjafjalla && active && skills.Skill == 2 ? 5 : Kind == CoreCasterKind.Eyjafjalla ? 10 : 8;
        }
        return base.CanUseItem(player);
    }
    public override float UseSpeedMultiplier(Player player)
        => Kind == CoreCasterKind.Eyjafjalla && player.GetModPlayer<CoreCasterPlayer>().IsActive(this)
            && player.GetModPlayer<WeaponPlayer>().Skill == 0 ? 1.6f : 1;
    public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
    {
        base.ModifyWeaponDamage(player, ref damage);
        var state = player.GetModPlayer<CoreCasterPlayer>();
        var skills = player.GetModPlayer<WeaponPlayer>();
        if (!state.IsActive(this)) return;
        if (Kind == CoreCasterKind.Absinthe && skills.Skill == 0) damage *= 1.8f;
        if (Kind == CoreCasterKind.Nightmare && skills.Skill == 0) damage *= 1.15f;
        if (Kind == CoreCasterKind.Eyjafjalla && skills.Skill == 0 && state.DuettoUses >= 2) damage *= 1.5f;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        var skills = player.GetModPlayer<WeaponPlayer>();
        var state = player.GetModPlayer<CoreCasterPlayer>();
        bool active = state.IsActive(this);
        Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
        position = player.MountedCenter;
        if (CoreCasterRules.Clear(position, position + aim * 30)) position += aim * 30;
        int mode = active ? skills.Skill + 1 : 0;
        if (Kind == CoreCasterKind.Eyjafjalla && skills.CurrentSkill?.Key.Item == Name && skills.Skill == 1 && skills.StockCount > 0)
        {
            skills.DelStockCount();
            damage = (int)(damage * (2.65f + CoreCasterRules.Rank(skills) * .05f)); mode = 2;
        }
        if (Kind == CoreCasterKind.Absinthe)
        {
            bool burst = Item.useLimitPerAnimation == 4;
            if (burst && (!active || skills.Skill != 1)) return false;
            var targets = CoreCasterRules.Targets(position, 1100, Main.MouseWorld, true, burst ? .5f : 1);
            if (burst && targets.Count == 0) return false;
            NPC target = (active || burst) && targets.Count > 0 ? targets[0] : null;
            Vector2 direction = target == null ? aim : (target.Center - position).SafeNormalize(aim);
            Fire(position, direction * 14, burst ? Math.Max(1, (int)(damage * .62f)) : damage, target);
        }
        else if (Kind == CoreCasterKind.Eyjafjalla && active && skills.Skill == 2)
        {
            // Use the visible staff tip above the raised hand, not the mouse-facing muzzle.
            aim = -Vector2.UnitY;
            position = player.MountedCenter;
            if (CoreCasterRules.Clear(position, position - Vector2.UnitY * 42)) position -= Vector2.UnitY * 42;
            var targets = CoreCasterRules.Targets(position, 1150, Main.MouseWorld);
            int count = Math.Min(CoreCasterRules.Rank(skills) >= 6 ? 6 : 4, targets.Count);
            if (count == 0) Fire(position, Launch(), (int)(damage * 1.5f), null);
            for (int i = 0; i < count; i++)
            {
                Fire(position, Launch(), (int)(damage * 1.5f), targets[i]);
            }
            Vector2 Launch() => (-Vector2.UnitY).RotatedBy(Main.rand.NextFloat(-.65f, .65f)) * Main.rand.NextFloat(28, 36) / 3;
        }
        else Fire(position, aim * Item.shootSpeed, damage, null);
        CoreCasterVisuals.Muzzle(position, aim, Kind, mode > 0);
        return false;

        void Fire(Vector2 start, Vector2 speed, int hit, NPC target)
        {
            // Spawn velocity is already in sub-update units on both the owner and remote clients.
            if (Kind == CoreCasterKind.Absinthe) speed *= .5f;
            Projectile projectile = CoreCasterRules.Spawn(source, start, speed,
                type, hit, knockback, player.whoAmI, (int)Kind, mode, target?.whoAmI + 1 ?? 0, player.GetWeaponCrit(Item));
            if (projectile?.ModProjectile is CoreCasterBolt bolt)
                bolt.SetFlightAim(target?.Center ?? start + (Main.MouseWorld - start).SafeNormalize(aim)
                    * Math.Min(1100, Vector2.Distance(start, Main.MouseWorld)));
        }
    }
}

public sealed class NightmareStaff : CoreCasterStaff
{
    public override CoreCasterKind Kind => CoreCasterKind.Nightmare;
    protected override int[] EliteDamage => [114, 131, 150];
    public override string Texture => "Terraria/Images/Item_" + ItemID.ShadowbeamStaff;
    public override void AddRecipes() => CreateRecipe().AddIngredient<HazeMagicBook>()
        .AddIngredient(ItemID.HallowedBar, 12).AddIngredient(ItemID.SoulofNight, 10).AddIngredient(ItemID.PixieDust, 20)
        .AddCondition(Condition.DownedMechBossAll).AddTile(TileID.MythrilAnvil).Register();
}
public sealed class AbsintheStaff : CoreCasterStaff
{
    public override CoreCasterKind Kind => CoreCasterKind.Absinthe;
    protected override int[] EliteDamage => [98, 112, 128];
    public override string Texture => "Terraria/Images/Item_" + ItemID.EmeraldStaff;
    public override void AddRecipes() => CreateRecipe().AddIngredient<HazeMagicBook>()
        .AddIngredient(ItemID.HallowedBar, 12).AddIngredient(ItemID.Wire, 40).AddIngredient(ItemID.CrystalShard, 15)
        .AddCondition(Condition.DownedMechBossAny).AddTile(TileID.MythrilAnvil).Register();
}
public sealed class EyjafjallaStaff : CoreCasterStaff
{
    public override CoreCasterKind Kind => CoreCasterKind.Eyjafjalla;
    protected override int[] EliteDamage => [128, 148, 172];
    public override string Texture => "Terraria/Images/Item_" + ItemID.InfernoFork;
    public override void AddRecipes() => CreateRecipe().AddIngredient<AbsintheStaff>()
        .AddIngredient(ItemID.SpectreBar, 10).AddIngredient(ItemID.HellstoneBar, 20).AddIngredient(ItemID.LivingFireBlock, 50)
        .AddCondition(Condition.DownedPlantera).AddTile(TileID.MythrilAnvil).Register();
}
