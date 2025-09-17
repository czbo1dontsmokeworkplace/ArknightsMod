using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster
{
	[AutoloadEquip(EquipType.Head)]
	public class AmiyaHead : ArknightsVanityHead
	{
		public override int Rarity => ItemRarityID.Pink;
	}

	[AutoloadEquip(EquipType.Body)]
	public class AmiyaBody : ArknightsVanityBody
	{
		public override int Rarity => ItemRarityID.Pink;
	}

	[AutoloadEquip(EquipType.Legs)]
	public class AmiyaLegs : ArknightsVanityLegs
	{
		public override int Rarity => ItemRarityID.Pink;
	}
}
