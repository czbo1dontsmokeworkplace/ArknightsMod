using ArknightsMod.Content.Items.Consumables.VanityBags;
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
	public class ClosureShopSystem : ModSystem
	{
		public static List<int> TodaysRotation = [];

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
				TodaysRotation = [ModContent.ItemType<AmiyaDefault>()];
				int rand = Main.rand.Next(3, 6);
				List<int> sixStars = [.. SixStars];
				while (sixStars.Count > rand) {
					sixStars.RemoveAt(Main.rand.Next(sixStars.Count));
				}
				TodaysRotation.AddRange(sixStars);
				rand = Main.rand.Next(3, 6);
				List<int> others = [.. Others];
				while (others.Count > rand) {
					others.RemoveAt(Main.rand.Next(others.Count));
				}
				TodaysRotation.AddRange(others);
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

		public static void SendUpdateClosureShop(Mod mod) {
			var packet = mod.GetPacket();
			packet.Write((short)ArknightsMod.ArkMessageID.UpdateClosureShopWhenStartDay);
			packet.Write(TodaysRotation.Count);
			for (int i = 0; i < TodaysRotation.Count; i++) {
				packet.Write(TodaysRotation[i]);
			}
			packet.Send();
		}

		public static void ReadUpdateClosureShop(BinaryReader reader) {
			TodaysRotation = [];
			try {
				int count = reader.ReadInt32();
				for (int i = 0; i < count; i++) {
					TodaysRotation.Add(reader.ReadInt32());
				}
			}
			catch {
				TodaysRotation = [ModContent.ItemType<AmiyaDefault>()];
			}
		}
	}
}
