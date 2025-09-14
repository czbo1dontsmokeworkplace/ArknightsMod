using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper
{
	[AutoloadEquip(EquipType.Head)]
	public class WisdelHead : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
			if (Main.netMode == NetmodeID.Server)
				return;
			ArmorIDs.Head.Sets.DrawHead[Item.headSlot] = false;
		}

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 28;
			Item.rare = ItemRarityID.LightPurple;
			Item.vanity = true;
		}
	}
}
