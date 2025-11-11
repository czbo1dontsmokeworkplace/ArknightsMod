using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter.Armor
{
	internal class KkdyAlterSetPlayer: ArknightsArmorPlayer
	{
		public bool KkdyAlterSetActive;
		public bool TalantActive= Main.rand.NextBool(5);
		public override void ResetEffects() {
			KkdyAlterSetActive = false;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {

			if (KkdyAlterSetActive) {
				if (TalantActive) {
					damage += 0.2f; // 武器基础伤害 +20%
				}
			}
		}
		public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
			if(KkdyAlterSetActive) {
				if (TalantActive) {

					int buffType = BuffID.Confused; // 迷惑效果
					int buffDuration = 12; // 持续时间0.2s
						target.AddBuff(buffType, buffDuration);
					
				}
			}
		}
	}
}
