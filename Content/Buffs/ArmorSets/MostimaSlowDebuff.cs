using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.ArmorSets
{
	public class MostimaSlowDebuff : ModBuff
	{
		public const int DurationTicks = 2 * 60;
		public const float SlowRatio = 0.15f;

		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
		}
	}

	public class MostimaSlowGlobalNPC : GlobalNPC
	{
		public override void PostAI(NPC npc) {
			if (!npc.HasBuff<MostimaSlowDebuff>())
				return;

			// 抵消本帧15%的速度位移，保留AI与攻击计时；不反复衰减存储的速度。
			npc.position -= npc.velocity * MostimaSlowDebuff.SlowRatio;
		}
	}
}
