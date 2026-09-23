using ArknightsMod.Content.Items;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.DisplayForUI;
using ArknightsMod.Content.Items.Gacha;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using Terraria.Utilities;

namespace ArknightsMod.Content.NPCs.Friendly
{
	[AutoloadHead]
	public class Closure : ModNPC
	{
		/// <summary>
		/// 五个等级材料商店的商店名，下标 = 材料稀有度（0~4 → 白/绿/蓝/紫/金），
		/// 与 <see cref="ArknightsMaterial.Rarity"/> 一一对应。
		/// 显示名（白色材料商店…）在「切换」菜单的 <c>Buttons.MaterialShop*</c> 里。
		/// </summary>
		public static readonly string[] MaterialShopNames = ["ShopWhite", "ShopGreen", "ShopBlue", "ShopPurple", "ShopGold"];

		/// <summary>时装商店：每日轮换货架。</summary>
		public const string VanityShopName = "Shop2";

		/// <summary>本 NPC 注册的全部商店（五个等级材料商店 + 时装商店）。</summary>
		public static string[] ShopName => [.. MaterialShopNames, VanityShopName];

		public static int ButtonCount;

		/// <summary>
		/// 「切换」菜单里用的自定义聊天标签名（解析见 <see cref="ClosureSwitchOptionTagHandler"/>）。
		/// 只能用小写字母：原版聊天标签的正则是 (?&lt;tag&gt;[a-zA-Z]{1,10})，数字和下划线都不合法。
		/// </summary>
		public const string SwitchOptionTag = "closure";

		/// <summary>
		/// 可切换页面的本地化键，下标与 <see cref="ButtonCount"/> 一一对应：
		/// 0 帮助 / 1~5 五个等级材料商店 / 6 时装商店 / 7 剿灭作战。
		/// 聊天按钮的文字和「切换」菜单都从这里取名字，加/改页面只要动这一处
		/// （注意菜单总行数 = 本表长度 + 1 行提示，别超过原版对话的 10 行上限）。
		/// </summary>
		private static readonly string[] SwitchOptionLangKeys = [
			"Mods.ArknightsMod.NPCs.Closure.Buttons.Help",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopWhite",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopGreen",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopBlue",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopPurple",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopGold",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.Shop2",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.Annihilation",
		];

		/// <summary>材料商店在「切换」菜单里的起始下标（1 = 白档，与 <see cref="MaterialShopNames"/> 顺序一致）。</summary>
		private const int FirstMaterialShopButton = 1;

		/// <summary>时装商店在「切换」菜单里的下标。</summary>
		private const int VanityShopButton = 6;

		/// <summary>剿灭作战在「切换」菜单里的下标。</summary>
		private const int AnnihilationButton = 7;

		public static int SwitchOptionCount => SwitchOptionLangKeys.Length;

		/// <summary>第 index 个可切换页面的显示名（同时用作对应商店的标题）。</summary>
		public static string GetSwitchOptionLabel(int index) {
			if (index < 0 || index >= SwitchOptionLangKeys.Length)
				return string.Empty;
			return Language.GetTextValue(SwitchOptionLangKeys[index]);
		}

		/// <summary>
		/// 给玩家看的显示名：未解锁的等级材料商店会带上「（未解锁）」后缀。
		/// 聊天第一个按钮和「切换」菜单都用它，这样玩家点不动时也知道为什么。
		/// </summary>
		public static string GetSwitchOptionDisplayName(int index) {
			string label = GetSwitchOptionLabel(index);
			if (label.Length == 0)
				return label;
			if (IsMaterialShopButton(index) && !IsMaterialShopUnlocked(index - FirstMaterialShopButton))
				return Language.GetTextValue("Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopLocked", label);
			return label;
		}

		/// <summary>第 index 个选项是不是等级材料商店（下标 1~5）。</summary>
		private static bool IsMaterialShopButton(int index) {
			return index >= FirstMaterialShopButton && index < VanityShopButton;
		}

		// ── 五档材料商店的解锁门槛（白/绿/蓝/紫/金）────────────────────────────
		// 每档都写成「本档条件 || 更后面的 Boss」：这样即使玩家跳着推进（装了别的模组、
		// 或先打了后面的 Boss），也不会因为漏了某个前置而卡在未解锁状态。

		/// <summary>绿色档门槛：击败任意 Boss。</summary>
		private static bool AnyBossDowned {
			get {
				return NPC.downedSlimeKing || NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3
					|| NPC.downedQueenBee || NPC.downedDeerclops || Main.hardMode || NPC.downedMechBossAny
					|| NPC.downedPlantBoss || NPC.downedGolemBoss || NPC.downedFishron
					|| NPC.downedEmpressOfLight || NPC.downedAncientCultist || NPC.downedMoonlord;
			}
		}

		/// <summary>蓝色档门槛：骷髅王或其后的 Boss。</summary>
		private static bool SkeletronOrLaterDowned {
			get {
				return NPC.downedBoss3 || Main.hardMode || NPC.downedMechBossAny || NPC.downedPlantBoss
					|| NPC.downedGolemBoss || NPC.downedFishron || NPC.downedEmpressOfLight
					|| NPC.downedAncientCultist || NPC.downedMoonlord;
			}
		}

		/// <summary>紫色档门槛：全部新三王（或更后面的 Boss）。</summary>
		private static bool AllMechsOrLaterDowned {
			get {
				return (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3)
					|| NPC.downedPlantBoss || NPC.downedGolemBoss || NPC.downedFishron
					|| NPC.downedEmpressOfLight || NPC.downedAncientCultist || NPC.downedMoonlord;
			}
		}

		/// <summary>
		/// 金色档门槛：石巨人或其后的 Boss。
		/// 这里只认"确实排在石巨人之后"的（邪教徒、月亮领主）——鱼人公爵/光明女皇虽然常在石巨人之后打，
		/// 但严格来说可以在石巨人之前打，按门槛定义不能算数。
		/// </summary>
		private static bool GolemOrLaterDowned {
			get {
				return NPC.downedGolemBoss || NPC.downedAncientCultist || NPC.downedMoonlord;
			}
		}

		/// <summary>第 tier 档（0~4 = 白/绿/蓝/紫/金）材料商店是否已解锁。</summary>
		public static bool IsMaterialShopUnlocked(int tier) {
			return tier switch {
				<= 0 => true,                // 白色：无门槛
				1 => AnyBossDowned,          // 绿色：击败任意 Boss
				2 => SkeletronOrLaterDowned, // 蓝色：骷髅王或其后的 Boss
				3 => AllMechsOrLaterDowned,  // 紫色：全部新三王或其后的 Boss
				_ => GolemOrLaterDowned,     // 金色：石巨人或其后的 Boss
			};
		}

		/// <summary>未解锁时在对话里说明门槛的本地化键（下标 = 稀有度档；白色档无门槛，留空）。</summary>
		private static readonly string[] MaterialShopLockedHintKeys = [
			"",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopLockedGreen",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopLockedBlue",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopLockedPurple",
			"Mods.ArknightsMod.NPCs.Closure.Buttons.MaterialShopLockedGold",
		];

		private static string vanityShopFullName;
		private static readonly string[] materialShopFullNames = new string[MaterialShopNames.Length];

		public override void SetStaticDefaults() {
			Main.npcFrameCount[NPC.type] = 22;
			NPCID.Sets.ExtraFramesCount[NPC.type] = 6;
			NPCID.Sets.AttackFrameCount[NPC.type] = 1;
			// 手持扫描枪射击，射程比原来的近战挥砍远得多，探测范围也相应放大
			NPCID.Sets.DangerDetectRange[NPC.type] = 500;
			// AttackType: 0=投掷 1=射击 2=魔法 3=近战挥砍。改为 1，让她端枪平射而不是挥手打人。
			NPCID.Sets.AttackType[NPC.type] = 1;
			NPCID.Sets.AttackTime[NPC.type] = 18;
			NPCID.Sets.AttackAverageChance[NPC.type] = 10;
			NPCID.Sets.HatOffsetY[NPC.type] = 4;

			NPC.Happiness
				.SetBiomeAffection<ForestBiome>(AffectionLevel.Like)
				.SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike)
				.SetNPCAffection(NPCID.Mechanic, AffectionLevel.Love)
				.SetNPCAffection(NPCID.Cyborg, AffectionLevel.Like)
				.SetNPCAffection(NPCID.Merchant, AffectionLevel.Dislike)
				.SetNPCAffection(NPCID.Angler, AffectionLevel.Hate)
			;
		}

		/// <summary>
		/// 不提供随机名字列表：可露希尔就是她的名字，而类型名（<c>Mods.ArknightsMod.NPCs.Closure.DisplayName</c>）
		/// 正好也是「可露希尔」，不需要再排一个"名字"。
		/// <br/>返回空列表时 <c>NPC.HasGivenName</c> 为 false，<c>FullName</c> 直接等于类型名；
		/// 若像以前那样把显示名塞进列表，<c>FullName</c> 会用 <c>Game.NPCTitle</c> 把"名字 + 类型名"拼起来，
		/// 房屋界面等处就会显示成「可露希尔 可露希尔」。（坎诺特也是返回空列表，见 Cannot.cs。）
		/// </summary>
		public override List<string> SetNPCNameList() {
			return [];
		}

		public override void SetDefaults() {
			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = NPCAIStyleID.Passive;
			NPC.damage = 90;
			NPC.defense = 15;
			NPC.lifeMax = 1000;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;
			AnimationType = NPCID.Guide;
		}

		public override bool CanTownNPCSpawn(int numTownNPCs) {
			for (int i = 0; i < Main.maxNPCs; i++) {
				if (Main.npc[i].active && Main.npc[i].type == Type) {
					return false;
				}
			}
			if (ClosureWorldSpawnSystem.ClosureTownUnlocked) {
				return true;
			}
			foreach (Player _ in Main.ActivePlayers) {
				return true;
			}
			return false;
		}

		public override bool CanGoToStatue(bool toQueenStatue) => true;

		public int HelpCount = -1;
		public bool Helping;

		public override string GetChat() {
			WeightedRandom<string> chat = new();
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.Dialogue1"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.Dialogue2"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.Dialogue3"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.Dialogue4"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.Dialogue5"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.Dialogue6"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.Dialogue7"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.Dialogue8"));
			return chat;
		}

		public override void SetChatButtons(ref string button, ref string button2) {
			button = GetSwitchOptionDisplayName(ButtonCount);
			button2 = this.GetLocalizedValue("Buttons.Switch");
		}

		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			if (firstButton) {
				if (Helping) {
					HelpCount++;
					HelpCount %= 5;
				}
				else
					HelpCount = 0;
				Helping = false;
				switch (ButtonCount) {
					case 0:
						var chat = Language.GetText($"Mods.ArknightsMod.Dialogue.Closure.Help{HelpCount + 1}");
						switch (HelpCount) {
							case 0:
								chat = chat.WithFormatArgs($"[i:{ModContent.ItemType<_3DPrintingProcessingStation>()}]");
								break;
							case 1:
								chat = chat.WithFormatArgs($"[i:{ModContent.ItemType<OrironShard>()}]");
								break;
							case 2:
								chat = chat.WithFormatArgs($"[i:{ModContent.ItemType<Drone>()}]");
								break;
							case 4:
								chat = chat.WithFormatArgs($"[i:{ModContent.ItemType<Orundum>()}]", $"[i:{ModContent.ItemType<OrirockCube>()}]", $"[i:{ModContent.ItemType<OriginiumShard>()}]");
								break;
						}
						Main.npcChatText = chat.Value;
						Helping = true;
						break;
					case >= FirstMaterialShopButton and < VanityShopButton: {
						// 五个等级材料商店：按钮下标 1~5 依次对应 白/绿/蓝/紫/金。
						// 未达门槛就不进店，只在对话里说明要求（shop 保持为空）。
						int tier = ButtonCount - FirstMaterialShopButton;
						if (!IsMaterialShopUnlocked(tier)) {
							Main.npcChatText = Language.GetTextValue(MaterialShopLockedHintKeys[tier]);
							Main.npcChatCornerItem = 0;
							break;
						}
						shop = MaterialShopNames[tier];
						break;
					}
					case VanityShopButton:
						shop = VanityShopName;
						break;
					case AnnihilationButton:
						AO();
						break;
				}
				return;
			}
			else {
				// 第二个按钮「切换」：不再循环切换，而是把全部可切换页面列在对话框里让博士自己点。
				ShowSwitchMenu();
			}
		}

		/// <summary>
		/// 把当前所有可切换的页面按行写进对话框，一行一个选项。
		/// 每个选项用自定义聊天标签 [closure/序号:名称] 标记：NPC 对话正文由 ChatManager 解析成
		/// TextSnippet，tModLoader 每帧还会对鼠标悬停的那一个调用 OnHover()/OnClick()，
		/// 所以对话里这些字本身就是按钮（标签的解析见 ClosureSwitchOptions.cs）。
		/// 换行直接写 '\n'：原版 Utils.WordwrapStringSmart 会先把每个 snippet 的文本按 '\n' 硬拆成多行，
		/// 再逐行做 460px 折行；拆行和折行都用 CopyMorph 复制片段，
		/// 所以拆出来的每一段仍然是可点的选项（见 ClosureSwitchOptionSnippet.CopyMorph）。
		/// </summary>
		public static void ShowSwitchMenu() {
			var text = new StringBuilder(Language.GetTextValue("Mods.ArknightsMod.NPCs.Closure.Buttons.SwitchPrompt"));
			for (int i = 0; i < SwitchOptionCount; i++) {
				// 装饰括号用【】：聊天标签的正则是 .+? 直到第一个 ]，选项名里出现 ] 会把标签提前截断；
				// 正则里的 . 也不跨行，所以 '\n' 只能放在标签外面（放在两个标签之间正好换行）。
				text.Append('\n').Append('[').Append(SwitchOptionTag).Append('/').Append(i)
					.Append(":【").Append(GetSwitchOptionDisplayName(i)).Append("】]");
			}
			Main.npcChatText = text.ToString();
			Main.npcChatCornerItem = 0;
		}

		/// <summary>
		/// 玩家点了「切换」菜单里的第 option 项：先选中该页，再走一遍和点第一个按钮完全相同的流程。
		/// 帮助翻页、开商店、剿灭任务三个分支因此原样复用，这里不用复制任何逻辑，
		/// 也不会漏掉 tModLoader 在按钮点击里替我们做的事（比如真正把商店开起来）。
		/// </summary>
		public static void SelectSwitchOption(int option) {
			if (option < 0 || option >= SwitchOptionCount)
				return;

			NPC npc = Main.LocalPlayer.TalkNPC;
			if (npc == null || npc.type != ModContent.NPCType<Closure>())
				return;

			ButtonCount = option;

			// PreChatButtonClicked / OnChatButtonClicked 是 tModLoader 处理聊天按钮的两个入口
			// （见 NPCLoader）：前者允许别的代码否决这次点击，后者负责调用 NPC 本身并在
			// OnChatButtonClicked 给出商店名时开店。和真实点击按钮走的是同一条路。
			if (!NPCLoader.PreChatButtonClicked(true))
				return;
			NPCLoader.OnChatButtonClicked(true);
		}

		public void AO() {
			var System = Main.LocalPlayer.GetModPlayer<AOSystem>();
			if (System.QuestType == 1 && System.QuestNum != System.CountQuest) {
				System.QuestType = 0;
			}
			if (!System.AOStatus) {
				if (System.QuestType == 0) {
					Main.npcChatText = System.GetCurrentQuest().ToString();
					Main.npcChatCornerItem = System.GetCurrentQuest().QuestItem;
					System.AOStatus = true;
				}
				else {
					System.QuestNum = Main.rand.Next(System.CountQuest);
					Main.npcChatText = System.GetCurrentQuest().ToString();
					Main.npcChatCornerItem = System.GetCurrentQuest().QuestItem;
					System.AOStatus = true;
				}
			}
			else {
				if (System.CheckQuest()) {
					Main.npcChatText = System.GetCurrentQuest().THX();
					Main.npcChatCornerItem = 0;
					System.SpawnReward(NPC);
					System.AOStatus = false;
					System.QuestNum++;
					if (System.QuestNum == System.CountQuest)
						System.QuestType = 1;
					return;
				}
				else {
					Main.npcChatText = System.GetCurrentQuest().ToString();
					Main.npcChatCornerItem = System.GetCurrentQuest().QuestItem;
				}
			}
		}

		public class AOSystem : ModPlayer
		{
			public static List<Quest> Quests = [];
			public int QuestNum = 0;
			public int CountQuest;
			public bool AOStatus = false;
			public int QuestType = 0;

			public override void Initialize() {
				Quests.Clear();
				Quests.Add(new Quest(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AO", "Green Slimes"), ItemID.GreenSlimeBanner, 1, Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AOThanks")));
				Quests.Add(new Quest(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AO", "Blue Slimes"), ItemID.SlimeBanner, 1, Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AOThanks")));
				Quests.Add(new Quest(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AO", Language.GetText("Mods.ArknightsMod.NPCs.OriginiumSlug.DisplayName")), ModContent.ItemType<Items.Placeable.Banners.OriginiumSlugBanner>(), 1, Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AOThanks")));
				Quests.Add(new Quest(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AO", Language.GetText("Mods.ArknightsMod.NPCs.OriginiumSlugAlpha.DisplayName")), ModContent.ItemType<Items.Placeable.Banners.OriginiumSlugAlphaBanner>(), 1, Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AOThanks")));
				Quests.Add(new Quest(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AO", Language.GetText("Mods.ArknightsMod.NPCs.OriginiumSlugBeta.DisplayName")), ModContent.ItemType<Items.Placeable.Banners.OriginiumSlugBetaBanner>(), 1, Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AOThanks")));
				Quests.Add(new Quest(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AO", Language.GetText("Mods.ArknightsMod.NPCs.AcidOgSlug.DisplayName")), ModContent.ItemType<Items.Placeable.Banners.AcidOgSlugBanner>(), 1, Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AOThanks")));

				CountQuest = Quests.Count;
			}

			public Quest GetCurrentQuest() {
				try {
					return Quests[QuestNum];
				}
				catch {
					QuestNum = 0;
					return Quests[QuestNum];
				}
			}

			public int Current {
				get => QuestNum;
				set => QuestNum = value;
			}

			public bool CheckQuest() {
				try {
					var quest = Quests[QuestNum];
					foreach (var item in Player.inventory) {
						if (item.type == quest.QuestItem) {
							if (Player.CountItem(quest.QuestItem, quest.ItemAmount) >= quest.ItemAmount) {
								item.stack -= quest.ItemAmount;
								if (item.stack <= 0)
									item.SetDefaults();
								return true;
							}
						}
					}
					return false;
				}
				catch { return false; }
			}

			public void SpawnReward(NPC npc) {
				int reward = Item.NewItem(npc.GetSource_Loot(), Player.getRect(), ModContent.ItemType<Orundum>(), 50);
				if (Main.netMode == NetmodeID.MultiplayerClient && reward >= 0)
					NetMessage.SendData(MessageID.SyncItem, -1, -1, null, reward, 0f, 0f, 0f, 0);
				return;
			}

			public static int StartQuest() {
				return 0;
			}

			public override void SaveData(TagCompound tag) {
				tag.Add("QuestNum", QuestNum);
				tag.Add("QuestType", QuestType);
				tag.Add("AOStatus", AOStatus);
			}

			public override void LoadData(TagCompound tag) {

				QuestNum = tag.GetInt("QuestNum");
				QuestType = tag.GetInt("QuestType");
				AOStatus = tag.GetBool("AOStatus");

			}
		}

		public class Quest(string questMessage, int itemID, int itemAmount, string thxMessage = null)
		{
			public string QuestMessage = questMessage;
			public int ItemAmount = itemAmount;
			public int QuestItem = itemID;
			public string ThxMessage = thxMessage;
			public double Weight;

			public override string ToString() {
				return Language.GetTextValue(QuestMessage, Main.LocalPlayer.name);
			}

			public string THX() {
				return Language.GetTextValue(ThxMessage);
			}
		}

		// tModLoader 的商店展示槽位是有限的（40 格）。本 NPC 现在注册六个商店：
		// 五个等级材料商店（货架由 ModifyActiveShop 按当前进度铺满本等级，不再每日随机）
		// 加一个每日轮换的时装商店。下面注册的静态列表只是给「哪里能买到」这类外部工具
		// 查询用的样本，本身并不会被玩家直接看到。
		// ⚠ 这里注册的条目没有挂任何 Condition，运行时全部算"生效中"——一旦数量超过槽位
		//   上限，tModLoader 每次开店都会弹出"物品太多，塞不进商店里 :("的警告。材料池
		//   （forceAllTiers=true 时 60+ 种）和全部时装袋（当前 50+ 个）都远超 40，
		//   所以每店都要截断到 MaxStaticShopSampleCount，不能把 GetContent 的结果直接全塞进去。
		private const int MaxStaticShopSampleCount = 30;

		/// <summary>没有接入稀有度体系的材料（材料目录下未继承 ArknightsMaterial 的那几种）沿用的旧价。</summary>
		private const int FallbackMaterialPrice = 10;

		/// <summary>上述材料的个别定价覆盖（物品类型 → 合成玉售价）。</summary>
		private static Dictionary<int, int> fallbackMaterialPrices;

		/// <summary>取未接入稀有度体系的材料的售价：先查覆盖表，再退回默认价。</summary>
		private static int GetFallbackMaterialPrice(int materialType) {
			// 惰性建表：静态字段初始化时 ModContent.ItemType 还拿不到内容 ID。
			fallbackMaterialPrices ??= new Dictionary<int, int> {
				// 源石碎片单独定价（比兜底价便宜）。
				[ModContent.ItemType<OriginiumShard>()] = 8,
			};
			return fallbackMaterialPrices.TryGetValue(materialType, out int price) ? price : FallbackMaterialPrice;
		}

		/// <summary>
		/// 给货架上的材料标合成玉买入价：A 组材料（继承 <see cref="ArknightsMaterial"/> 的普通材料）
		/// 按稀有度定价 —— 白 4 / 绿 10 / 蓝 50 / 紫 200 / 金 600 合成玉
		/// （见 <see cref="ArknightsMaterial.GetOrundumBuyPrice"/>）；
		/// 其余如蟹钳、源石碎片这类还没接入稀有度体系的材料维持原来的 10 合成玉。
		/// <br/>回收价恒为买入价的一半，由 Players/MaterialRecyclePlayer 在她这里卖材料时以合成玉结算。
		/// <br/>碳素（<see cref="ArknightsMaterial.TradedWithOrundum"/> 为 false）改回金币结算，
		/// 货架价由它自己的 <c>Item.value</c> 决定，这里不插手。
		/// </summary>
		private static Item PriceMaterialByRarity(int materialType) {
			var material = new Item(materialType);

			// 不走合成玉结算的材料（碳素；以及不在货架上的采集物）原样返回：
			// 不挂 shopCustomPrice/shopSpecialCurrency，原版就会按 Item.value 以金币定价。
			if (material.ModItem is ArknightsMaterial { TradedWithOrundum: false })
				return material;

			// 接入稀有度体系的材料按档定价；其余（尚未继承基类的裸 ModItem 材料）用兜底价，可单独覆盖。
			int price = GetFallbackMaterialPrice(materialType);
			if (material.ModItem is ArknightsMaterial arkMaterial)
				price = ArknightsMaterial.GetOrundumBuyPrice(arkMaterial.Rarity);
			material.shopCustomPrice = price;
			material.shopSpecialCurrency = ArknightsMod.OrundumCurrencyId;
			return material;
		}

		public override void AddShops() {
			// 五个等级材料商店：下面的静态列表只是给「哪里能买到」这类外部工具查询用的样本
			// （每店最多 MaxStaticShopSampleCount 条），真实货架由 ModifyActiveShop 铺。
			for (int tier = 0; tier < MaterialShopNames.Length; tier++) {
				var materialShop = new NPCShop(Type, MaterialShopNames[tier]);
				if (tier == 0) {
					// 白色材料商店第 0 格：常驻家具（原来是材料商店的第 0 格）
					materialShop.Add(new Item(ModContent.ItemType<Items.Placeable.Furniture.DareUsa>()) {
						shopCustomPrice = 30,
						shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
					});
				}
				foreach (int materialType in NPCShopSystem.BuildClosurePinnedMaterials()) {
					if (GetShopTierOf(materialType) == tier)
						materialShop.Add(PriceMaterialByRarity(materialType));
				}
				int sampleCount = 0;
				foreach (int materialType in NPCShopSystem.BuildClosureMaterialPool(true)) {
					if (sampleCount >= MaxStaticShopSampleCount)
						break;
					if (GetShopTierOf(materialType) != tier)
						continue;
					materialShop.Add(PriceMaterialByRarity(materialType));
					sampleCount++;
				}
				materialShop.Register();
			}

			var vanityShop = new NPCShop(Type, VanityShopName);
			int bagSampleCount = 0;
			foreach (var bag in ModContent.GetContent<ArknightsVanityBag>()) {
				if (bagSampleCount >= MaxStaticShopSampleCount)
					break;
				vanityShop.Add(new Item(bag.Type) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				});
				bagSampleCount++;
			}
			vanityShop.Register();
		}

		public override void OnSpawn(IEntitySource source) {
			if (source is EntitySource_WorldGen || source is EntitySource_SpawnNPC) {
				if (!ClosureWorldSpawnSystem.ClosureTownUnlocked) {
					ClosureWorldSpawnSystem.ClosureTownUnlocked = true;
					if (Main.netMode == NetmodeID.Server) {
						NetMessage.SendData(MessageID.WorldData);
					}
				}
			}
			NPCShopSystem.UpdateClosureShop(Mod, true);
		}

		/// <summary>
		/// 材料该进哪个等级的商店：A 组材料按自己的稀有度；没接入稀有度体系的那几种
		/// （蟹钳、源石碎片…）一律进白色商店，与它们沿用旧价（<see cref="FallbackMaterialPrice"/>）的处理一致。
		/// </summary>
		private static int GetShopTierOf(int materialType) {
			var material = new Item(materialType);
			if (material.ModItem is ArknightsMaterial arkMaterial)
				return Math.Clamp(arkMaterial.Rarity, 0, MaterialShopNames.Length - 1);
			return 0;
		}

		/// <summary>
		/// 铺一个等级的材料商店：只列该等级已解锁的材料（碳素是蓝档，会进蓝色商店）。
		/// 库存现场计算（<see cref="NPCShopSystem.BuildClosureMaterialStock"/>）而不是读缓存：
		/// 材料货架是进度的纯函数，缓存只在每日换日时刷新，会让当天新解锁的等级商店一直空架。
		/// </summary>
		private void FillMaterialShop(int tier, Item[] items) {
			Array.Fill(items, null);

			int slot = 0;
			if (tier == 0) {
				// 白色材料商店第 0 格：常驻家具
				items[slot++] = new Item(ModContent.ItemType<Items.Placeable.Furniture.DareUsa>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				};
			}

			foreach (int materialType in NPCShopSystem.BuildClosureMaterialStock()) {
				if (slot >= items.Length)
					break;
				if (GetShopTierOf(materialType) != tier)
					continue;
				items[slot++] = PriceMaterialByRarity(materialType);
			}

			// 原版商店面板的标题是写死的「商店」，等级名交给 ClosureShopTitleSystem 自绘。
			ClosureShopTitleState.Current = GetSwitchOptionLabel(FirstMaterialShopButton + tier);
		}

		/// <summary>铺时装商店（这一家仍然是每日轮换）。</summary>
		private void FillVanityShop(Item[] items) {
			if (NPCShopSystem.ClosureTodaysRotation.Count == 0)
				NPCShopSystem.UpdateClosureShop(Mod, true);
			Array.Fill(items, null);

			items[0] = new Item(ModContent.ItemType<DoctorArchiveBag>()) {
				shopCustomPrice = 100,
				shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
			};

			var rotation = NPCShopSystem.ClosureTodaysRotation;
			for (int j = 0; j < rotation.Count && j + 1 < items.Length; j++) {
				items[j + 1] = new Item(rotation[j]) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				};
			}

			ClosureShopTitleState.Current = GetSwitchOptionLabel(VanityShopButton);
		}

		public override void ModifyActiveShop(string shopName, Item[] items) {
			for (int tier = 0; tier < MaterialShopNames.Length; tier++) {
				materialShopFullNames[tier] ??= NPCShopDatabase.GetShopName(ModContent.NPCType<Closure>(), MaterialShopNames[tier]);
				if (shopName != materialShopFullNames[tier])
					continue;
				FillMaterialShop(tier, items);
				return;
			}

			vanityShopFullName ??= NPCShopDatabase.GetShopName(ModContent.NPCType<Closure>(), VanityShopName);
			if (shopName == vanityShopFullName)
				FillVanityShop(items);
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback) {
			damage = 30;
			knockback = 4f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) {
			cooldown = 30;
			randExtraCooldown = 30;
		}

		// 主动攻击改为使用「工程部特制扫描枪」，但只会武器的普通攻击——
		// 发射和玩家左键相同的能量镖弹幕（ClosureScanShotProjectile），不涉及武器的任何技能：
		// 技能依赖 WeaponPlayer 上的技力/充能状态，那是玩家专属的系统，NPC 身上并不存在，
		// 所以这里只复用普攻弹幕，不去碰技能逻辑。
		public override void TownNPCAttackProj(ref int projType, ref int attackDelay) {
			projType = ModContent.ProjectileType<Projectiles.Medic.Closure.ClosureScanShotProjectile>();
			attackDelay = 1;
		}

		// 弹幕本身直线飞行、不受重力影响（见 ClosureScanShotProjectile.AI），所以重力补正给 0；
		// 速度对齐武器的 Item.shootSpeed=14，保证 NPC 打出来的手感和玩家左键一致。
		public override void TownNPCAttackProjSpeed(ref float multiplier, ref float gravityCorrection, ref float randomOffset) {
			multiplier = 14f;
			gravityCorrection = 0f;
			randomOffset = 0.1f;
		}

		// 攻击时在手上绘制扫描枪的物品贴图（AttackType=1 走这个 hook，原来的近战 TownNPCAttackSwing 不再适用）。
		//
		// horizontalHoldoutOffset 会被原版直接拿去当绘制原点用（Main.cs：origin = (-offset, 高度/2)），
		// 数值越大枪越往前伸、离身体越远。原版自己算这个值的公式是
		//     DrawPlayerItemPos(1f, 物品).X - 4
		// 而 DrawPlayerItemPos 对没有自定义 HoldoutOffset 的物品固定返回 X=10，也就是默认 6。
		// 原版给个别 NPC 把枪往回收，用的也是加大这个减数的办法（num10 = 16/18/28）。
		// 这里沿用同一套算法，只把减数提出来方便调：数值越大枪贴得越近。
		private const int GunPullback = 6;

		public override void DrawTownAttackGun(ref Texture2D item, ref Rectangle itemFrame, ref float scale, ref int horizontalHoldoutOffset) {
			int itemType = ModContent.ItemType<Items.Weapons.Medic.Closure.ClosureScanGun>();
			Main.instance.LoadItem(itemType);
			item = TextureAssets.Item[itemType].Value;
			itemFrame = item.Frame();
			scale = 1f;
			horizontalHoldoutOffset = (int)Main.DrawPlayerItemPos(1f, itemType).X - GunPullback;
		}
	}
}
