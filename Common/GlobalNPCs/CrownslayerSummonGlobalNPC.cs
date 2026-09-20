using System.IO;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Common.GlobalNPCs
{
	// 弑君者召唤的援军标记。
	// 不再借用 NPC.ai[3]：HoundPro 自己的 AI 把该槽当 0~300 的循环计时器，
	// 而弩手队长（aiStyle = 0）会让原版 AI_003_Fighters() 覆写它（该函数有 62 处 ai[3] 读写），
	// 两种情况都会让原先的 999f 标记失效。此标记与 NPC 自身的 ai[] 完全无关。
	public class CrownslayerSummonGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		public bool IsCrownslayerSummon;

		public static bool IsSummon(NPC npc) =>
			npc != null && npc.active && npc.GetGlobalNPC<CrownslayerSummonGlobalNPC>().IsCrownslayerSummon;

		public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter binaryWriter) {
			bitWriter.WriteBit(IsCrownslayerSummon);
		}

		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader binaryReader) {
			IsCrownslayerSummon = bitReader.ReadBit();
		}
	}
}
