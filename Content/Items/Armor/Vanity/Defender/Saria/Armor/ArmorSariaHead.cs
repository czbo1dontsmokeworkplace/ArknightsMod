using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Saria.Armor
{
	[AutoloadEquip(EquipType.Head)]
	public class ArmorSariaHead : ArknightsArmorHead
	{
		public override int LifeBonus => 315;
		public override void SetArmorDefaults() {
			Item.defense = 0;
		}
		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<ArmorSariaBody>() &&
				legs.type == ModContent.ItemType<ArmorSariaLegs>();
		}
		public override void UpdateArmorEquip(Player Player) {
			Player.GetModPlayer<ArknightsArmorPlayer>().extraDefenseBonus += 0.05f;
		}
		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
			player.GetModPlayer<SariaSetPlayer>().SariaSetActive = true;
		}
	}
	
}
