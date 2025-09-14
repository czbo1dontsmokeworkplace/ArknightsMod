using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables
{
	public class OblivionisDefault : ModItem
	{
		public override void SetStaticDefaults() {

			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.maxStack = 9999;
			Item.consumable = true;
			Item.width = 28;
			Item.height = 50;
			Item.rare = ItemRarityID.White;
		}

		public override bool CanRightClick() {
			return true;
		}

		public override void ModifyItemLoot(ItemLoot itemLoot) {
			IItemDropRule rule = ItemDropRule.Common(ModContent.ItemType<Armor.Vanity.Guard.TogawaSakiko.TogawaSakikoHead>(), 1);
			rule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Armor.Vanity.Guard.TogawaSakiko.TogawaSakikoBody>(), 1));
			rule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<Armor.Vanity.Guard.TogawaSakiko.TogawaSakikoLegs>(), 1));

			itemLoot.Add(rule);
		}
	}
}
