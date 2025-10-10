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
	public class Cannot : ModNPC
	{
		public const string ShopName = "Shop";

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
			return new List<string> { Language.GetTextValue($"Mods.ArknightsMod.NPCs.{GetType().Name}.DisplayName") };
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
			button = Language.GetTextValue("LegacyInterface.28");
		}

		public override void OnChatButtonClicked(bool firstButton, ref string shop) {
			if (firstButton) {
				shop = ShopName;
				return;
			}
		}
		public override void AddShops() {
			/*var npcShop = new NPCShop(Type, ShopName)
				.Add(new Item(ModContent.ItemType<Items.Material.Polyketon>()) {
					shopCustomPrice = 1,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				})
				.Add(new Item(ModContent.ItemType<Items.Material.Oriron>()) {
					shopCustomPrice = 1,
					shopSpecialCurrency = ArknightsMod.OrundumCurrencyId
				});
				
			npcShop.Register();*/
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
