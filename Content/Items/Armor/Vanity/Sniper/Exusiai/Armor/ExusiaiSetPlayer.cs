
using ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Exusiai.Armor
{
	public class ExusiaiSetPlayer:ArknightsArmorPlayer
	{
		public bool ExusiaiSetActive;
		int extraUseSpeed = 12;

		public override void ResetEffects() {
			ExusiaiSetActive = false;
		}
		public override float UseSpeedMultiplier(Item item) {
			if (ExusiaiSetActive) {
				return (100 + extraUseSpeed) / 100f;
			}
			return 1f;
		}
		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (ExusiaiSetActive) {
				damage*=1.06f;

				if (item.type == ModContent.ItemType<ExusiaiVector>()) {
					damage *= 2f;
				}
				else if (item.DamageType == DamageClass.Ranged) {
					damage *= 1.5f;
				}
			}
		}
		public override void PostUpdate() {
			Player.statLifeMax2 = (int)(Player.statLifeMax2 * 1.1f);
		}
	}
}
