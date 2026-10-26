using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Medic.ReedFlameShadow
{
	[AutoloadEquip(EquipType.Head)]
	public class ReedFlameShadowHead : NeoArmorReforgeVanityHead
	{
		public override int Rarity => 5;

		public override NeoArmorReforgeSetProfile SetProfile => new() {
			Defense = 0,
			LifeBonus = 166,
			LocalizationPrefix = "Mods.ArknightsMod.ArmorSets.ReedFlameShadow",
			SetBonusKey = "Mods.ArknightsMod.ArmorSets.ReedFlameShadow.SetBonus",
			Materials = recipe => recipe
				.AddIngredient<Orundum>(50)
				.AddIngredient<OrirockConcentration>(10)
				.AddIngredient<LoxicKohl>(10),
		};
	}
}
