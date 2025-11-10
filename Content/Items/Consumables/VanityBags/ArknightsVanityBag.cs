using System.Collections.Generic;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.VanityBags
{
	public abstract class ArknightsVanityBag : ModItem
	{
		protected abstract List<int> GetItems();

		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
		}

		public override void SetDefaults() {
			Item.maxStack = 9999;
			Item.consumable = true;
			Item.width = 30;
			Item.height = 56;
			Item.rare = ItemRarityID.White;
		}

		public override bool CanRightClick() {
			return true;
		}

		public override void ModifyItemLoot(ItemLoot itemLoot)
		{
			var items = GetItems();
			if (items == null || items.Count == 0)
				return;

			IItemDropRule rule = ItemDropRule.Common(items[0], 1);
			IItemDropRule currentRule = rule;

			for (int i = 1; i < items.Count; i++) {
				IItemDropRule nextRule = ItemDropRule.Common(items[i], 1);
				currentRule.OnSuccess(nextRule);
				currentRule = nextRule;
			}

			itemLoot.Add(rule);
		}
	}
}
