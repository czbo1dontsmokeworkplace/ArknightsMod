using System.Linq;
using ArknightsMod.Content;
using ArknightsMod.Content.Projectiles.Guard.BladeRoutes;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.BladeRoutes;

// The two close-range hitboxes are deliberately independent: one blade hit and
// one aura hit per target per swing. Child slashes cannot trigger this attack again.
public abstract class OperatorBladeWeapon : ExpansionWeaponBase
{
    public abstract int Kind { get; }
    public bool IsLord => Kind >= 3;
    public override string Texture => BladeRouteRules.WeaponTexture(Kind);
    public override void SetDefaults()
    {
        Item.width = Item.height = 44;
        Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Melee;
        Item.useTime = Item.useAnimation = BladeRouteRules.UseTicks(Kind);
        Item.useStyle = ItemUseStyleID.Swing; Item.autoReuse = true;
        Item.noUseGraphic = Item.noMelee = true;
        Item.shootsEveryUse = true; Item.shootSpeed = 18;
        Item.shoot = ModContent.ProjectileType<OperatorBladeSwing>();
        Item.knockBack = 4 + Kind % 3; Item.UseSound = SoundID.Item1;
        Item.rare = Kind switch { 0 or 3 => ItemRarityID.Blue, 1 or 4 => ItemRarityID.Orange, _ => ItemRarityID.Pink };
        Item.value = Item.sellPrice(silver: 50 + Kind % 3 * 100);
    }
    public override bool AltFunctionUse(Player player) => false;
    public override bool CanUseItem(Player player) => !ArknightsKeybinds.SkillActivatePressed(player)
        && !Main.projectile.Any(q => q.active && q.owner == player.whoAmI && q.ModProjectile is OperatorBladeSwing);
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        BladeRouteCombat.Swing(player,source,Kind,damage,knockback);
        return false;
    }
    public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        BladeRouteArt.Icon(spriteBatch, Kind, position, drawColor, 42 * scale); return false;
    }
    public override bool PreDrawInWorld(SpriteBatch spriteBatch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        BladeRouteArt.Icon(spriteBatch, Kind, Item.Center - Main.screenPosition, lightColor, 48 * scale, rotation); return false;
    }
}

public sealed class CutterBlades : OperatorBladeWeapon
{
    public override int Kind => 0;
    protected override int[] EliteDamage => [16, 20, 24];
    public override void AddRecipes() => CreateRecipe().AddRecipeGroup(OperatorWeaponRecipeGroups.IronOrLeadBar, 10)
        .AddIngredient(ItemID.Topaz, 2).AddIngredient(ItemID.Wood, 8).AddTile(TileID.Anvils).Register();
}
public sealed class BibeakSword : OperatorBladeWeapon
{
    public override int Kind => 1;
    protected override int[] EliteDamage => [28, 35, 42];
    public override void AddRecipes() => CreateRecipe().AddIngredient<CutterBlades>().AddIngredient(ItemID.HellstoneBar, 12)
        .AddIngredient(ItemID.Feather, 5).AddTile(TileID.Anvils).Register();
}
public sealed class IreneSaber : OperatorBladeWeapon
{
    public override int Kind => 2;
    protected override int[] EliteDamage => [49, 60, 72];
    public override void AddRecipes() => CreateRecipe().AddIngredient<BibeakSword>().AddIngredient(ItemID.HallowedBar, 12)
        .AddIngredient(ItemID.SoulofSight, 5).AddTile(TileID.MythrilAnvil).Register();
}
public sealed class MidnightSword : OperatorBladeWeapon
{
    public override int Kind => 3;
    protected override int[] EliteDamage => [12, 16];
    public override void AddRecipes() => CreateRecipe().AddRecipeGroup(OperatorWeaponRecipeGroups.IronOrLeadBar, 8)
        .AddIngredient(ItemID.Amethyst, 2).AddIngredient(ItemID.Wood, 6).AddTile(TileID.Anvils).Register();
}
public sealed class AreneSword : OperatorBladeWeapon
{
    public override int Kind => 4;
    protected override int[] EliteDamage => [29, 36, 43];
    public override void AddRecipes() => CreateRecipe().AddIngredient<global::ArknightsMod.Content.Items.Weapons.Guard.Frostleaf.FrostleafAxe>()
        .AddIngredient(ItemID.HellstoneBar, 12).AddIngredient(ItemID.Emerald, 3).AddTile(TileID.Anvils).Register();
}
public sealed class LapplandSwords : OperatorBladeWeapon
{
    public override int Kind => 5;
    protected override int[] EliteDamage => [52, 64, 76];
    public override void AddRecipes() => CreateRecipe().AddIngredient<AreneSword>().AddIngredient(ItemID.HallowedBar, 12)
        .AddIngredient(ItemID.SoulofNight, 8).AddTile(TileID.MythrilAnvil).Register();
}

// Additional bridge recipes leave the existing weapons' combat, skills and old
// recipes intact. The new sequence can be followed without rewriting legacy items.
public sealed class BladeRouteRecipes : ModSystem
{
    public override void AddRecipes()
    {
        Recipe.Create(ModContent.ItemType<global::ArknightsMod.Content.Items.Weapons.Guard.Frostleaf.FrostleafAxe>())
            .AddIngredient<MidnightSword>().AddIngredient(ItemID.IceBlock, 30).AddIngredient(ItemID.Bone, 15).AddTile(TileID.Anvils).Register();
        Recipe.Create(ModContent.ItemType<global::ArknightsMod.Content.Items.Weapons.Guard.SilverAsh.SilverAshWeapon>())
            .AddIngredient<LapplandSwords>().AddIngredient(ItemID.TrueExcalibur).AddIngredient(ItemID.Feather, 3).AddTile(TileID.MythrilAnvil).Register();
        Recipe.Create(ModContent.ItemType<global::ArknightsMod.Content.Items.Weapons.Guard.Thorns.ThornsWeapon>())
            .AddIngredient<LapplandSwords>().AddIngredient(ItemID.TerraBlade).AddIngredient(ItemID.VialofVenom, 10).AddTile(TileID.MythrilAnvil).Register();
    }
}
