using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Specialist
{
	[AutoloadEquip(EquipType.Head)]
	public class MortisHead : ArknightsVanityHead
	{
		public override int Rarity => ItemRarityID.Pink;
	}

	[AutoloadEquip(EquipType.Body)]
	public class MortisBody : ArknightsVanityBody
	{
		public override int Rarity => ItemRarityID.Pink;
	}

	[AutoloadEquip(EquipType.Legs)]
	public class MortisLegs : ArknightsVanityLegs
	{
		public override int Rarity => ItemRarityID.Pink;
	}
}
