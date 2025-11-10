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
		public const string ShopName = "Shop";
		public int TouchCount = 0;
		// 消失计时器：记录NPC已存在的帧数（60帧=1秒）
		private int despawnTimer = 0;
		// 最大存在时间（例如：3000帧=50秒，可自行调整）
		private const int MaxExistTime = 3600;
		// 最大距离：超过此距离则消失（例如：2000像素，约125个砖块）
		private const float MaxDistanceFromPlayer = 2000f;
		public bool isnpcexist = false;

		int[] Eliteslist = new int[5] { ModContent.NPCType<ShieldGuard>(),ModContent.NPCType<IceCleaver>(), ModContent.NPCType<Seniorcaster>(), ModContent.NPCType<InsaneZombieL>(), ModContent.NPCType<Oneiros>()};

		public override void SetStaticDefaults() {
			Main.npcFrameCount[NPC.type] = 23;
			NPCID.Sets.ExtraFramesCount[NPC.type] = 6;
			NPCID.Sets.AttackFrameCount[NPC.type] = 1;
			NPCID.Sets.DangerDetectRange[NPC.type] = 40;
			NPCID.Sets.AttackType[NPC.type] = 3;
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

		public override List<string> SetNPCNameList() {
			return [Language.GetTextValue($"Mods.ArknightsMod.NPCs.{GetType().Name}.DisplayName")];
		}

		public override void SetDefaults() {
			NPC.townNPC = true;
			NPC.friendly = true;
			NPC.width = 18;
			NPC.height = 40;
			NPC.aiStyle = 7;
			NPC.damage = 90;
			NPC.defense = 15;
			NPC.lifeMax = 1000;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;
			AnimationType = NPCID.Guide;
			NPC.dontTakeDamage = true;
		}

		
		public override bool CanGoToStatue(bool toQueenStatue) => true;



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
		public override void AI() {


			// 2. 距离消失：远离玩家后消失（可选，增强临时感）
			Player player = Main.player[NPC.target];
			if (player != null && player.active) {
				float distance = Vector2.Distance(NPC.Center, player.Center);
				if (distance > MaxDistanceFromPlayer) {
					Despawn();
					return;
				}
			}
			//// 若玩家不存在（如死亡），直接消失
			//else {
			//	Despawn();
			//	return;
			//}
			if (TouchCount >= 5 && !isnpcexist) {
				DropLoot();
				TouchCount = 0;
				return;
			}
		}

		public void Despawn() {
			NPC.active = false;
			NPC.netUpdate = true;
		}
		private void DropLoot() {
			// 在NPC位置生成物品
			Item.NewItem(
				NPC.GetSource_Loot(),
				NPC.getRect(), // 掉落范围（NPC碰撞箱）
				ModContent.ItemType<OriginiumIngot>(), // 物品ID
				2 // 数量
			);
			Despawn();
		}

		public override void ModifyNPCLoot(NPCLoot npcLoot) {
			npcLoot.Add(ItemDropRule.Common(ArknightsMod.OriginiumIngotCurrencyId, 1, 2, 3));
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
								Main.npcChatText = Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Touch2");
								break;
							case 4:
								summonElites();
								Main.npcChatText = Language.GetTextValue("Mods.ArknightsMod.Dialogue.Cannot.Touch2");
								break;
							case 5:
								summonElites();
								break;

						}
					}
				}
			
		}
		
		public override void AddShops() {
			var npcShop = new NPCShop(Type, ShopName);
			foreach (var modItem in Mod.GetContent<ModItem>()) {
				if (modItem.GetType().Namespace == "ArknightsMod.Content.Items.Accessories.Rogue") {
					if (modItem.Type == ModContent.ItemType<Items.Accessories.Rogue.HotWaterKettle>())
						continue;
					Item item = new(modItem.Type) {
						shopSpecialCurrency = ArknightsMod.OriginiumIngotCurrencyId
					};
					npcShop.Add(item);
				}
			}

			npcShop.Register();
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

	public class CannotSpawn : ModSystem
	{
		private int spawnCooldown = 0;
		private const int MinCooldown = 1800;
		private const int MaxCooldown = 3600;
		
		private void TrySpawnMerchant() {
			Player player = Main.player[Main.myPlayer];
			if (player == null || !player.active)
				return;
			if (NPC.downedBoss1 || NPC.downedBoss2 || NPC.downedBoss3 || NPC.downedQueenBee || Main.hardMode) {


				
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
	}
}
