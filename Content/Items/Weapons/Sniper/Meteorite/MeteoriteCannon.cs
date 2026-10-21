using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Sniper.Meteorite;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Meteorite;

/// <summary>陨星的跨射爆破弩炮；无需弹药，使用专属爆破弹。</summary>
public sealed class MeteoriteCannon : ExpansionWeaponBase
{
    protected override int[] EliteDamage => [276, 317, 364]; // +10%, rounded to the nearest integer
    private static readonly SoundStyle FireSound = new("ArknightsMod/Sounds/MeteoriteCannonFire")
    { Volume = .64f, MaxInstances = 3 };
    private static readonly SoundStyle SkillSound = new("ArknightsMod/Sounds/SkillActive1")
    { Volume = .46f, MaxInstances = 3 };

    // 专属榴弹弩贴图也供手持弹幕使用。
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Sniper/Meteorite/MeteoriteCannon";

    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Ranged;
        Item.width = 54;
        Item.height = 24;
        Item.useTime = Item.useAnimation = 70;
        Item.noUseGraphic = true;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.knockBack = 5f;
        Item.value = Item.sellPrice(gold: 1);
        Item.rare = ItemRarityID.Yellow;
        Item.autoReuse = true;
        Item.noMelee = true;
        Item.useAmmo = AmmoID.None;
        Item.shoot = ModContent.ProjectileType<MeteoriteShell>();
        Item.shootSpeed = ArtilleryBallistics.LaunchSpeed;
        Item.UseSound = null;
    }

    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player))
        {
            WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
            if (skills.Skill == 1 && skills.StockCount > 0 && !skills.SkillActive)
                ArmSkill(skills, player);
            return false;
        }
        return base.CanUseItem(player);
    }

    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (player.whoAmI == Main.myPlayer && skills.Skill == 0
            && skills.StockCount > 0 && !skills.SkillActive)
            ArmSkill(skills, player);
    }

    private static void ArmSkill(WeaponPlayer skills, Player player)
    {
        skills.SkillActive = true;
        skills.SkillTimer = 0;
        skills.DelStockCount();
        if (!Main.dedServ)
            SoundEngine.PlaySound(SkillSound, player.Center);
    }

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        int mode = skills.SkillActive ? skills.Skill + 1 : 0;
        Vector2 target = global::ArknightsMod.Content.Items.Weapons.Sniper.Fiammetta.FiammettaHandCannon.ClampTarget(player, Main.MouseWorld,
            global::ArknightsMod.Content.Items.Weapons.Sniper.Fiammetta.FiammettaHandCannon.NormalMaximumRange);
        Vector2 direction = ArtilleryBallistics.LaunchDirection(player, target);
        // No ammunition damage or consumption; apply the weapon's skill multiplier once.
        int shellDamage = mode switch
        {
            1 => (int)(damage * 1.45f),
            2 => (int)(damage * 2.1f),
            _ => damage
        };
        foreach (Projectile old in Main.ActiveProjectiles)
            if (old.owner == player.whoAmI && old.type == ModContent.ProjectileType<MeteoriteHoldout>()) old.Kill();
        Projectile.NewProjectile(source, player.MountedCenter, direction, ModContent.ProjectileType<MeteoriteHoldout>(), 0, 0f, player.whoAmI);
        Vector2 muzzle = player.MountedCenter + Collision.TileCollision(player.MountedCenter - new Vector2(4f), direction * 30f, 8, 8, true, true);
        int shell = Projectile.NewProjectile(source, muzzle, direction * ArtilleryBallistics.LaunchSpeed,
            ModContent.ProjectileType<MeteoriteShell>(), shellDamage, knockback,
            player.whoAmI, target.X, target.Y, mode);
        if (Main.projectile.IndexInRange(shell)) Main.projectile[shell].CritChance = player.GetWeaponCrit(Item);
        if (!Main.dedServ)
            SoundEngine.PlaySound(FireSound, player.Center);
        skills.OffensiveRecovery();
        if (mode != 0)
        {
            skills.SkillActive = false;
            skills.SkillTimer = 0;
        }
        return false;
    }

    public override void AddRecipes()
    {
        // 同系列前期武器可向上升级；保留已有两种连弩配方。
        CreateRecipe()
            .AddIngredient<global::ArknightsMod.Content.Items.Weapons.Sniper.Catapult.CatapultLauncher>()
            .AddIngredient(ItemID.ShroomiteBar, 12)
            .AddIngredient(ItemID.SoulofNight, 8)
            .AddIngredient(ItemID.ExplosivePowder, 12)
            .AddTile(TileID.MythrilAnvil)
            .Register();
        CreateRecipe()
            .AddIngredient(ItemID.MythrilRepeater)
            .AddIngredient(ItemID.ShroomiteBar, 12)
            .AddIngredient(ItemID.SoulofNight, 8)
            .AddIngredient(ItemID.ExplosivePowder, 12)
            .AddTile(TileID.MythrilAnvil)
            .Register();
        CreateRecipe()
            .AddIngredient(ItemID.OrichalcumRepeater)
            .AddIngredient(ItemID.ShroomiteBar, 12)
            .AddIngredient(ItemID.SoulofNight, 8)
            .AddIngredient(ItemID.ExplosivePowder, 12)
            .AddTile(TileID.MythrilAnvil)
            .Register();
    }
}
