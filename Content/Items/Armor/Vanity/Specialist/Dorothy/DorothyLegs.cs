using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity;

namespace ArknightsMod.Content.Items.Armor.Vanity.Specialist.Dorothy
{
	[AutoloadEquip(EquipType.Legs)]
	public class DorothyLegs : ArknightsVanityLegs
	{
		public override int Rarity => 6;
		public override void Load() {
		}
		public override void UpdateEquip(Player player) {
		}
	}
}