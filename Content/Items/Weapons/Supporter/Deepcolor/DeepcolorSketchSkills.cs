using System;
using ArknightsMod.Content.Projectiles.Supporter.Deepcolor;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Deepcolor
{
	public static class DeepcolorSketchSkills
	{
		public const float ShadowTentacleDamageMult = 1.06f;
		public const float ShadowTentacleDefenseMult = 1.06f;
		public const int ShadowTentacleRegenPerSecond = 7;
		// 二技能额外攻击半径（格）
		public const float VisualTrapRangeBonusTiles = 5f;
		public const float VisualTrapDodgeChance = 0.5f;
		public const float VisualTrapDrawScale = 1.5f;

		public static bool IsHoldingDeepcolorSketch(Player player)
			=> player.HeldItem.ModItem is DeepcolorSketch;

		public static bool IsSkillActive(Player player, int skillIndex) {
			if (!IsHoldingDeepcolorSketch(player))
				return false;
			var wp = player.GetModPlayer<WeaponPlayer>();
			return wp.Skill == skillIndex && wp.SkillActive;
		}

		public static bool ShadowTentacleActive(Player player) => IsSkillActive(player, 0);
		public static bool VisualTrapActive(Player player) => IsSkillActive(player, 1);

		public static float GetAttackRadiusPx(Player owner) {
			float radius = DeepcolorMinion.BaseAttackRangeRadiusPx;
			if (VisualTrapActive(owner))
				radius += VisualTrapRangeBonusTiles * 16f;
			return radius;
		}

		// 以触手中心为圆心、向左经上方到右的半圆弧（不索敌正下方）
		public static bool IsInAttackRangeAt(Projectile tentacle, Vector2 targetCenter, Player owner) {
			Vector2 offset = targetCenter - tentacle.Center;
			float dist = offset.Length();
			float radius = GetAttackRadiusPx(owner);

			if (dist > radius)
				return false;

			if (dist <= 4f)
				return offset.Y <= 2f;

			// 不攻击脚下及下方
			if (offset.Y > 2f)
				return false;

			float angle = (float)Math.Atan2(offset.Y, offset.X);
			return angle <= 0f && angle >= -(float)Math.PI;
		}

		public static bool IsOwnerInAnyTentacleAttackRange(Player owner) {
			if (!VisualTrapActive(owner))
				return false;

			int type = ModContent.ProjectileType<DeepcolorMinion>();
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (!proj.active || proj.owner != owner.whoAmI || proj.type != type)
					continue;

				if (IsInAttackRangeAt(proj, owner.Center, owner))
					return true;
			}

			return false;
		}
	}
}
