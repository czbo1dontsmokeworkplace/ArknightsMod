using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.ArmorSets
{
	public class TyphonHelmetSlowDebuff : ModBuff
	{
		public const float SlowRatio = 0.8f;

		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
		}
	}

	public class TyphonHelmetSlowGlobalNPC : GlobalNPC
	{
		public override void PostAI(NPC npc) {
			if (!npc.HasBuff<TyphonHelmetSlowDebuff>())
				return;

			// 抵消本帧80%的速度位移，保留AI和攻击计时，避免逐帧乘速度导致趋近停滞。
			npc.position -= npc.velocity * TyphonHelmetSlowDebuff.SlowRatio;
		}
	}
}
