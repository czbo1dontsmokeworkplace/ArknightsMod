using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Common.GlobalNPCs
{
	public class RaidianMarkGlobalNPC : GlobalNPC
	{
		public bool RaidianMarked;

		public override bool InstancePerEntity => true;

		public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) {
			bitWriter.WriteBit(RaidianMarked);
		}

		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) {
			RaidianMarked = bitReader.ReadBit();
		}

		public override void OnKill(NPC npc) {
			RaidianMarked = false;
		}
	}
}
