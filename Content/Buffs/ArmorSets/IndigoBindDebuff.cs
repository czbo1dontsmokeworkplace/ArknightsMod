using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Buffs.ArmorSets
{
	public class IndigoBindDebuff : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
		}
	}

	public class IndigoBindGlobalNPC : GlobalNPC
	{
		public override bool InstancePerEntity => true;

		private bool bound;
		private Vector2 anchor;

		public override bool PreAI(NPC npc) {
			// 服务端决定束缚起止，并同步锚点；客户端不依赖本地 Buff 列表推断状态。
			if (Main.netMode != NetmodeID.MultiplayerClient) {
				bool hasBind = npc.HasBuff<IndigoBindDebuff>();
				if (hasBind != bound) {
					bound = hasBind;
					if (bound)
						anchor = npc.position;
					npc.netUpdate = true;
				}
			}

			// 不跳过 AI：攻击计时、瞄准和发射弹幕仍正常执行。
			return true;
		}

		public override void PostAI(NPC npc) {
			if (!bound)
				return;

			// 同时拦住速度移动与 AI 直接改位置（冲刺、瞬移等），不修改接触伤害。
			npc.position = anchor;
			npc.velocity = Vector2.Zero;
		}

		public override void SendExtraAI(NPC npc, BitWriter bitWriter, BinaryWriter writer) {
			writer.Write(bound);
			if (bound) {
				writer.Write(anchor.X);
				writer.Write(anchor.Y);
			}
		}

		public override void ReceiveExtraAI(NPC npc, BitReader bitReader, BinaryReader reader) {
			bound = reader.ReadBoolean();
			if (bound)
				anchor = new Vector2(reader.ReadSingle(), reader.ReadSingle());
		}
	}
}
