using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Sniper.Catapult;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using ArknightsMod.Content.Items.Weapons.Sniper.Fiammetta;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Catapult;

/// <summary>前期炮手：不消耗火箭弹；技能只扩范围，不叠伤害或伤害破片。</summary>
public sealed class CatapultLauncher : ExpansionWeaponBase
{
    internal const int AttackInterval = 64;
    internal const float MaximumRange = ArtilleryBallistics.MaximumRange;
    internal const float BlastRadius = ArtilleryBlast.Radius;
    protected override int[] EliteDamage => [46, 56];
    // TODO: 正式空爆武器贴图；手持绘制与物品使用同一原版占位图。
    public override string Texture => "Terraria/Images/Item_" + ItemID.GrenadeLauncher;

    public override string GetSkillActivateKeyHint() => string.IsNullOrEmpty(ArknightsKeybinds.SkillActivateKeyDisplay)
        ? Language.GetTextValue("Mods.ArknightsMod.CatapultText.Unbound")
        : Language.GetTextValue("Mods.ArknightsMod.CatapultText.SkillKey", ArknightsKeybinds.SkillActivateKeyDisplay);

    public override void ModifyTooltips(System.Collections.Generic.List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(new TooltipLine(Mod, "CatapultSkillKey", GetSkillActivateKeyHint()));
        tooltips.Add(new TooltipLine(Mod, "CatapultPlaceholder", Language.GetTextValue("Mods.ArknightsMod.CatapultText.Placeholder")));
    }

    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Ranged;
        Item.width = 48;
        Item.height = 22;
        Item.useTime = Item.useAnimation = AttackInterval;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noUseGraphic = Item.noMelee = true;
        Item.autoReuse = true;
        Item.knockBack = 4f;
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.sellPrice(silver: 80);
        Item.shoot = ModContent.ProjectileType<CatapultShell>();
        Item.shootSpeed = ArtilleryBallistics.LaunchSpeed;
        Item.useAmmo = AmmoID.None;
    }

    public override bool CanUseItem(Player player)
    {
        if (!ArknightsKeybinds.SkillActivatePressed(player)) return base.CanUseItem(player);
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        if (player.whoAmI == Main.myPlayer && skills.CurrentSkill != null
            && skills.StockCount > 0 && !skills.SkillActive)
        {
            skills.SkillActive = true;
            skills.SkillTimer = 0;
            skills.DelStockCount();
            SoundEngine.PlaySound(SoundID.Item4 with { Volume = .45f }, player.Center);
        }
        return false;
    }

    internal static float RangeMultiplier(int level) => level >= 7 ? 2f : level >= 4 ? 1.75f : 1.5f;

    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
        Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        Vector2 target = FiammettaHandCannon.ClampTarget(player, Main.MouseWorld, MaximumRange);
        target = Vector2.Clamp(target, new Vector2(16f), new Vector2(Main.maxTilesX * 16f - 16f, Main.maxTilesY * 16f - 16f));
        Vector2 aim = ArtilleryBallistics.LaunchDirection(player, target);
        Vector2 muzzle = CatapultHoldout.Muzzle(player, aim);
        velocity = aim * ArtilleryBallistics.LaunchSpeed;
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        int level = skills.CurrentSkill?.ForceReplaceLevel ?? skills.CurrentSkill?.Level ?? 1;
        float radius = BlastRadius * (skills.SkillActive ? RangeMultiplier(level) : 1f);
        foreach (Projectile old in Main.ActiveProjectiles)
            if (old.owner == player.whoAmI && old.type == ModContent.ProjectileType<CatapultHoldout>()) old.Kill();
        Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(aim),
            ModContent.ProjectileType<CatapultHoldout>(), 0, 0f, player.whoAmI);
        int shell = Projectile.NewProjectile(source, muzzle, velocity, ModContent.ProjectileType<CatapultShell>(),
            damage, knockback, player.whoAmI, target.X, target.Y, radius);
        if (Main.projectile.IndexInRange(shell)) Main.projectile[shell].CritChance = player.GetWeaponCrit(Item);
        SoundEngine.PlaySound(SoundID.Item61 with { Volume = .55f, Pitch = -.15f, MaxInstances = 3 }, muzzle);
        return false;
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient(ItemID.Handgun)
        .AddIngredient(ItemID.Bone, 50)
        .AddIngredient(ItemID.Grenade, 25)
        .AddRecipeGroup("IronBar", 8)
        .AddCondition(Condition.DownedSkeletron)
        .AddTile(TileID.Anvils).Register();
}
