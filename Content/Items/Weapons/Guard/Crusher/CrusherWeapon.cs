using ArknightsMod.Content.Projectiles.Guard.Crusher;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Crusher;

public abstract class CrusherWeapon : ExpansionWeaponBase
{
    public abstract CrusherKind Kind { get; }
    public override string Texture => CrusherRules.Art(Kind);
    public override void SetDefaults()
    {
        Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Melee;
        Item.width = 58; Item.height = 58; Item.knockBack = 8 + (int)Kind;
        Item.useTime = Item.useAnimation = CrusherRules.Interval(Kind);
        Item.useStyle = ItemUseStyleID.Shoot; Item.noMelee = Item.noUseGraphic = true;
        Item.autoReuse = true; Item.channel = true;
        Item.shoot = ModContent.ProjectileType<CrusherHeldBlade>(); Item.shootSpeed = 1;
        Item.rare = Kind switch { CrusherKind.Quartz => ItemRarityID.Green, CrusherKind.WindChimes => ItemRarityID.LightRed,
            CrusherKind.Hoederer => ItemRarityID.Lime, _ => ItemRarityID.Red };
        Item.value = Item.sellPrice(silver: 60 + (int)Kind * 230);
    }
    public override bool AltFunctionUse(Player player) => false;
    public override void HoldItem(Player player)
    {
        base.HoldItem(player);
        bool hasAura = false;
        foreach (Projectile p in Main.ActiveProjectiles) if (p.owner == player.whoAmI && p.ModProjectile is CrusherAura) { hasAura = true; break; }
        if (player.whoAmI == Main.myPlayer && !player.dead && !hasAura)
            Projectile.NewProjectile(player.GetSource_ItemUse(Item), player.Bottom, Vector2.Zero,
                ModContent.ProjectileType<CrusherAura>(), 0, 0, player.whoAmI, (int)Kind, player.selectedItem);
    }
    public override float UseSpeedMultiplier(Player player) => player.GetModPlayer<CrusherPlayer>().SwingSpeed;
    public override bool CanUseItem(Player player)
    {
        if (ArknightsKeybinds.SkillActivatePressed(player)) { player.GetModPlayer<CrusherPlayer>().TryActivate(); return false; }
        if (player.altFunctionUse == 2 || player.GetModPlayer<CrusherPlayer>().Charging) return false;
        if (Kind == CrusherKind.Ulpianus && UlpianusAnchorThrow.Exists(player)) return false;
        CrusherHeldBlade blade = CrusherHeldBlade.Find(player);
        Item.useTime = Item.useAnimation = CrusherRules.Interval(Kind);
        return blade is not { Busy: true } && base.CanUseItem(player);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        if (player.whoAmI != Main.myPlayer) return false;
        CrusherHeldBlade blade = CrusherHeldBlade.Find(player);
        if (blade == null) {
            int id = Projectile.NewProjectile(source, player.MountedCenter, velocity.SafeNormalize(new Vector2(player.direction, 0)),
                type, damage, knockback, player.whoAmI, (int)Kind, player.selectedItem);
            if (id >= 0 && id < Main.maxProjectiles) blade = Main.projectile[id].ModProjectile as CrusherHeldBlade;
        }
        blade?.BeginCharge(player);
        return false;
    }
}

public sealed class QuartzGreatsword : CrusherWeapon
{
    public override CrusherKind Kind => CrusherKind.Quartz;
    protected override int[] EliteDamage => [36, 44, 52];
    public override void AddRecipes() => CreateRecipe().AddRecipeGroup("ArknightsMod:IronOrLeadBar", 12)
        .AddIngredient(ItemID.Topaz, 3).AddIngredient(ItemID.Wood, 10).AddTile(TileID.Anvils).Register();
}
public sealed class WindChimesHeavyBlade : CrusherWeapon
{
    public override CrusherKind Kind => CrusherKind.WindChimes;
    protected override int[] EliteDamage => [91, 108, 125];
    public override void AddRecipes() => CreateRecipe().AddIngredient<QuartzGreatsword>()
        .AddRecipeGroup("ArknightsMod:CobaltOrPalladiumBar", 16).AddIngredient(ItemID.SoulofLight, 8).AddTile(TileID.Anvils).Register();
}
public sealed class HoedererGreatsword : CrusherWeapon
{
    public override CrusherKind Kind => CrusherKind.Hoederer;
    protected override int[] EliteDamage => [178, 208, 240];
    public override void AddRecipes() => CreateRecipe().AddIngredient<WindChimesHeavyBlade>()
        .AddIngredient(ItemID.ChlorophyteBar, 18).AddIngredient(ItemID.Ectoplasm, 8)
        .AddIngredient(ItemID.HellstoneBar, 12).AddTile(TileID.MythrilAnvil).Register();
}
public sealed class UlpianusAnchor : CrusherWeapon
{
    public override CrusherKind Kind => CrusherKind.Ulpianus;
    protected override int[] EliteDamage => [268, 312, 356];
    public override void AddRecipes() => CreateRecipe().AddIngredient<HoedererGreatsword>()
        .AddIngredient(ItemID.FragmentSolar, 14).AddIngredient(ItemID.FragmentVortex, 8)
        .AddIngredient(ItemID.SharkFin, 5).AddTile(TileID.LunarCraftingStation).Register();
}
