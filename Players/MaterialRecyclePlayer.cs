using ArknightsMod.Content.Items;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.NPCs.Friendly;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players
{
	// A 组材料（继承 ArknightsMaterial 的普通材料）在可露希尔处的回收：
	// 按 "该稀有度买入价的一半" 用合成玉结算。
	//
	// 为什么必须接管交易：自定义货币在原版只有购买侧能用——玩家背包里的物品
	// hoverItem.shopSpecialCurrency 恒为 -1，Main.MouseText_DrawItemTooltip 只会按
	// Item.value/5 显示并发放金币（见 Player.SellItem）。所以想用合成玉回收材料，
	// 只能在交易发生前于 CanSellItem 里自己发币、清空物品并返回 false，
	// 与坎诺特回收自然采集物的做法一致（见 RareCollectibleSellPlayer）。
	public class MaterialRecyclePlayer : ModPlayer
	{
		public override bool CanSellItem(NPC vendor, Item[] shopInventory, Item item) {
			// TradedWithOrundum 为 false 的材料不走合成玉回收：碳素改回了金币结算，
			// 自然采集物则在坎诺特处换源石锭（见 RareCollectibleSellPlayer）。
			// 注意 tModLoader 的 PlayerLoader.CanSellItem 是 result &= ... 形式，所有 ModPlayer 的
			// 这个钩子都会被执行，所以必须自己把它们排除掉，否则会和坎诺特那边重复结算。
			if (item.ModItem is not ArknightsMaterial material || !material.TradedWithOrundum)
				return true;

			// 只收给可露希尔；卖给其他 NPC 仍按原版金币结算（金币价值见 ArknightsMaterial.GetValue）。
			if (vendor.ModNPC is not Closure)
				return true;

			int unitPrice = ArknightsMaterial.GetOrundumSellPrice(material.Rarity);
			if (unitPrice <= 0 || item.stack <= 0)
				return true;

			// 原版一次卖出整叠（Player.SellItem 的 stack 参数默认取 item.stack），这里保持一致。
			long total = (long)unitPrice * item.stack;
			if (total > int.MaxValue)
				total = int.MaxValue;
			Player.QuickSpawnItem(Player.GetSource_Misc("ClosureMaterialRecycle"),
				ModContent.ItemType<Orundum>(), (int)total);
			item.TurnToAir();
			SoundEngine.PlaySound(SoundID.Coins);

			// 交易已完成；返回 false 阻止原版再发一份金币，也顺带阻止把物品塞进商店的回购栏。
			return false;
		}
	}
}
