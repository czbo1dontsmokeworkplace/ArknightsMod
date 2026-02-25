using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3
{
	public class SarkazKingRegalRest : ModItem
	{
		public override void SetDefaults() {
			Item.width = 30;
			Item.height = 30;
			Item.accessory = true;
			Item.rare = ItemRarityID.Purple;
			Item.value = Item.sellPrice(0,6,0,0);
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			player.GetModPlayer<SarkazKingRegalRestPlayer>().effectActive = true;
		}
	}

	public class SarkazKingRegalRestPlayer : ModPlayer
	{
		public bool effectActive;

		public override void ResetEffects() {
			effectActive = false;
		}

		public override void PostUpdateEquips() {
			if (effectActive && Player.statLife >= Player.statLifeMax2) {

				Player.statDefense *= 20;
				Player.endurance += 0.2f;
			}
		}
	}
}