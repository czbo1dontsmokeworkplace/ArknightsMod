using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.ArmorSets
{
	public class OrchidSlowDebuff : ModBuff
	{
		public const int DurationTicks = 48;
		public const float SlowRatio = 0.8f;

		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
		}
	}

	public class OrchidSlowGlobalNPC : GlobalNPC
	{
		public override void PostAI(NPC npc) {
			if (npc.boss || !npc.HasBuff<OrchidSlowDebuff>())
				return;

			// 抵消本帧80%的速度位移，保留AI和攻击计时，避免逐帧衰减速度。
			npc.position -= npc.velocity * OrchidSlowDebuff.SlowRatio;
		}
	}
}
