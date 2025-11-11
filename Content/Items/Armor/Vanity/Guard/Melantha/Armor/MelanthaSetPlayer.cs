using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

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
			damage += 0.04f;
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			
		}
	}
}
