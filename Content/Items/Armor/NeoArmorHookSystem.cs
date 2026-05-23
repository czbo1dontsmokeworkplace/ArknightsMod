using System.Collections.Generic;
using System.Reflection;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	public class NeoArmorHookSystem : ModSystem
	{
		private static FieldInfo _recipeChestsField;
		private static Recipe _currentCraftingRecipe = null;
		public override void Load() {
			On_Recipe.UpdateRecipeList += UpdateRecipeList_FilterNeoArmor;
			_recipeChestsField = typeof(Recipe).GetField("_recipeChests", BindingFlags.NonPublic | BindingFlags.Static);

			On_CraftingRequests.CraftItem += CraftItem_RecordRecipe;
			On_CraftingRequests.ConsumeItemsFrom_ItemArray_int_RequiredItemEntry_refInt32_List1_int += ConsumeItemsFrom_CustomOrder;
		}

		public override void Unload() {
			On_Recipe.UpdateRecipeList -= UpdateRecipeList_FilterNeoArmor;
			_recipeChestsField = null;

			On_CraftingRequests.CraftItem -= CraftItem_RecordRecipe;
			On_CraftingRequests.ConsumeItemsFrom_ItemArray_int_RequiredItemEntry_refInt32_List1_int -= ConsumeItemsFrom_CustomOrder;
			_currentCraftingRecipe = null;
		}

		#region 筛选可合成物品
		private void UpdateRecipeList_FilterNeoArmor(On_Recipe.orig_UpdateRecipeList orig) {
			orig.Invoke();

			if (Main.InGuideCraftMenu) {
				return;
			}

			for (int i = Main.numAvailableRecipes - 1; i >= 0; i--) {
				Recipe r = Main.recipe[Main.availableRecipe[i]];

				if (!r.Conditions.Contains(NeoArmorUtils.NeedVanity))
					continue;

				HashSet<int> requiredNeoArmorTypes = new HashSet<int>();
				foreach (Item ing in r.requiredItem) {
					if (ing.IsAir)
						break;
					if (ing.GetGlobalItem<NeoArmorGItem>().isNeoArmor)
						requiredNeoArmorTypes.Add(ing.type);
				}

				if (requiredNeoArmorTypes.Count == 0)
					continue;

				// 使用与合成时完全相同的物品来源（背包 + 所有可用箱子）
				bool hasUnupgraded = HasUnupgradedNeoArmor(requiredNeoArmorTypes);

				if (!hasUnupgraded) {
					// 移除当前配方
					for (int j = i; j < Main.numAvailableRecipes - 1; j++)
						Main.availableRecipe[j] = Main.availableRecipe[j + 1];
					Main.numAvailableRecipes--;

					if (Main.focusRecipe >= Main.numAvailableRecipes)
						Main.focusRecipe = Main.numAvailableRecipes - 1;
					if (Main.focusRecipe < 0)
						Main.focusRecipe = 0;
				}
			}
		}

		private bool HasUnupgradedNeoArmor(HashSet<int> requiredTypes) {
			// 检查玩家背包
			for (int i = 0; i < 58; i++) {
				Item item = Main.LocalPlayer.inventory[i];
				if (item.IsAir)
					continue;
				if (requiredTypes.Contains(item.type)) {
					NeoArmorGItem g = item.GetGlobalItem<NeoArmorGItem>();
					if (g.isNeoArmor && !g.hasUpgraded)
						return true;
				}
			}

			// 获取合成时可用的箱子列表（和原版合成逻辑一致）
			List<Chest> craftingChests = GetCraftingChests();
			if (craftingChests != null) {
				foreach (Chest chest in craftingChests) {
					for (int i = 0; i < chest.maxItems; i++) {
						Item item = chest.item[i];
						if (item.IsAir)
							continue;
						if (requiredTypes.Contains(item.type)) {
							NeoArmorGItem g = item.GetGlobalItem<NeoArmorGItem>();
							if (g.isNeoArmor && !g.hasUpgraded)
								return true;
						}
					}
				}
			}

			return false;
		}

		private List<Chest> GetCraftingChests() {
			if (_recipeChestsField == null)
				return null;
			return _recipeChestsField.GetValue(null) as List<Chest>;
		}
		#endregion

		#region 实际合成时优先选择未升级的
		private void CraftItem_RecordRecipe(On_CraftingRequests.orig_CraftItem orig, Recipe recipe, int qty, bool quickCraft) {
			try {
				_currentCraftingRecipe = recipe;
				orig(recipe, qty, quickCraft);
			}
			finally {
				_currentCraftingRecipe = null;
			}
		}

		private void ConsumeItemsFrom_CustomOrder(On_CraftingRequests.orig_ConsumeItemsFrom_ItemArray_int_RequiredItemEntry_refInt32_List1_int orig,
			Item[] inventory, int maxItems, Recipe.RequiredItemEntry req,
			ref int toConsume, List<Item> consumedItems, int chestIndex)
		{
			// 非特殊配方，调用原版
			if (_currentCraftingRecipe == null || !_currentCraftingRecipe.Conditions.Contains(NeoArmorUtils.NeedVanity)) {
				orig(inventory, maxItems, req, ref toConsume, consumedItems, chestIndex);
				return;
			}

			// 特殊配方：自定义扣除顺序（优先未升级的 NeoArmor，再已升级的，最后其他）
			if (toConsume <= 0)
				return;

			// 收集所有匹配的物品槽位，并判断是否为已升级的 NeoArmor
			var matches = new List<(int slot, Item item, bool isUpgraded)>();
			for (int i = 0; i < maxItems; i++) {
				Item item = inventory[i];
				if (item.IsAir)
					continue;
				if (!req.Matches(item.type))
					continue;

				bool isUpgraded = false;
				var neo = item.GetGlobalItem<NeoArmorGItem>();
				if (neo != null && neo.isNeoArmor)
					isUpgraded = neo.hasUpgraded;

				matches.Add((i, item, isUpgraded));
			}

			// 排序，未升级在前 (false < true)
			matches.Sort((a, b) => a.isUpgraded.CompareTo(b.isUpgraded));

			// 执行扣除
			foreach (var match in matches) {
				if (toConsume <= 0)
					break;
				Item item = match.item;
				int slot = match.slot;

				if (item.stack > toConsume) {
					if (consumedItems != null) {
						Item copy = item.Clone();
						copy.stack = toConsume;
						consumedItems.Add(copy);
					}
					item.stack -= toConsume;
					toConsume = 0;
				}
				else {
					toConsume -= item.stack;
					if (consumedItems != null)
						consumedItems.Add(item);
					inventory[slot] = new Item();
				}

				// 同步
				if (chestIndex >= 0)
					NetMessage.SendData(32, -1, -1, null, chestIndex, slot, 0f, 0f, 0, 0, 0);
			}
		}
		#endregion
	}
}