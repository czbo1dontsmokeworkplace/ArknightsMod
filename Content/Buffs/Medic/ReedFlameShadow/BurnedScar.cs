using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Buffs.Medic.ReedFlameShadow
{
	public sealed class BurnedScar : ModBuff
	{
		public override void SetStaticDefaults() {
			Main.debuff[Type] = true;
			Main.buffNoSave[Type] = true;
		}
	}

	public sealed class BurnedScarNPC : GlobalNPC
	{
		private static bool HasScar(NPC npc) => npc.HasBuff(ModContent.BuffType<BurnedScar>());

		// 敌人本体的碰撞攻击伤害降低 20%。
		public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers) {
			if (HasScar(npc))
				modifiers.FinalDamage *= 0.8f;
		}

		public override void ModifyHitNPC(NPC npc, NPC target, ref NPC.HitModifiers modifiers) {
			if (HasScar(npc))
				modifiers.FinalDamage *= 0.8f;
		}

		// 只增强魔法类型的物品与弹幕命中，不影响物理、召唤等其他类型。
		public override void ModifyHitByItem(NPC npc, Player player, Item item, ref NPC.HitModifiers modifiers) {
			if (HasScar(npc) && item.DamageType.CountsAsClass(DamageClass.Magic))
				modifiers.FinalDamage *= 1.38f;
		}

		public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers) {
			if (HasScar(npc) && projectile.DamageType.CountsAsClass(DamageClass.Magic))
				modifiers.FinalDamage *= 1.38f;
		}
	}

	// 敌人受灼痕影响时发出的攻击弹幕同样降低 20% 伤害。
	public sealed class BurnedScarProjectile : GlobalProjectile
	{
		public override void OnSpawn(Projectile projectile, IEntitySource source) {
			if (!projectile.hostile || projectile.damage <= 0
				|| source is not EntitySource_Parent { Entity: NPC npc }
				|| !npc.HasBuff(ModContent.BuffType<BurnedScar>()))
				return;

			projectile.damage = Math.Max(1, (int)MathF.Round(projectile.damage * 0.8f));
		}
	}
}
