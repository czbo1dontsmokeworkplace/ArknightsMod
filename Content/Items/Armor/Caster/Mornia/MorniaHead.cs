using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Reforge;
using ArknightsMod.Content.Items.Material;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	[AutoloadEquip(EquipType.Head)]
	public class MorniaHead : ReforgeVanityHead
	{
		public override int Rarity => 4;

		public override ReforgeSetProfile SetProfile => new() {
			Defense = 8,
			LifeBonus = 100,
			LocalizationPrefix = "Mods.ArknightsMod.ArmorSets.Mornia",
			Materials = recipe => recipe
				.AddIngredient<Orundum>(40)
				.AddIngredient<Aketon>(3)
				.AddIngredient<RefinedSolvent>(2),
			OnHelmetActive = MorniaSetPlayer.OnHelmetActive,
			SetBonusKey = "Mods.ArknightsMod.ArmorSets.Mornia.SetBonus",
		};
	}
}
