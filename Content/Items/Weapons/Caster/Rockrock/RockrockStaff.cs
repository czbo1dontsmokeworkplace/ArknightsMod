using ArknightsMod.Content.Items.Weapons.Caster;
using ArknightsMod.Content.Items.Weapons.Caster.Click;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Weapons.Caster.Rockrock;

public sealed class RockrockStaff : MechAccordStaffBase
{
    protected override int[] EliteDamage => [91, 91, 91];
    internal override bool IsRockrock => true;
    internal override float SearchRadius => 225f * 1.5f;
    internal override float Reach => 1200f;
    internal override int PulseInterval => 17;
    // Vanilla Spectre Staff gives Rockrock its own recognizable wand silhouette.
    public override string Texture => "Terraria/Images/Item_" + ItemID.SpectreStaff;

    internal override float AttackSpeedBonus(WeaponPlayer skills) => skills.SkillActive
        ? skills.Skill == 0 ? 1.3f + .035f * SkillRank(skills) : 1.3f + .04f * SkillRank(skills)
        : 1f;

    internal override float DroneCap(WeaponPlayer skills) => skills.SkillActive && skills.Skill == 1 &&
        skills.CurrentSkill != null && skills.SkillTimer >= skills.CurrentSkill.CurrentLevelData.ActiveTime * 30f ?
        1.5f + .055f * SkillRank(skills) : 1.1f;

    internal override float DroneDamageBonus(WeaponPlayer skills) => skills.SkillActive && skills.Skill == 1 &&
        skills.CurrentSkill != null && skills.SkillTimer >= skills.CurrentSkill.CurrentLevelData.ActiveTime * 30f ?
        1.2f + .04f * SkillRank(skills) : 1f;

    public override void SetDefaults()
    {
        base.SetDefaults();
        Item.rare = ItemRarityID.LightRed;
        Item.value = Item.sellPrice(gold: 6);
    }

    public override void AddRecipes() => CreateRecipe()
        .AddIngredient<ClickStaff>()
        .AddIngredient(ItemID.HallowedBar, 12)
        .AddIngredient(ItemID.SoulofMight, 8)
        .AddIngredient(ItemID.Wire, 30)
        .AddIngredient(ItemID.CrystalShard, 16)
        .AddTile(TileID.MythrilAnvil)
        .Register();
}
