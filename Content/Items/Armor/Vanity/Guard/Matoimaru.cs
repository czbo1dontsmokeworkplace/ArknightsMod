using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard
{
	[AutoloadEquip(EquipType.Head)]
	public class MatoimaruHead : ArknightsVanityHead
	{
		public override int Rarity => ItemRarityID.LightRed;
	}

	[AutoloadEquip(EquipType.Body)]
	public class MatoimaruBody : ArknightsVanityBody
	{
		public override int Rarity => ItemRarityID.LightRed;
	}

	[AutoloadEquip(EquipType.Legs)]
	public class MatoimaruLegs : ArknightsVanityLegs
	{
		public override int Rarity => ItemRarityID.LightRed;
	}
}
