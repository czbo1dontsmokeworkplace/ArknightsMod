using System;
using ArknightsMod.Content.Projectiles.Medic.Shining;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic.Shining
{
	public sealed class ShiningStaffPlayer : ModPlayer
	{
		public int ShieldHp { get; private set; }
		public int ShieldMax { get; private set; }
		public int ShieldTime { get; private set; }
		public float ShieldHitFlash { get; private set; }

		public float ShieldRatio => ShieldMax > 0
			? MathHelper.Clamp(ShieldHp / (float)ShieldMax, 0f, 1f)
			: 0f;

		public void GrantShield(float maxLifeRatio, int duration)
		{
			if (Player.whoAmI != Main.myPlayer || !Player.active || Player.dead)
				return;

			int capacity = Math.Max(1, (int)Math.Round(Player.statLifeMax2 * maxLifeRatio));
			// 重新套盾只取更高的剩余量，不把两个护盾相加，防止叠层。
			ShieldMax = Math.Max(ShieldMax, capacity);
			ShieldHp = Math.Max(ShieldHp, capacity);
			ShieldTime = Math.Max(ShieldTime, duration);
			ShieldHitFlash = 0.35f;
			EnsureShieldProjectile();
			ShiningVisuals.SpawnBarrierParticles(Player.MountedCenter, true, 16);
		}

		public override void PostUpdate()
		{
			ShieldHitFlash *= 0.86f;

			if (ShieldHp <= 0 || ShieldTime <= 0)
			{
				ShieldHp = 0;
				ShieldMax = 0;
				ShieldTime = 0;
				return;
			}

			ShieldTime--;
			EnsureShieldProjectile();
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers)
		{
			if (Player.whoAmI != Main.myPlayer || ShieldHp <= 0 || ShieldTime <= 0)
				return;

			modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) =>
			{
				if (info.Damage <= 0 || ShieldHp <= 0)
					return;

				int absorbed = Math.Min(ShieldHp, info.Damage);
				ShieldHp -= absorbed;
				info.Damage = Math.Max(0, info.Damage - absorbed);
				ShieldHitFlash = 1f;
				ShiningVisuals.SpawnBarrierHit(Player.MountedCenter, ShieldHp <= 0);

				if (!Main.dedServ)
					SoundEngine.PlaySound(SoundID.NPCHit53 with
					{
						Volume = 0.48f,
						Pitch = ShieldHp <= 0 ? -0.12f : 0.28f
					}, Player.Center);

				if (ShieldHp <= 0)
					ShieldTime = 0;
			};
		}

		public void TryHealFromAttack(Vector2 sourcePosition, int attackDamage)
		{
			// 命中只在持有者一侧生成治疗术体；真正的生命回复在术体抵达玩家后结算。
			if (Player.whoAmI != Main.myPlayer || !Player.active || Player.dead
				|| attackDamage <= 0 || Player.statLife >= Player.statLifeMax2)
				return;

			// 闪灵以护盾为主；每次返回术体请求回复2点生命，抵达时由服务器共享治疗节流。
			int amount = 2;
			amount = Math.Min(amount, Player.statLifeMax2 - Player.statLife);

			Vector2 toPlayer = (Player.MountedCenter - sourcePosition).SafeNormalize(Vector2.UnitY);
			float openingTurn = Main.rand.NextBool() ? 0.58f : -0.58f;
			Vector2 initialVelocity = toPlayer.RotatedBy(openingTurn) * 4.2f;
			Projectile.NewProjectile(Player.GetSource_Misc("ShiningAttackHeal"), sourcePosition,
				initialVelocity, ModContent.ProjectileType<ShiningHealingWispProjectile>(),
				0, 0f, Player.whoAmI, amount);
		}

		public override void UpdateDead()
		{
			ShieldHp = 0;
			ShieldMax = 0;
			ShieldTime = 0;
			ShieldHitFlash = 0f;
		}

		private void EnsureShieldProjectile()
		{
			if (Player.whoAmI != Main.myPlayer || ShieldHp <= 0 || ShieldTime <= 0)
				return;
			int type = ModContent.ProjectileType<ShiningBarrierProjectile>();
			if (Player.ownedProjectileCounts[type] <= 0)
				Projectile.NewProjectile(Player.GetSource_Misc("ShiningBarrier"), Player.MountedCenter,
					Vector2.Zero, type, 0, 0f, Player.whoAmI, ShieldHp, ShieldMax, ShieldHitFlash);
		}
	}
}
