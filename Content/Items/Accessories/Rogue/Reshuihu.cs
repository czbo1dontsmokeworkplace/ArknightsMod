using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Accessories.Rogue
{
	public class Reshuihu : ModItem
	{
		private int reshuijishi = 0;
		private int[] buffIDs = new int[] {
		2,         
        3,         
        5,       
        14,       
        16,
		43,
		48,
		58,
		62,
		63,
		89,
		105,
		113,
		114,
		115,
		116,
		117,
		124,
		146,
		151,
		159,
		165,
		198,
		207,
		215,
		257,
		336,
		1,
		6,
		7
	};
		public override void SetDefaults() {
			Item.width = 30;
			Item.height = 30;
			Item.value = Item.sellPrice(12, 0, 0, 0); // 15金币价值
			Item.rare = ItemRarityID.Blue; // 青柠色(专家模式)稀有度
			Item.accessory = true;
		}
		public override void UpdateAccessory(Player player, bool hideVisual) {
			// 提升50%最大生命值
			reshuijishi++;
			player.statLifeMax2 += 20;
			int randomBuffIndex = Main.rand.Next(buffIDs.Length);
			int selectedBuff = buffIDs[randomBuffIndex];

			// 给被命中的NPC施加选中的Buff，持续时间示例：5秒（300帧）
			if (reshuijishi >= 600) {
				player.AddBuff(selectedBuff, 300);
				reshuijishi = 0;
			}
		}
	}
}