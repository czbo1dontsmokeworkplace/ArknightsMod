using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3
{
    public class CommandersPortrait : ModItem
    {
        public override void SetDefaults()
        {
            Item.width = 30;
            Item.height = 30;
			Item.value = Item.sellPrice(0, 6, 0, 0);
			Item.rare = ItemRarityID.Purple; // 青柠色(专家模式)稀有度
            Item.accessory = true;
        }
        public override void UpdateAccessory(Player player, bool hideVisual)
        {
            float bonus = IsBoss() ? 0.15f : 0.05f;
            player.GetDamage(DamageClass.Generic) += bonus;
            player.statLifeMax2 += (int)(player.statLifeMax2 * bonus);
		}
		private bool IsBoss() {
			foreach (NPC npc in Main.npc) {
				if (npc.active && (npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type])) {
					return true;
				}
			}
			return false;
		}
	}
}