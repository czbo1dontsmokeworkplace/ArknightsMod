using System;
using ArknightsMod.Systems.Gameplay.Damage;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.ArmorSets
{
	public class EntelechiaBleedDebuff : ModBuff
	{
		public const int DamagePerSecond = 40;
		public const int DurationTicks = 5 * 60;

		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.pvpBuff[Type] = true;
		}
	}

	public sealed class EntelechiaBleedNPC : GlobalNPC
	{
		public override void UpdateLifeRegen(NPC npc, ref int damage) {
			if (!npc.HasBuff<EntelechiaBleedDebuff>() || npc.friendly || npc.dontTakeDamage) return;
			float resistance = Math.Clamp(npc.GetGlobalNPC<DamageCategoryNPC>().GetEffectiveArtsResistance(npc), 0f, 1f);
			// NPC regeneration bypasses defense. Two regen units equal one HP/s;
			// apply effective arts resistance here because this is not an item/projectile hit.
			int loss = (int)MathF.Round(EntelechiaBleedDebuff.DamagePerSecond * 2 * (1f - resistance));
			if (loss <= 0) return;
			npc.lifeRegen = Math.Min(0, npc.lifeRegen) - loss;
			damage = Math.Max(damage, 1);
		}
	}
}
