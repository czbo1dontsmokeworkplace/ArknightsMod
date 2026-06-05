using ArknightsMod.Content.Buffs.ArmorSets;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalNPCs
{
	public class OperatorSetDebuffGlobalNPC : GlobalNPC
	{
		public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) {
			if (npc.HasBuff(ModContent.BuffType<HazeMagicFragileDebuff>())
				&& modifiers.DamageType.CountsAsClass(DamageClass.Magic)) {
				modifiers.SourceDamage *= 1.25f;
			}
		}

		public override void AI(NPC npc) {
			if (npc.HasBuff(ModContent.BuffType<IndigoBindDebuff>()))
				npc.velocity = Vector2.Zero;

			if (npc.HasBuff(ModContent.BuffType<MostimaSlowDebuff>()))
				npc.velocity *= 0.85f;

			if (npc.HasBuff(ModContent.BuffType<OrchidSlowDebuff>()))
				npc.velocity *= 0.2f;

			if (npc.HasBuff(ModContent.BuffType<TyphonHelmetSlowDebuff>()))
				npc.velocity *= 0.2f;

			if (npc.HasBuff(ModContent.BuffType<LapplandSilenceDebuff>())
				&& (npc.aiStyle == NPCAIStyleID.Caster || npc.aiStyle == NPCAIStyleID.Spell))
				npc.ai[0] = 0f;
		}

		public override void UpdateLifeRegen(NPC npc, ref int damage) {
			if (npc.HasBuff(ModContent.BuffType<EntelechiaBleedDebuff>())) {
				if (npc.lifeRegen > 0)
					npc.lifeRegen = 0;

				npc.lifeRegen -= EntelechiaBleedDebuff.DamagePerSecond;
				if (damage < EntelechiaBleedDebuff.DamagePerSecond)
					damage = EntelechiaBleedDebuff.DamagePerSecond;
			}
		}
	}
}
