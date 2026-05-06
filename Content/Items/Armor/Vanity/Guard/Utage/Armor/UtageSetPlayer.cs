using System.Security.Policy;
using Terraria;
using Terraria.ModLoader;
using System;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Utage.Armor
{
	internal class UtageSetPlayer : ArknightsArmorPlayer {
		int healamount = 10;
		float MaxExtraSpeed = 75f;
		public bool UtageSetActive;

		public override void ResetEffects() {
			UtageSetActive = false;
		}

		public override float UseSpeedMultiplier(Item item) {
			float result = 1f;
			if (UtageSetActive) {
				float liferatio = Player.statLife / (float)Player.statLifeMax2;
				if (liferatio <= 0.6) {
					result = (100 + MaxExtraSpeed) / 100;
				}
				else if (liferatio <= 1) {
					result = (100 + MaxExtraSpeed * ((1f - liferatio) / 0.4f)) / 100;
				}

			}
			return result;
		}
		#region 禁疗效果
		// 禁止自然回血
		public override void NaturalLifeRegen(ref float regen) {
			if (UtageSetActive) {
				regen = 0f;
			}
		}
		public override void UpdateLifeRegen() {
			if (!UtageSetActive)
				return;

			if (Player.lifeRegen > 0)
				Player.lifeRegen = 0;

			if (Player.lifeRegenCount > 0)
				Player.lifeRegenCount = 0;
		}
		// 禁止药水、食物、治疗类物品的回血
		public override void GetHealLife(Item item, bool quickHeal, ref int healValue) {
			if (UtageSetActive) {
				healValue = 0;
			}
		}

		// 禁止护士治疗
		public override bool ModifyNurseHeal(NPC nurse, ref int health, ref bool removeDebuffs, ref string chatText) {
			if (UtageSetActive) {
				health = 0;
				removeDebuffs = false;
				return false;
			}

			return true;
		}
		#endregion
		//public override void OnHitAnything(float x, float y, Entity victim) {
		//	if (UtageSetActive) {
		//		int realHeal = Math.Min(healamount, Player.statLifeMax2 - Player.statLife);

		//		Player.Heal(10);
		//	}
		//}
		#region 攻击回血
		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			if (!item.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			TryUtageHeal(target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			if (!proj.DamageType.CountsAsClass(DamageClass.Melee))
				return;

			TryUtageHeal(target, damageDone);
		}

		private void TryUtageHeal(NPC target, int damageDone) {
			if (!UtageSetActive)
				return;

			if (damageDone <= 0)
				return;

			if (target.friendly || target.lifeMax <= 5 || target.dontTakeDamage || target.immortal)
				return;

			int realHeal = Math.Min(healamount, Player.statLifeMax2 - Player.statLife);

			if (realHeal <= 0)
				return;

			Player.Heal(realHeal);
		}
		#endregion
		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (item.DamageType == DamageClass.Melee) {
				damage*= 1.5f;
			}
			//后面是专属武器加成
			//else if{ }
		}

	}
}
