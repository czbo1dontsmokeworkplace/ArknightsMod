using ArknightsMod.Content.Items.Consumables.VanityBags;
using ArknightsMod.Content.NPCs.Friendly;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Systems
{
	public class NPCShopSystem : ModSystem
	{
		public static List<int> ClosureTodaysRotation = [];
		public static List<int> CannotShopItems = [];
		public static int OldCannotShopCount;

		public static readonly List<int> SixStars = [
			ModContent.ItemType<ChenDefault>(),
			ModContent.ItemType<WDefault>(),
			ModContent.ItemType<MudrockDefault>(),
			ModContent.ItemType<OblivionisDefault>(),
			ModContent.ItemType<RaidianDefault>(),
			ModContent.ItemType<WisdelDefault>(),
			ModContent.ItemType<BagpipeDefault>(),
			ModContent.ItemType<FiammettaDefault>(),
			ModContent.ItemType<CivilightEternaDefault>(),
			ModContent.ItemType<DorothyDefault>(),
			ModContent.ItemType<ExusiaiDefault>(),
			ModContent.ItemType<FartoothDefault>(),
			ModContent.ItemType<KaltsitDefault>(),
			ModContent.ItemType<LingDefault>(),
			ModContent.ItemType<MostimaDefault>(),
			ModContent.ItemType<RosmontisDefault>(),
			ModContent.ItemType<SariaDefault>(),
			ModContent.ItemType<SkadiDefault>(),
			ModContent.ItemType<SurtrDefault>(),
			ModContent.ItemType<TexalterDefault>(),
		];

		public static readonly List<int> Others = [
			ModContent.ItemType<MelanthaDefault>(),
			ModContent.ItemType<MatoimaruDefault>(),
			ModContent.ItemType<IndigoDefault>(),
			ModContent.ItemType<BeagleDefault>(),
			ModContent.ItemType<HazeDefault>(),
			ModContent.ItemType<KroosAlterDefault>(),
			ModContent.ItemType<LaPlumaDefault>(),
			ModContent.ItemType<ManticoreDefault>(),
			ModContent.ItemType<MelaniteDefault>(),
			ModContent.ItemType<UtageDefault>(),
			ModContent.ItemType<WarfarinDefault>(),
			ModContent.ItemType<LapplandDefault>(),
		];

		public override void Load() {
			On_Main.UpdateTime_StartDay += On_Main_UpdateTime_StartDay;
		}

		private void On_Main_UpdateTime_StartDay(On_Main.orig_UpdateTime_StartDay orig, ref bool stopEvents) {
			orig(ref stopEvents);
			UpdateClosureShop(Mod);
		}

		public static void UpdateClosureShop(Mod mod, bool firstTime = false) {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				ClosureTodaysRotation = [ModContent.ItemType<AmiyaDefault>()];
				int rand = Main.rand.Next(3, 6);
				List<int> sixStars = [.. SixStars];
				while (sixStars.Count > rand) {
					sixStars.RemoveAt(Main.rand.Next(sixStars.Count));
				}
				ClosureTodaysRotation.AddRange(sixStars);
				rand = Main.rand.Next(3, 6);
				List<int> others = [.. Others];
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
			if (!firstTime)
				Main.NewText(Language.GetTextValue("Mods.ArknightsMod.StatusMessage.UpdateClosureShop"), Color.Yellow);
		}

		public static void TryUpdateCannotShop(Mod mod) {
			if (Main.netMode != NetmodeID.MultiplayerClient) {
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

				int cannotShopCount = countBeforeSkeletron + countBetweenSkeletronAndPlantera + countBetweenPlanteraAndDukeFishron + countFromFishronOnward;
				if (cannotShopCount == OldCannotShopCount)
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

				CannotShopItems.Clear();
				CannotShopItems.AddRange(tempShop.GenerateNewInventoryList().Select(i => i.type));

				if (Main.dedServ)
					SendUpdateCannotShop(mod);

				OldCannotShopCount = cannotShopCount;
			}
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

		public static void SendUpdateCannotShop(Mod mod) {
			var packet = mod.GetPacket();
			packet.Write((short)ArknightsMod.ArkMessageID.UpdateCannotShop);
			packet.Write(CannotShopItems.Count);
			for (int i = 0; i < CannotShopItems.Count; i++) {
				packet.Write(CannotShopItems[i]);
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

		public static void ReadUpdateCannotShop(BinaryReader reader) {
			CannotShopItems = [];
			try {
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					CannotShopItems.Add(reader.ReadInt32());
				}
			}
			catch {
				CannotShopItems = [];
			}
		}
	}
}
