using ArknightsMod.Content.Projectiles.Guard.Earthshaker;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;

public abstract class EarthshakerWeapon : ExpansionWeaponBase
{
    public abstract EarthshakerKind Kind { get; }
    public override string Texture => EarthshakerRules.Art(Kind);
    public override void SetStaticDefaults() => ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
    public override void SetDefaults()
    {
        Item.width = Item.height = 54; Item.damage = EliteDamage[0]; Item.DamageType = DamageClass.Melee;
        Item.knockBack = 9 + (int)Kind; Item.shootSpeed = 1;
        Item.shoot = ModContent.ProjectileType<EarthshakerHeldHammer>();
        Item.rare = Kind switch { EarthshakerKind.Odda => ItemRarityID.Orange, EarthshakerKind.Zima => ItemRarityID.Lime, _ => ItemRarityID.Red };
        Item.value = Item.sellPrice(gold: 2 + (int)Kind * 5); Configure(false);
    }
    internal void Configure(bool throwing)
    {
        Item.hammer = 0;
        Item.shoot = throwing ? ModContent.ProjectileType<EarthshakerReturningHammer>() : ModContent.ProjectileType<EarthshakerHeldHammer>();
        Item.shootSpeed = throwing ? 14f : 1f;
        Item.knockBack = throwing ? 9f : 9 + (int)Kind;
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = true; Item.noUseGraphic = throwing; Item.channel = !throwing;
        Item.useTurn = !throwing; Item.autoReuse = true;
        Item.useTime = Item.useAnimation = 15;
        Item.UseSound = throwing ? SoundID.Item1 with { Pitch = -.5f } : SoundID.Item1;
    }
    public override bool AltFunctionUse(Player player) => false;
    public override bool CanUseItem(Player player)
    {
        var state = player.GetModPlayer<EarthshakerPlayer>();
        if (ArknightsKeybinds.SkillActivatePressed(player)) { state.TryActivate(); return false; }
        if (state.Busy || state.Throwing) return false;
        if(!state.LeftHeld&&!state.RightHeld)return false;
        Configure(state.ThrowUse);
        return base.CanUseItem(player);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        var state = player.GetModPlayer<EarthshakerPlayer>();
        if (player.whoAmI != Main.myPlayer) return false;
        if (state.RightHeld && state.ThrowUse)
        {
            Projectile.NewProjectile(source, position, velocity, ModContent.ProjectileType<EarthshakerReturningHammer>(),
                damage, knockback, player.whoAmI, 0f, 0f, (int)Kind);
            return false;
        }
        if (!state.LeftHeld || state.ThrowUse || state.RightHeld) return false;
        state.BeginCharge(damage, knockback);
        return false;
    }
    public override void OnHitNPC(Player player, NPC target, NPC.HitInfo hit, int damageDone) => player.GetModPlayer<EarthshakerPlayer>().CreditAttack();
}

public sealed class OddaHammer : EarthshakerWeapon
{
    public override EarthshakerKind Kind => EarthshakerKind.Odda;
    protected override int[] EliteDamage => [48, 58, 67];
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.HellstoneBar, 12)
        .AddRecipeGroup(OperatorWeaponRecipeGroups.GoldOrPlatinumBar, 8).AddIngredient(ItemID.StoneBlock, 35).AddTile(TileID.Anvils).Register();
}
public sealed class ZimaFuryHammer : EarthshakerWeapon
{
    public override EarthshakerKind Kind => EarthshakerKind.Zima;
    protected override int[] EliteDamage => [114, 138, 162];
    public override void AddRecipes() => CreateRecipe().AddIngredient<OddaHammer>()
        .AddRecipeGroup("ArknightsMod:CobaltOrPalladiumBar", 16)
        .AddIngredient(ItemID.SoulofLight, 8).AddTile(TileID.Anvils).Register();
}
public sealed class PepeHammer : EarthshakerWeapon
{
    public override EarthshakerKind Kind => EarthshakerKind.Pepe;
    protected override int[] EliteDamage => [190, 225, 260];
    public override void AddRecipes() => CreateRecipe().AddIngredient<ZimaFuryHammer>().AddIngredient(ItemID.FragmentSolar, 12)
        .AddIngredient(ItemID.FragmentStardust, 12).AddIngredient(ItemID.Amber, 8).AddTile(TileID.LunarCraftingStation).Register();
}
