using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Systems
{
	public class DownedBossSystem : ModSystem
	{
		public static bool DownedPompeii;
		public static bool DownedTheFirstToTalk;
		public static bool DownedFrostNova;
		public static bool DownedAACT;
		public static bool DownedEvolution;
		public static bool DownedW;
		public static bool DownedFaustAndMephisto;

		public override void OnWorldLoad()
		{
			DownedFaustAndMephisto = false;
			DownedPompeii = false;
			DownedTheFirstToTalk = false;
			DownedFrostNova = false;
			DownedAACT = false;
			DownedEvolution = false;
			DownedW = false;
		}

		public override void OnWorldUnload()
		{
			DownedFaustAndMephisto = false;
			DownedPompeii = false;
			DownedTheFirstToTalk = false;
			DownedFrostNova = false;
			DownedAACT = false;
			DownedEvolution = false;
			DownedW = false;
		}

		public override void SaveWorldData(TagCompound tag)
		{
			if (DownedFaustAndMephisto)
				tag["ArknightsMod.DownedFaustAndMephisto"] = true;
			if (DownedPompeii)
				tag["ArknightsMod.DownedPompeii"] = true;
			if (DownedTheFirstToTalk)
				tag["ArknightsMod.DownedTheFirstToTalk"] = true;
			if (DownedFrostNova)
				tag["ArknightsMod.DownedFrostNova"] = true;
			if (DownedAACT)
				tag["ArknightsMod.DownedAACT"] = true;
			if (DownedEvolution)
				tag["ArknightsMod.DownedEvolution"] = true;
			if (DownedW)
				tag["ArknightsMod.DownedW"] = true;
		}

		public override void LoadWorldData(TagCompound tag)
		{
			DownedFaustAndMephisto = tag.ContainsKey("ArknightsMod.DownedFaustAndMephisto");
			DownedPompeii = tag.ContainsKey("ArknightsMod.DownedPompeii");
			DownedTheFirstToTalk = tag.ContainsKey("ArknightsMod.DownedTheFirstToTalk");
			DownedFrostNova = tag.ContainsKey("ArknightsMod.DownedFrostNova");
			DownedAACT = tag.ContainsKey("ArknightsMod.DownedAACT");
			DownedEvolution = tag.ContainsKey("ArknightsMod.DownedEvolution");
			DownedW = tag.ContainsKey("ArknightsMod.DownedW");
		}

		public static void MarkDowned(ref bool flag)
		{
			if (flag)
				return;

			flag = true;
			if (Main.netMode == NetmodeID.Server)
				NetMessage.SendData(MessageID.WorldData);
		}

		public override void NetSend(BinaryWriter writer)
		{
			BitsByte flags = new BitsByte(DownedPompeii, DownedTheFirstToTalk, DownedFrostNova, DownedAACT, DownedEvolution, DownedW, DownedFaustAndMephisto);
			writer.Write(flags);
		}

		public override void NetReceive(BinaryReader reader)
		{
			BitsByte flags = reader.ReadByte();
			DownedPompeii = flags[0]; DownedTheFirstToTalk = flags[1]; DownedFrostNova = flags[2];
			DownedAACT = flags[3]; DownedEvolution = flags[4]; DownedW = flags[5];
			DownedFaustAndMephisto = flags[6];
		}
	}
}
