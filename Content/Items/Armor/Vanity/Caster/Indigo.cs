using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster
{
	[AutoloadEquip(EquipType.Head)]
	public class IndigoHead : ArknightsVanityHead
	{
		public override int Rarity => ItemRarityID.LightRed;
	}

	[AutoloadEquip(EquipType.Body)]
	public class IndigoBody : ArknightsVanityBody
	{
		public override int Rarity => ItemRarityID.LightRed;
	}

	[AutoloadEquip(EquipType.Legs)]
	public class IndigoLegs : ArknightsVanityLegs
	{
		public override int Rarity => ItemRarityID.LightRed;
	}
}
