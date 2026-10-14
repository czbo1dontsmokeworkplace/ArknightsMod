using ArknightsMod.Content.Items.Weapons.Caster;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Caster.Click;

public sealed class ClickStaff : MechAccordStaffBase
{
    protected override int[] EliteDamage => [52, 52, 52];
    internal override bool IsRockrock => false;
    // Goldenglow's ordinary cursor acquisition disk is 225 px.
    internal override float SearchRadius => 225f * .7f * 1.5f;
    internal override float Reach => 1050f;
    internal override int PulseInterval => 24;
    // Vanilla Inferno Fork keeps Click visually distinct from Rockrock and Goldenglow.
    public override string Texture => "Terraria/Images/Item_" + ItemID.InfernoFork;

    internal override float AttackSpeedBonus(WeaponPlayer skills) =>
        skills.SkillActive && skills.Skill == 0 ? 1.25f + .025f * SkillRank(skills) : 1f;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.Orange;
        Item.value = Item.sellPrice(gold: 2);
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient(ItemID.MagicMissile)
        .AddIngredient(ItemID.Bone, 28)
        .AddIngredient(ItemID.Lens, 5)
        .AddIngredient(ItemID.Wire, 18)
        .AddIngredient(ItemID.Sapphire, 6)
        .AddTile(TileID.Anvils)
        .Register();
}
