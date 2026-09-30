using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Material
{
	public abstract class ArknightsMaterial : ModItem
	{
		/// <summary>
		/// 材料稀有度，0~4分别对应 白，绿，蓝，紫，金材料
		/// </summary>
		public virtual int Rarity => 0;

		/// <summary>
		/// 是否在可露希尔处以合成玉买卖：商店定价、出售回收、工具条提示三处都看这个开关。
		/// 普通材料为 true；碳素改回了金币结算、自然采集物走坎诺特的源石锭，二者都覆写为 false。
		/// </summary>
		public virtual bool TradedWithOrundum => true;

		public sealed override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 100;
			SafeSetStaticDefaults();
		}

		public sealed override void SetDefaults() {
			Item.width = 20;
			Item.height = 20;

			Item.maxStack = Item.CommonMaxStack;
			Item.rare = GetRarity(Rarity);
			Item.value = GetValue(Rarity);

			SafeSetDefaults();
		}

		public virtual void SafeSetDefaults() { }
		public virtual void SafeSetStaticDefaults() { }

		public override void ModifyTooltips(List<TooltipLine> tooltips) {
			// 在工具条最后一行标出可露希尔处的回收价。
			// 走金币结算的碳素、走坎诺特源石锭的采集物都不标（见 TradedWithOrundum）。
			// 注意：商店里原版的"价格"行是在本钩子之后才追加的，所以那一种场合它会排在提示下面。
			if (!TradedWithOrundum)
				return;
			tooltips.Add(new TooltipLine(Mod, "OrundumRecycleHint", Language.GetTextValue(
				"Mods.ArknightsMod.CommonTooltips.OrundumRecycleHint",
				GetOrundumSellPrice(Rarity), $"[i:{ModContent.ItemType<Orundum>()}]")));
		}

		public static int GetRarity(int rarity) {
			rarity = Math.Clamp(rarity, 0, 5);
			int result = rarity switch {
				0 => ItemRarityID.White,
				1 => ItemRarityID.Green,
				2 => ItemRarityID.Cyan,
				3 => ItemRarityID.LightPurple,
				4 => ItemRarityID.Quest,
				_ => ItemRarityID.White
			};
			return result;
		}

		public static int GetValue(int rarity) {
			rarity = Math.Clamp(rarity, 0, 5);
			int result = rarity switch {
				0 => Item.sellPrice(0, 0, 0, 50),//0
				1 => Item.sellPrice(0, 0, 2, 00),//1=4*0
				2 => Item.sellPrice(0, 0, 8, 00),//2=4*1
				3 => Item.sellPrice(0, 0, 32, 0),//3=4*2
				4 => Item.sellPrice(0, 1, 28, 0),//4=4*3
				_ => Item.sellPrice(0, 0, 0, 50)
			};
			return result;
		}

		/// <summary>
		/// 在可露希尔处买卖材料用的合成玉定价，下标即稀有度（0~4 → 白/绿/蓝/紫/金）：
		/// 买入价 4 / 10 / 50 / 200 / 600，回收价恒为买入价的一半。
		/// <br/>注意回收价不是原版的金币结算：原版只按 <c>Item.value / 5</c> 发金币
		/// （见 <c>Player.SellItem</c>），合成玉回收由 Players/MaterialRecyclePlayer 接管。
		/// </summary>
		private static readonly int[] OrundumBuyPriceByRarity = [4, 10, 50, 200, 600];

		/// <summary>第 rarity 档材料的买入价（单位：合成玉）。</summary>
		public static int GetOrundumBuyPrice(int rarity) {
			return OrundumBuyPriceByRarity[Math.Clamp(rarity, 0, OrundumBuyPriceByRarity.Length - 1)];
		}

		/// <summary>第 rarity 档材料的回收价（单位：合成玉），恒为买入价的一半。</summary>
		public static int GetOrundumSellPrice(int rarity) {
			return GetOrundumBuyPrice(rarity) / 2;
		}
	}
}
