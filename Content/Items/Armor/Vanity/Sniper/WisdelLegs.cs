using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper
{
	[AutoloadEquip(EquipType.Legs)]
	public class WisdelLegs : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;

			if (Main.netMode == NetmodeID.Server)
				return;
			ArmorIDs.Legs.Sets.HidesBottomSkin[Item.legSlot] = true;
		}

		public override void SetDefaults() {
			Item.width = 18;
			Item.height = 8;
			Item.rare = ItemRarityID.LightPurple;
			Item.vanity = true;
		}
	}
}
