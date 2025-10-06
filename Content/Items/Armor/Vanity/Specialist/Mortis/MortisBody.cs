using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity;

namespace ArknightsMod.Content.Items.Armor.Vanity.Specialist.Mortis
{
	[AutoloadEquip(EquipType.Body)]
	public class MortisBody : ArknightsVanityBody
	{
		public override int Rarity => ItemRarityID.LightPurple;
		public override void Load() {
		}
		public override void UpdateEquip(Player player) {
		}
	}
}
