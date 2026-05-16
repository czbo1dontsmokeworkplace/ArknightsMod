using ArknightsMod.Content.Items.Weapons.Sniper.KroosAlter;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter.Armor
{
	internal class KkdyAlterSetPlayer: ArknightsArmorPlayer
	{
		public bool KkdyAlterSetActive;
		public bool TalantActive;
		public override void ResetEffects() {
			TalantActive = Main.rand.NextBool(5);
			KkdyAlterSetActive = false;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if(KkdyAlterSetActive) {
				if(item.type==ModContent.ItemType<KroosAlterCrossbow>()) {
					damage *= 2f;
				}
				else if(item.DamageType==DamageClass.Ranged) {
					damage *= 1.5f;
				}
				if (TalantActive && Player.itemAnimation > 0) {
					damage *= 1.5f;
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
