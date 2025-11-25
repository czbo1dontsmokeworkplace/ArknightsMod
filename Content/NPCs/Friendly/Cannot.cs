using ArknightsMod.Content.Currencies;
using ArknightsMod.Content.Items;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.NPCs.Enemy.Chapter6;
using ArknightsMod.Content.NPCs.Enemy.GT;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;
using ArknightsMod.Content.NPCs.Enemy.TillChapter7;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ItemDropRules;
using Terraria.GameContent.Personalities;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.Utilities;
using Terraria.Utilities;


namespace ArknightsMod.Content.NPCs.Friendly
{
	[AutoloadHead]
	public class Cannot : ModNPC
	{
		// The list of items in the traveler's shop. Saved with the world and set when the traveler spawns. Synced by the server to clients in multi player
		public readonly static List<Item> shopItems = new();
		// A static instance of the declarative shop, defining all the items which can be brought. Used to create a new inventory when the NPC spawns
		public static CannotShop Shop;
		public const string ShopName = "Shop";
		public int TouchCount = 0;
		// 消失计时器：记录NPC已存在的帧数（60帧=1秒）
		private int despawnTimer = 0;
		// 最大距离：超过此距离则消失（例如：2000像素，约125个砖块）
		private const float MaxDistanceFromPlayer = 2000f;
		public bool isnpcexist = false;
		public int summoncd = 0;

		int[] Eliteslist = new int[5] { ModContent.NPCType<ShieldGuard>(),ModContent.NPCType<IceCleaver>(), ModContent.NPCType<Seniorcaster>(), ModContent.NPCType<InsaneZombieL>(), ModContent.NPCType<Oneiros>()};

		public override void SetStaticDefaults() {
			Main.npcFrameCount[NPC.type] = 23;
			NPCID.Sets.ExtraFramesCount[NPC.type] = 6;
			NPCID.Sets.AttackFrameCount[NPC.type] = 1;
			NPCID.Sets.DangerDetectRange[NPC.type] = 40;
			NPCID.Sets.ActsLikeTownNPC[NPC.type] = true;
			NPCID.Sets.AttackType[NPC.type] = 3;
			NPCID.Sets.AttackTime[NPC.type] = 18;
			NPCID.Sets.AttackAverageChance[NPC.type] = 10;
			NPCID.Sets.HatOffsetY[NPC.type] = 4;
			
		}

		public override List<string> SetNPCNameList() {
			return [Language.GetTextValue($"Mods.ArknightsMod.NPCs.{GetType().Name}.DisplayName")];
		}
		public override void SetDefaults() {
			NPC.friendly = false;
			NPC.dontTakeDamage = false;
			NPC.dontTakeDamageFromHostiles= true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = 7;
			NPC.damage = 0;
			NPC.defense = 99;
			NPC.lifeMax = 1000;
			NPC.npcSlots = 7f;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0f;
			AnimationType = NPCID.Guide;
		}

		
		public override bool CanGoToStatue(bool toQueenStatue) => false;

		public override void OnHitByItem(Player player, Item item, NPC.HitInfo hit, int damageDone) {
			if (summoncd <= 0) {
				summonElites();
				summoncd = 600;
			}
		}
		public override void OnHitByProjectile(Projectile projectile, NPC.HitInfo hit, int damageDone) {
			if (summoncd <= 0) {
				summonElites();
				summoncd = 600;
			}
		}

		public override string GetChat() {
			WeightedRandom<string> chat = new();
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Dialogue1"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Dialogue2"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Dialogue3"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Dialogue4"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Dialogue5"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Dialogue6"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Dialogue7"));
			chat.Add(Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Dialogue8"));
			return chat;
		}
		public override void SetChatButtons(ref string button, ref string button2) {
			button = this.GetLocalizedValue("Buttons.Shop");
			button2 = this.GetLocalizedValue("Buttons.Touch");

		}
		
		public void summonElites() {
			if (Eliteslist.Length == 0)
				return;

			// 随机选择一个精英类型
			int randomIndex = Main.rand.Next(Eliteslist.Length);
			int selectedNPCType = Eliteslist[randomIndex];

			// 计算生成位置：在 Cannot 上方偏移（避免卡进地形）
			int offsetX = Main.rand.NextBool()
				? Main.screenWidth / 2 + Main.rand.Next(0, 160)
				: -(Main.screenWidth / 2 + Main.rand.Next(0, 160));
			int offsetY = -Main.rand.Next(120, 180); // 向上偏移

			Vector2 spawnPosition = new Vector2(NPC.Center.X + offsetX, NPC.Center.Y + offsetY);

			// 确保在有效范围内（可选）
			//if (!WorldGen.InWorld((int)spawnPosition.X / 16, (int)spawnPosition.Y / 16))
				//return;

			// 生成 NPC
			NPC.NewNPC(
				NPC.GetSource_FromAI(), // 或 EntitySource_NaturalSpawn
				(int)spawnPosition.X,
				(int)spawnPosition.Y,
				selectedNPCType
			);
		}
		public bool anyPlayerNearby = false;
		public override void AI() {
			if (summoncd > 0) {
				summoncd--;
			}
			//// 若玩家不存在（如死亡），直接消失
			//else {
			//	Despawn();
			//	return;
			//}
			if (TouchCount >= 5 && !isnpcexist) {
				DropLoot();
				Main.NewText("坎诺特逃走了",Color.LightBlue);
				TouchCount = 0;
				return;
			}
		}

		public void Despawn() {
			NPC.active = false;
			NPC.netUpdate = true;
		}
		private void DropLoot() {
			// 随机选一个商品
			Item selectedItem = shopItems[Main.rand.Next(shopItems.Count)];

			// 克隆一份用于掉落
			Item dropItem = selectedItem.Clone();

			// 设置数量为 1
			dropItem.stack = 1;

			// 掉落物品
			Item.NewItem(
				NPC.GetSource_Loot(),
				NPC.getRect(),
				dropItem.type,
				dropItem.stack,
				noGrabDelay: false,
				prefixGiven: dropItem.prefix
			);

			// 掉落后消失
			Despawn();
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.Common(ModContent.ItemType<OriginiumIngot>(), 1, 2, 3));
		}
			
		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			if (firstButton) {
				shop = ShopName;
				return;
			}
			else {
				bool isnpcexist = false;
				for (int i = 0; i < Main.maxNPCs; i++) {
					NPC SeekForNPCs = Main.npc[i];
					if (SeekForNPCs.active && Array.Exists(Eliteslist, x => x == SeekForNPCs.type)) {
						isnpcexist = true;
						break;
					}
				}
					if (isnpcexist) {
						Main.npcChatText = Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Touchcd");
					}
					else {
					TouchCount++;
					switch (TouchCount) {
							case 1:
								summonElites();
								Main.npcChatText = Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Touch1");
								break;
							case 2:
								summonElites();
								Main.npcChatText = Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Touch2");
								break;
							case 3:
								summonElites();
								Main.npcChatText = Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Touch3");
								break;
							case 4:
								summonElites();
								Main.npcChatText = Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Touch4");
								break;
							case 5:
								summonElites();
								break;

						}
					}
				}
			
		}

		//骷髅王之前，对应价值为4的商品格
		private int countBeforeSkeletron;
		private int countBetweenSkeletronAndPlantera;
		private int countBetweenPlanteraAndDukeFishron;
		private int countFromFishronOnward;
		public override void AddShops() {

			Shop = new CannotShop(NPC.type);
			Shop.Register();
			//foreach (var modItem in Mod.GetContent<ModItem>()) {
			//	if (modItem.GetType().Namespace == "ArknightsMod.Content.Items.Accessories.Rogue") {
			//		if (modItem.Type == ModContent.ItemType<Items.Accessories.Rogue.HotWaterKettle>())
			//			continue;
			//		Item item = new(modItem.Type) {
			//			shopSpecialCurrency = ArknightsMod.OriginiumIngotCurrencyId
			//		};
			//		npcShop.Add(item);
			//	}
			//}


		}
		public override void OnSpawn(IEntitySource source) {
			// ✅ 动态计算槽位（现在可以安全使用 NPC.downedXXX）
			int countBeforeSkeletron = 1 +
				(NPC.downedSlimeKing ? 1 : 0) +//史莱姆
				(NPC.downedBoss1 ? 1 : 0) +//克眼
				(NPC.downedBoss2 ? 1 : 0);//邪恶boss

			int countBetweenSkeletronAndPlantera =
				(NPC.downedBoss3 ? 1 : 0) +//骷髅王
				(NPC.downedQueenBee ? 1 : 0) +//蜂后
				(Main.hardMode ? 1 : 0) +//肉山
				(NPC.downedMechBoss1 ? 1 : 0) +//机械1
				(NPC.downedMechBoss2 ? 1 : 0) +//机械2
				(NPC.downedMechBoss3 ? 1 : 0);//机械3

			int countBetweenPlanteraAndDukeFishron =
				(NPC.downedPlantBoss ? 1 : 0) +//世花
				(NPC.downedGolemBoss ? 1 : 0);//石巨人

			int countFromFishronOnward =
				(NPC.downedFishron ? 1 : 0) +//猪鲨
				(NPC.downedEmpressOfLight ? 1 : 0) +//光女
				(NPC.downedAncientCultist ? 1 : 0) +//教徒
				(NPC.downedMoonlord ? 1 : 0);//月总

			// ✅ 创建临时 CannotShop（不注册到全局），仅用于生成商品
			var tempShop = new CannotShop(NPC.type);
			if (countBeforeSkeletron > 0)
				tempShop.AddPoolFromNameSpace("Rogue.Rarity_l1", countBeforeSkeletron, "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l1", Mod);
			if (countBetweenSkeletronAndPlantera > 0)
				tempShop.AddPoolFromNameSpace("Rogue.Rarity_l2", countBetweenSkeletronAndPlantera, "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l2", Mod);
			if (countBetweenPlanteraAndDukeFishron > 0)
				tempShop.AddPoolFromNameSpace("Rogue.Rarity_l3", countBetweenPlanteraAndDukeFishron, "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3", Mod);
			if (countFromFishronOnward > 0)
				tempShop.AddPoolFromNameSpace("Rogue.Rarity_l4", countFromFishronOnward, "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4", Mod);

			// ✅ 生成商品列表
			shopItems.Clear();
			shopItems.AddRange(tempShop.GenerateNewInventoryList());

			// 同步到客户端（多人游戏）
			if (Main.netMode == NetmodeID.Server) {
				NetMessage.SendData(MessageID.WorldData);
			}
		}
		public override bool CanChat() {
			return true;
		}

		public override void TownNPCAttackStrength(ref int damage, ref float knockback) {
			damage = 30;
			knockback = 4f;
		}

		public override void TownNPCAttackCooldown(ref int cooldown, ref int randExtraCooldown) {
			cooldown = 30;
			randExtraCooldown = 30;
		}
	}
	public class CannotShop : AbstractNPCShop
	{
		public new record Entry(Item Item, List<Condition> Conditions) : AbstractNPCShop.Entry
		{
			IEnumerable<Condition> AbstractNPCShop.Entry.Conditions => Conditions;

			public bool Disabled { get; private set; }

			public Entry Disable() {
				Disabled = true;
				return this;
			}

			public bool ConditionsMet() => Conditions.All(c => c.IsMet());
		}

		public record Pool(string Name, int Slots, List<Entry> Entries)
		{
			public Pool Add(Item item, params Condition[] conditions) {
				Entries.Add(new Entry(item, conditions.ToList()));
				return this;
			}

			public Pool Add<T>(params Condition[] conditions) where T : ModItem => Add(ModContent.ItemType<T>(), conditions);
			public Pool Add(int item, params Condition[] conditions) => Add(ContentSamples.ItemsByType[item], conditions);

			// Picks a number of items (up to Slots) from the entries list, provided conditions are met.
			public IEnumerable<Item> PickItems() {
				// This is not a fast way to pick items without replacement, but it's certainly easy. Be careful not to do this many many times per frame, or on huge lists of items.
				var list = Entries.Where(e => !e.Disabled && e.ConditionsMet()).ToList();
				for (int i = 0; i < Slots; i++) {
					if (list.Count == 0)
						break;

					int k = Main.rand.Next(list.Count);
					yield return list[k].Item;

					// remove the entry from the list so it can't be selected again this pick
					list.RemoveAt(k);
				}
			}
		}

		public List<Pool> Pools { get; } = new();

		public CannotShop(int npcType) : base(npcType) { }

		public override IEnumerable<Entry> ActiveEntries => Pools.SelectMany(p => p.Entries).Where(e => !e.Disabled);

		public Pool AddPool(string name, int slots) {
			var pool = new Pool(name, slots, new List<Entry>());
			Pools.Add(pool);
			return pool;
		}
		public Pool AddPoolFromNameSpace(string name, int slots, string fromNamespace, Mod mod) {
			var pool = new Pool(name, slots, new List<Entry>());

			var items = mod.GetContent<ModItem>()
				.Where(item => item.GetType().Namespace == fromNamespace)
				.ToList();

			if (items.Count == 0) {
				Main.NewText($"[CannotShop] 警告：命名空间 '{fromNamespace}' 中未找到任何 ModItem！", Color.OrangeRed);
			}

			foreach (var modItem in items) {
				var shopItem = new Item(modItem.Type);
				shopItem.shopSpecialCurrency = ArknightsMod.OriginiumIngotCurrencyId;
				pool.Add(shopItem); // 调用 Add(Item, Condition[])
			}

			Pools.Add(pool);
			return pool;
		}

		// Some methods to add a pool with a single item
		public void Add(Item item, params Condition[] conditions) => AddPool(item.ModItem?.FullName ?? $"Terraria/{item.type}", slots: 1).Add(item, conditions);
		public void Add<T>(params Condition[] conditions) where T : ModItem => Add(ModContent.ItemType<T>(), conditions);
		public void Add(int item, params Condition[] conditions) => Add(ContentSamples.ItemsByType[item], conditions);

		// Here is where we actually 'roll' the contents of the shop
		public List<Item> GenerateNewInventoryList() {
			var items = new List<Item>();
			foreach (var pool in Pools) {
				items.AddRange(pool.PickItems());
			}
			return items;
		}

		public override void FillShop(ICollection<Item> items, NPC npc) {
			// use the items which were selected when the NPC spawned.
			foreach (var item in Cannot.shopItems) {
				// make sure to add a clone of the item, in case any ModifyActiveShop hooks adjust the item when the shop is opened
				items.Add(item.Clone());
			}
		}

		public override void FillShop(Item[] items, NPC npc, out bool overflow) {
			overflow = false;
			int i = 0;
			// use the items which were selected when the NPC spawned.
			foreach (var item in Cannot.shopItems) {

				if (i == items.Length - 1) {
					// leave the last slot empty for selling
					overflow = true;
					return;
				}

				// make sure to add a clone of the item, in case any ModifyActiveShop hooks adjust the item when the shop is opened
				items[i++] = item.Clone();
			}
		}
	}
	public class CannotSpawn : ModSystem
	{
		private int spawnCooldown = 0;
		private const int MinCooldown = 1800;
		private const int MaxCooldown = 3600;
		bool hasBeatenFirstBoss = NPC.downedSlimeKing || NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3 || NPC.downedQueenBee || Main.hardMode;
		private void TrySpawnMerchant() {
			Player player = Main.player[Main.myPlayer];
			if (IsNPCAlreadySpawned()) {
				return;
			}
			if (player == null || !player.active)
				return;
			if (hasBeatenFirstBoss) {
				// 允许在地表（Overworld）或洞穴（Cavern）生成

				bool isInOverworld = player.ZoneOverworldHeight;
				bool isInCavern = player.ZoneGemCave;
				if (!isInOverworld && !isInCavern&&(player.ZoneCrimson||player.ZoneCorrupt))
					return;

				// 随机生成位置（以玩家为中心，半径800像素内）
				Vector2 spawnPos = player.Center + new Vector2(
					Main.rand.Next(-800, 801),
					Main.rand.Next(-800, 801)
				);

				// 确保生成在固体方块上
				if (!WorldGen.SolidTile(Framing.GetTileSafely(spawnPos)))
					return;

				// 生成NPC
				int npcType = ModContent.NPCType<Cannot>();
				NPC.NewNPC(new EntitySource_WorldEvent(), (int)spawnPos.X, (int)spawnPos.Y, npcType);
			}
		}
		
		public override void PostUpdateNPCs() {
			if (!Main.gameInactive && Main.hasFocus) {
				if (spawnCooldown > 0) {
					spawnCooldown--;
					return;
				}

				// 满足概率时，调用TrySpawnMerchant尝试生成
				if (Main.rand.NextBool(8000)) {
					TrySpawnMerchant(); // 调用辅助方法
					spawnCooldown = Main.rand.Next(MinCooldown, MaxCooldown);
				}
			}
		}
		private bool IsNPCAlreadySpawned() {
			int npcType = ModContent.NPCType<Cannot>();
			// 遍历所有NPC，检查是否存在目标类型且活跃的实例
			foreach (NPC npc in Main.npc) {
				if (npc.active && npc.type == npcType) {
					return true; // 已存在
				}
			}
			return false; // 不存在
		}
	}
}
