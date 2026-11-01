using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mortis
{
	[AutoloadEquip(EquipType.Head)]
	public class MortisHead : NeoArmorReforgeVanityHead
	{
		public override int Rarity => 5;

		public override NeoArmorReforgeSetProfile SetProfile => new() {
			Defense = 0,
			LifeBonus = 221,
			LocalizationPrefix = "Mods.ArknightsMod.ArmorSets.Mortis",
			SetBonusKey = "Mods.ArknightsMod.ArmorSets.Mortis.SetBonus",
			Materials = recipe => recipe
				.AddIngredient<Orundum>(50)
				.AddIngredient<CyclicenePrefab>(8)
				.AddIngredient<PolyesterPack>(13),
		};
	}
}
