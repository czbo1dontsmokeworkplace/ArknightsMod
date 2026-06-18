using ArknightsMod.Content.Items.Armor.Caster.Amiya;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.NPCs.Friendly;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Systems
{
	public class NPCShopSystem : ModSystem
	{
		public static List<int> ClosureTodaysRotation = [];
		// 淇濆瓨瀹屾暣鐨?Item 瀵硅薄鑰屼笉鏄彧淇濆瓨 type
		public static List<Item> CannotShopItems = [];
		public static int OldCannotShopCount;

		public override void Load() {
			On_Main.UpdateTime_StartDay += On_Main_UpdateTime_StartDay;
		}

		private void On_Main_UpdateTime_StartDay(On_Main.orig_UpdateTime_StartDay orig, ref bool stopEvents) {
			orig(ref stopEvents);
			UpdateClosureShop(Mod);
		}

		public static void UpdateClosureShop(Mod mod, bool firstTime = false) {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				int amiyaType = ModContent.ItemType<AmiyaDefault>();
				var sixStars = new List<int>();
				var others = new List<int>();

				foreach (var bag in ModContent.GetContent<ArknightsVanityBag>()) {
					if (bag.Type == amiyaType) continue;
					if (bag.Rarity == 6)
						sixStars.Add(bag.Type);
					else
						others.Add(bag.Type);
				}

				ClosureTodaysRotation = [amiyaType];
				int rand = Main.rand.Next(3, 6);
				while (sixStars.Count > rand) {
					sixStars.RemoveAt(Main.rand.Next(sixStars.Count));
				}
				ClosureTodaysRotation.AddRange(sixStars);
				rand = Main.rand.Next(3, 6);
				while (others.Count > rand) {
					others.RemoveAt(Main.rand.Next(others.Count));
				}
				ClosureTodaysRotation.AddRange(others);
				if (Main.dedServ) {
					SendUpdateClosureShop(mod);
				}
				else if (!firstTime && Main.LocalPlayer.talkNPC > -1) {
					var closure = Main.npc[Main.LocalPlayer.talkNPC];
					if (closure.type == ModContent.NPCType<Closure>())
						Main.playerInventory = false;
				}
			}
			else
				RequestUpdateClosureShopWhenStartDay(mod);
			if (!firstTime)
				Main.NewText(Language.GetTextValue("Mods.ArknightsMod.StatusMessage.UpdateClosureShop"), Color.Yellow);
		}

		public static void TryUpdateCannotShop(Mod mod, bool forcedUpdate = false) {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				int countBeforeSkeletron = 1 +
					(NPC.downedSlimeKing ? 1 : 0) +//鍙茶幈濮?
					(NPC.downedBoss1 ? 1 : 0) +//鍏嬬溂
					(NPC.downedBoss2 ? 1 : 0) +//閭伓boss
					(NPC.downedDeerclops ? 1 : 0);//宸ㄩ箍


				int countBetweenSkeletronAndPlantera =
					(NPC.downedBoss3 ? 1 : 0) +//楠烽珔鐜?
					(NPC.downedQueenBee ? 1 : 0) +//铚傚悗
					(Main.hardMode ? 1 : 0) +//鑲夊北
					(NPC.downedMechBoss1 ? 1 : 0) +//鏈烘1
					(NPC.downedMechBoss2 ? 1 : 0) +//鏈烘2
					(NPC.downedMechBoss3 ? 1 : 0);//鏈烘3

				int countBetweenPlanteraAndDukeFishron =
					(NPC.downedPlantBoss ? 1 : 0) +//涓栬姳
					(NPC.downedGolemBoss ? 1 : 0);//鐭冲法浜?

				int countFromFishronOnward =
					(NPC.downedFishron ? 1 : 0) +//鐚波
					(NPC.downedEmpressOfLight ? 1 : 0) +//鍏夊コ
					(NPC.downedAncientCultist ? 1 : 0) +//鏁欏緬
					(NPC.downedMoonlord ? 1 : 0);//鏈堟€?

				int cannotShopCount = countBeforeSkeletron + countBetweenSkeletronAndPlantera + countBetweenPlanteraAndDukeFishron + countFromFishronOnward;
				if (!forcedUpdate && cannotShopCount == OldCannotShopCount)
					return;

				var tempShop = new CannotShop();
				if (countBeforeSkeletron > 0)
					tempShop.AddPoolFromNameSpace("Rogue.Rarity_l1", countBeforeSkeletron, "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l1", mod);
				if (countBetweenSkeletronAndPlantera > 0)
					tempShop.AddPoolFromNameSpace("Rogue.Rarity_l2", countBetweenSkeletronAndPlantera, "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l2", mod);
				if (countBetweenPlanteraAndDukeFishron > 0)
					tempShop.AddPoolFromNameSpace("Rogue.Rarity_l3", countBetweenPlanteraAndDukeFishron, "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l3", mod);
				if (countFromFishronOnward > 0)
					tempShop.AddPoolFromNameSpace("Rogue.Rarity_l4", countFromFishronOnward, "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l4", mod);

				// 淇濆瓨瀹屾暣鐨?Item 瀵硅薄
				CannotShopItems.Clear();
				CannotShopItems.AddRange(tempShop.GenerateNewInventoryList());

				if (Main.dedServ)
					SendUpdateCannotShop(mod);

				OldCannotShopCount = cannotShopCount;
			}
			else
				RequestUpdateCannotShop(mod, forcedUpdate);
		}

		public static void RequestUpdateClosureShopWhenStartDay(Mod mod) {
			var packet = mod.GetPacket();
			packet.Write((short)ArknightsMod.ArkMessageID.RequestUpdateClosureShopWhenStartDay);
			packet.Send(255);
		}

		public static void RequestUpdateCannotShop(Mod mod, bool forcedUpdate) {
			var packet = mod.GetPacket();
			packet.Write((short)ArknightsMod.ArkMessageID.RequestUpdateCannotShop);
			packet.Write(forcedUpdate);
			packet.Send(255);
		}

		public static void SendUpdateClosureShop(Mod mod) {
			var packet = mod.GetPacket();
			packet.Write((short)ArknightsMod.ArkMessageID.UpdateClosureShopWhenStartDay);
			packet.Write(ClosureTodaysRotation.Count);
			for (int i = 0; i < ClosureTodaysRotation.Count; i++) {
				packet.Write(ClosureTodaysRotation[i]);
			}
			packet.Send();
		}

		// 淇敼锛氬悓姝ヨ嚜瀹氫箟璐у竵淇℃伅
		public static void SendUpdateCannotShop(Mod mod) {
			var packet = mod.GetPacket();
			packet.Write((short)ArknightsMod.ArkMessageID.UpdateCannotShop);
			packet.Write(CannotShopItems.Count);
			for (int i = 0; i < CannotShopItems.Count; i++) {
				packet.Write(CannotShopItems[i].type);
				packet.Write(CannotShopItems[i].shopSpecialCurrency);
			}
			packet.Send();
		}

		public static void ReadUpdateClosureShop(BinaryReader reader) {
			ClosureTodaysRotation = [];
			try {
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					ClosureTodaysRotation.Add(reader.ReadInt32());
				}
			}
			catch {
				ClosureTodaysRotation = [ModContent.ItemType<AmiyaDefault>()];
			}
		}

		// 璇诲彇骞舵仮澶嶈嚜瀹氫箟璐у竵淇℃伅
		public static void ReadUpdateCannotShop(BinaryReader reader) {
			CannotShopItems = [];
			try {
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					int type = reader.ReadInt32();
					int currency = reader.ReadInt32();
					var item = new Item(type) {
						shopSpecialCurrency = currency
					};
					CannotShopItems.Add(item);
				}
			}
			catch {
				CannotShopItems = [];
			}
		}
	}
}