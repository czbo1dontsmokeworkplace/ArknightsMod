using ArknightsMod.Content.Items;
using ArknightsMod.Content.Items.Consumables.VanityBags;
using ArknightsMod.Content.Items.DisplayForUI;
using ArknightsMod.Content.Items.Gacha;
using ArknightsMod.Content.Items.Material;
using ArknightsMod.Systems;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.DataStructures;
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
		public static string[] ShopName => ["Shop", "Shop2"];

		public static int ButtonCount;

		public override void SetStaticDefaults() {
			Main.npcFrameCount[NPC.type] = 22;
			NPCID.Sets.ExtraFramesCount[NPC.type] = 6;
			NPCID.Sets.AttackFrameCount[NPC.type] = 1;
			NPCID.Sets.DangerDetectRange[NPC.type] = 40;
			NPCID.Sets.AttackType[NPC.type] = 3;
			NPCID.Sets.AttackTime[NPC.type] = 18;
			NPCID.Sets.AttackAverageChance[NPC.type] = 10;
			NPCID.Sets.HatOffsetY[NPC.type] = 4; // For when a party is active, the party hat spawns at a Y offset.
												 // NPCID.Sets.ShimmerTownTransform[NPC.type] = true; // This set says that the Town NPC has a Shimmered form. Otherwise, the Town NPC will become transparent when touching Shimmer like other enemies.



			// Set Example Person's biome and neighbor preferences with the NPCHappiness hook. You can add happiness text and remarks with localization (See an example in ExampleMod/Localization/en-US.lang).
			// NOTE: The following code uses chaining - a style that works due to the fact that the SetXAffection methods return the same NPCHappiness instance they're called on.
			NPC.Happiness
				.SetBiomeAffection<ForestBiome>(AffectionLevel.Like) // Example Person prefers the forest.
				.SetBiomeAffection<SnowBiome>(AffectionLevel.Dislike) // Example Person dislikes the snow.
																	  // .SetBiomeAffection<ExampleSurfaceBiome>(AffectionLevel.Love) // Example Person likes the Example Surface Biome
				.SetNPCAffection(NPCID.Mechanic, AffectionLevel.Love) // Loves living near the dryad.
				.SetNPCAffection(NPCID.Cyborg, AffectionLevel.Like) // Likes living near the guide.
				.SetNPCAffection(NPCID.Merchant, AffectionLevel.Dislike) // Dislikes living near the merchant.
				.SetNPCAffection(NPCID.Angler, AffectionLevel.Hate) // Hates living near the demolitionist.
			; // < Mind the semicolon!
		}

		public override List<string> SetNPCNameList() {
			return [Language.GetTextValue($"Mods.ArknightsMod.NPCs.{GetType().Name}.DisplayName")];
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
			foreach (Player player in Main.player) {
				if (!player.active) {
					continue;
				}
				if (player.statDefense > 0) {
					return true;
				}
			}
			return false;
		}

		// Make this Town NPC teleport to the King and/or Queen statue when triggered. Return toKingStatue for only King Statues. Return !toKingStatue for only Queen Statues. Return true for both.
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
			string Text = ButtonCount switch {
				1 => Language.GetTextValue("LegacyInterface.28"),
				2 => this.GetLocalizedValue("Buttons.Shop2"),
				3 => this.GetLocalizedValue("Buttons.Annihilation"),
				_ => this.GetLocalizedValue("Buttons.Help"),
			};
			button = Text;
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
					case 1:
					case 2:
						shop = ShopName[ButtonCount - 1];
						break;
					case 3:
						AO();
						break;
				}
				return;
			}
			else {
				ButtonCount++;
				ButtonCount %= 4;
			}
		}

		public void AO() {
			var System = Main.LocalPlayer.GetModPlayer<AOSystem>();
			// AOStatus: false=not have a quest, true=doing quest
			// QuestType: 0:pre/unfin 1:pre/fin (2:HM/unfin 3:HM/fin)
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
					//Main.npcChatText = Language.GetTextValue("Mods.ArknightsMod.Dialogue.Closure.AOFin");
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

		// Not completely finished, but below is what the NPC will sell
		public override void AddShops() {
			var npcShop = new NPCShop(Type, ShopName[0])
				.Add(new Item(ModContent.ItemType<Polyketon>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Oriron>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Sugar>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Device>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Polyester>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<ManganeseOre>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Grindstone>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<LoxicKohl>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<CoagulatingGel>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<IncandescentAlloy>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<CrystallineComponent>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<CompoundCuttingFluid>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<SemiSyntheticSolvent>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<TransmutedSalt>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<CarbonBrick>()) {
					shopCustomPrice = Item.buyPrice(0, 0, 10, 0),
				})
				.Add(new Item(ModContent.ItemType<Items.Placeable.Furniture.DareUsa>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				});
			npcShop.Register();
			npcShop = new NPCShop(Type, ShopName[1])
				.Add(new Item(ModContent.ItemType<AmiyaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<MelanthaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<MatoimaruDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<IndigoDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<ChenDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<WDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<MudrockDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<OblivionisDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<RaidianDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<WisdelDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<BagpipeDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<FiammettaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<BeagleDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<CivilightEternaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<DorothyDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<ExusiaiDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<FartoothDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<HazeDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<KaltsitDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<KroosAlterDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<LaPlumaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<LingDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<ManticoreDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<MelaniteDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<MostimaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<RosmontisDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<SariaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<SkadiDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<SurtrDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<UtageDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<WarfarinDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<LapplandDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<TexalterDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				});
			npcShop.Register(); // Name of this shop tab
		}

		public override void OnSpawn(IEntitySource source) {
			NPCShopSystem.UpdateClosureShop(Mod, true);
		}

		public override void ModifyActiveShop(string shopName, Item[] items) {
			if (shopName == new NPCShop(Type, ShopName[1]).FullName) {
				if (NPCShopSystem.ClosureTodaysRotation.Count == 0)
					NPCShopSystem.UpdateClosureShop(Mod, true);
				Array.Fill(items, null);

				items[0] = new Item(ModContent.ItemType<DoctorArchiveBag>()) {
					shopCustomPrice = 100,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				};

				Item[] todayItems = [.. NPCShopSystem.ClosureTodaysRotation.Select(i => new Item(i) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})];
				for (int i = 1; i < items.Length && (i - 1) < todayItems.Length; i++) {
					items[i] = todayItems[i - 1]?.Clone();
				}
			}
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
}
