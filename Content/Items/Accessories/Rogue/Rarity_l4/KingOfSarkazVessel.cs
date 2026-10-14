using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Common.Items;
using System;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4
{
	public class KingOfSarkazVessel : ModItem
	{
		public override void SetStaticDefaults() {
			ItemID.Sets.ItemNoGravity[Item.type] = true;
		}

		public override void SetDefaults() {
			Item.width = 30;
			Item.height = 30;
			Item.value = Item.sellPrice(0, 16, 0, 0);
			Item.rare = ItemRarityID.Purple;
			Item.accessory = true;
			Item.GetGlobalItem<SarkazKingGlobalItem>().isSarkazKing = true;
		}

		private int CountSarkazKingItems(Player player) {
			int count = 0;
			int startIndex = 3;
			int endIndex = 8 + player.extraAccessorySlots;

			for (int i = startIndex; i < endIndex; i++) {
				if (i < player.armor.Length) {
					Item accessory = player.armor[i];
					if (!accessory.IsAir &&
						accessory.TryGetGlobalItem(out SarkazKingGlobalItem sarkazKing) &&
						sarkazKing.isSarkazKing) {
						count++;
					}
				}
			}
			return count;
		}

		public override void UpdateAccessory(Player player, bool hideVisual) {
			var modPlayer = player.GetModPlayer<KingOfSarkazVesselPlayer>();
			modPlayer.effectActive = true;

			int sarkazKingCount = CountSarkazKingItems(player);
			modPlayer.effectLevel = sarkazKingCount >= 3 ? 2 : 1;

			// 直接在这里应用生命值加成
			float healthBonus = sarkazKingCount >= 3 ? 0.4f : 0.2f;
			player.statLifeMax2 += (int)(player.statLifeMax2 * healthBonus);
		}
	}

	public class KingOfSarkazVesselPlayer : ModPlayer
	{
		public bool effectActive;
		public int effectLevel;

        public override void ResetEffects()
        {
            effectActive = false;
            effectLevel = 0;
        }

        public override void UpdateLifeRegen()
        {
            if (effectActive && effectLevel == 2 && Player.statLife > Player.statLifeMax2 * 0.85f)
                Player.lifeRegen += 8; // 4 HP per second.
        }
    }
}
