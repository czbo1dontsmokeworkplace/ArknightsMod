using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Matoimaru.Armor
{
	internal class MatoimaruSetPlayer:ArknightsArmorPlayer
	{
		public bool MatoimaruSetActive;
		public override void ResetEffects() {
			MatoimaruSetActive = false;
		}
		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (MatoimaruSetActive) {
				if(item.DamageType==DamageClass.Melee) {
					damage *= 1.5f;
				}
			}
		}
		public override void PostUpdateEquips() {
			if (MatoimaruSetActive) {
				Player.statLifeMax2 += (int)(Player.statLifeMax2*0.2f);
				Player.statDefense *= 0.8f;
				
			}
		}
	}
}
