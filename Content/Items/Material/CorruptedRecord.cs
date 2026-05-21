using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	public class CorruptedRecord : ModItem
	{
		public override void SetDefaults() {
			Item.rare = ItemRarityID.Blue;
			Item.height = 20;
			Item.width = 20;
			Item.maxStack = Item.CommonMaxStack;
			Item.material = true;

		}
	}
}