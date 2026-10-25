using System.Collections.Generic;
using ArknightsMod.Content.Projectiles.Vanguard.StandardBearers;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Vanguard.StandardBearers;

public abstract class StandardBearerWeapon : ExpansionWeaponBase
{
    public abstract StandardBearerKind Kind { get; }
    // Temporary existing polearm art; the flag cloth is intentionally not animated.
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Vanguard/Fang/FangSpear";
    public override void SetDefaults()
    {
        Item.width = Item.height = 56; Item.damage = EliteDamage[0];
        Item.DamageType = DamageClass.Summon; Item.knockBack = 2;
        Item.useTime = Item.useAnimation = StandardBearerRules.AttackFrames;
        Item.useStyle = ItemUseStyleID.Shoot;
        Item.noMelee = Item.noUseGraphic = Item.autoReuse = true;
        Item.useTurn = false; Item.shootSpeed = 1;
        Item.shoot = ModContent.ProjectileType<StandardBearerHoldout>();
        Item.rare = new[] { ItemRarityID.Green, ItemRarityID.LightRed, ItemRarityID.Pink, ItemRarityID.Yellow }[(int)Kind];
        Item.value = Item.sellPrice(gold: 1 + (int)Kind * 2);
    }
    public override bool AltFunctionUse(Player player) => false;
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        if (player.whoAmI == Main.myPlayer && !player.dead && !player.CCed && !player.noItems
            && player.ownedProjectileCounts[ModContent.ProjectileType<StandardBearerHoldout>()] == 0)
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.MountedCenter, Vector2.Zero,
                ModContent.ProjectileType<StandardBearerHoldout>(), 0, Item.knockBack, player.whoAmI, (int)Kind, -1);
    }
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player) || StandardBearerPlayer.IsPhased(player)) return false;
        return StandardBearerHoldout.Find(player)?.Idle == true;
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position,
        Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI == Main.myPlayer)
            StandardBearerHoldout.Find(player)?.Thrust((Main.MouseWorld - player.MountedCenter)
                .SafeNormalize(Vector2.UnitX * player.direction).ToRotation());
        return false;
    }
    public override string GetSkillActivateKeyHint() => Language.GetTextValue("Mods.ArknightsMod.StandardBearers.SkillKey",
        ArknightsKeybinds.SkillActivateKeyDisplay);
    public override void ModifyTooltips(List<TooltipLine> tooltips)
    {
        base.ModifyTooltips(tooltips);
        tooltips.Add(new TooltipLine(Mod, "StandardBearerAura", Language.GetTextValue("Mods.ArknightsMod.StandardBearers.Aura",
            5 + (int)Kind * 2, 2 + (int)Kind * 2, 3 + (int)Kind * 2)));
        tooltips.Add(new TooltipLine(Mod, "StandardBearerControls", Language.GetTextValue("Mods.ArknightsMod.StandardBearers.Controls")));
        tooltips.Add(new TooltipLine(Mod, "StandardBearerSkill", GetSkillActivateKeyHint()));
    }
}

public sealed class MyrtleStandard : StandardBearerWeapon
{
    public override StandardBearerKind Kind => StandardBearerKind.Myrtle;
    protected override int[] EliteDamage => [12, 15, 18];
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.Spear).AddIngredient(ItemID.Silk, 8)
        .AddIngredient(ItemID.LifeCrystal).AddTile(TileID.Anvils).Register();
}
public sealed class ElysiumStandard : StandardBearerWeapon
{
    public override StandardBearerKind Kind => StandardBearerKind.Elysium;
    protected override int[] EliteDamage => [24, 29, 34];
    public override void AddRecipes() => CreateRecipe().AddIngredient<MyrtleStandard>()
        .AddRecipeGroup(OperatorWeaponRecipeGroups.CobaltOrPalladiumBar, 12).AddIngredient(ItemID.SoulofLight, 8)
        .AddTile(TileID.Anvils).Register();
}
public sealed class JacintStandard : StandardBearerWeapon
{
    public override StandardBearerKind Kind => StandardBearerKind.Jacint;
    protected override int[] EliteDamage => [35, 41, 48];
    public override void AddRecipes() => CreateRecipe().AddIngredient<ElysiumStandard>().AddIngredient(ItemID.HallowedBar, 12)
        .AddIngredient(ItemID.SoulofSight, 8).AddTile(TileID.MythrilAnvil).Register();
}
public sealed class SaileachStandard : StandardBearerWeapon
{
    public override StandardBearerKind Kind => StandardBearerKind.Saileach;
    protected override int[] EliteDamage => [48, 56, 65];
    public override void AddRecipes() => CreateRecipe().AddIngredient<JacintStandard>().AddIngredient(ItemID.ChlorophyteBar, 12)
        .AddIngredient(ItemID.Ectoplasm, 8).AddTile(TileID.MythrilAnvil).Register();
}
