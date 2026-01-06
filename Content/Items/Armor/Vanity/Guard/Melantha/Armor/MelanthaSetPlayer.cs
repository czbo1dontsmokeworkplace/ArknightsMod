using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Melantha.Armor
{
	internal class MelanthaSetPlayer : ArknightsArmorPlayer
	{
		public bool MelanthaSetActive;

		public override void ResetEffects() {
			MelanthaSetActive = false;
		}
		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			// 对所有武器增加 4% 伤害
			if (MelanthaSetActive) {
				damage += 4;
				//if(item.type==ModContent.ItemType<MelanthaSword>)
				if(item.DamageType == DamageClass.Melee) {
					damage *= 1.5f;
				}
			}
			
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			
		}
	}
}
