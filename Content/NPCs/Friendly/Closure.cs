using System.Collections.Generic;
using Terraria;
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
			NPC.aiStyle = 7;
			NPC.damage = 90;
			NPC.defense = 15;
			NPC.lifeMax = 1000;
			NPC.HitSound = SoundID.NPCHit1;
			NPC.DeathSound = SoundID.NPCDeath1;
			NPC.knockBackResist = 0.5f;
			AnimationType = NPCID.Guide;
		}

		public override bool CanTownNPCSpawn(int numTownNPCs) {
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
				1 => this.GetLocalizedValue("Buttons.Shop2"),
				2 => this.GetLocalizedValue("Buttons.Annihilation"),
				_ => Language.GetTextValue("LegacyInterface.28"),
			};
			button = Text;
			button2 = this.GetLocalizedValue("Buttons.Switch");
		}

		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			if (firstButton) {
				switch (ButtonCount) {
					case 0:
					case 1:
						shop = ShopName[ButtonCount];
						break;
					case 2:
						AO();
						break;
				}
				return;
			}
			else {
				ButtonCount++;
				ButtonCount %= 3;
			}
		}

		public void AO() {
			var System = Main.player[Main.myPlayer].GetModPlayer<AOSystem>();
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
			public static List<Quest> Quests = new();
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
				get { return QuestNum; }
				set { QuestNum = value; }
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
				int reward = Item.NewItem(npc.GetSource_Loot(), Player.getRect(), ModContent.ItemType<Items.Orundum>(), 50);
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

		public class Quest
		{
			public string QuestMessage;
			public int ItemAmount;
			public int QuestItem;
			public string ThxMessage;
			public double Weight;

			public Quest(string questMessage, int itemID, int itemAmount, string thxMessage = null) {
				QuestMessage = questMessage;
				QuestItem = itemID;
				ItemAmount = itemAmount;
				ThxMessage = thxMessage;
			}

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
				.Add(new Item(ModContent.ItemType<Items.Material.Polyketon>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.Oriron>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.Sugar>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.Device>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.Polyester>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.ManganeseOre>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.Grindstone>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.LoxicKohl>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.CoagulatingGel>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.IncandescentAlloy>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.CrystallineComponent>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.CompoundCuttingFluid>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.SemiSyntheticSolvent>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.TransmutedSalt>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Placeable.Furniture.DareUsa>()) {
					shopCustomPrice = 30,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				});
			npcShop.Register();
			npcShop = new NPCShop(Type, ShopName[1])
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.AmiyaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.MelanthaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.MatoimaruDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.IndigoDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.ChenDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.WDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.MudrockDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.OblivionisDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.RaidianDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.WisdelDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.BagpipeDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.FiammettaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.BeagleDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.CivilightEternaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.DorothyDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.ExusiaiDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.FartoothDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.HazeDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.KaltsitDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.KroosAlterDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.LaPlumaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.LingDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.ManticoreDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.MelaniteDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.MostimaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.RosmontisDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.SariaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.SkadiDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.SurtrDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.UtageDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.WarfarinDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.LapplandDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.TexalterDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.AdnachielDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.AnselDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.CardiganDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType < Items.Consumables.VanityBags.CatapultDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.FangDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.HibiscusDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.KroosDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.LavaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType < Items.Consumables.VanityBags.MidnightDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.OrchidDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.PlumeDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.PopukarDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.SpotDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType <Items.Consumables.VanityBags.StewardDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Consumables.VanityBags.VanillaDefault>()) {
					shopCustomPrice = 10,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				});
			npcShop.Register(); // Name of this shop tab
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
