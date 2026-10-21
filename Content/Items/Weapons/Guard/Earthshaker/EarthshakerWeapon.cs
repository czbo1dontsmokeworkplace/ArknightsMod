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
        Item.value = Item.sellPrice(gold: 2 + (int)Kind * 5); Configure(true);
    }
    internal void Configure(bool tool)
    {
        Item.hammer = EarthshakerRules.HammerPower(Kind);
        // A nonzero shoot runs ApplyItemTime before vanilla mining, even when Shoot
        // returns false. A real tool swing must skip that path to pound tiles/walls.
        Item.shoot = tool ? ProjectileID.None : ModContent.ProjectileType<EarthshakerHeldHammer>();
        Item.useStyle = ItemUseStyleID.Swing;
        Item.noMelee = !tool; Item.noUseGraphic = false; Item.channel = !tool;
        Item.useTurn = Item.autoReuse = true;
        Item.useTime = tool ? 16 - (int)Kind * 4 : 15;
        Item.useAnimation = tool ? 24 - (int)Kind * 2 : 15;
        Item.UseSound = SoundID.Item1;
    }
    public override bool AltFunctionUse(Player player) => false;
    public override float UseSpeedMultiplier(Player player) =>
        player.GetModPlayer<EarthshakerPlayer>().ToolUse ? .7f : 1f;
    public override bool CanUseItem(Player player)
    {
        var state = player.GetModPlayer<EarthshakerPlayer>();
        if (ArknightsKeybinds.SkillActivatePressed(player)) { state.TryActivate(); return false; }
        if (state.Busy || EarthshakerHeldHammer.Find(player) != null) return false;
        if(!state.LeftHeld&&!state.RightHeld)return false;
        bool tool = state.ToolUse;
        Configure(tool);
        if(!tool)Item.hammer=0;
        return base.CanUseItem(player);
    }
    public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
    {
        var state = player.GetModPlayer<EarthshakerPlayer>();
        if (!state.LeftHeld || state.ToolUse || state.RightHeld || player.whoAmI != Main.myPlayer) return false;
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
