using ArknightsMod.Content.Items.BattleRecords;
using ArknightsMod.Content.NPCs.Enemy.W;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.BossSummon
{
	/// <summary>W 的宝藏袋（专家/大师）。内容与普通模式本体掉落共用 AddContents。</summary>
	public class WTreasureBag : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 3;
			ItemID.Sets.BossBag[Type] = true;
		}

		public override void SetDefaults() {
			Item.width = 54;
			Item.height = 50;
			Item.maxStack = Item.CommonMaxStack;
			Item.consumable = true;
			Item.expert = true;
			Item.rare = ItemRarityID.Expert;
		}

		public override bool CanRightClick() => true;

		public override void ModifyItemLoot(ItemLoot itemLoot) {
			AddContents(itemLoot);
			itemLoot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<WBoss>()));
		}

		/// <summary>袋子内容：以后加 W 专属掉落只改这里，普通模式会跟着一起掉。</summary>
		public static void AddContents(ILoot loot) {
			loot.Add(ItemDropRule.Common(ModContent.ItemType<OriginiumIngot>(), 1, 15, 25));
			loot.Add(ItemDropRule.Common(ModContent.ItemType<TacticalBattleRecord>(), 1, 2, 4));
		}

		public static void AddContents(LeadingConditionRule rule) {
			rule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<OriginiumIngot>(), 1, 15, 25));
			rule.OnSuccess(ItemDropRule.Common(ModContent.ItemType<TacticalBattleRecord>(), 1, 2, 4));
		}
	}
}
