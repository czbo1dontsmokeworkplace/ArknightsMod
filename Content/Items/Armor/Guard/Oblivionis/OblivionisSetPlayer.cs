using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Oblivionis
{
	internal class OblivionisSetPlayer : ArknightsArmorPlayer
	{
		public bool OblivionisHelmetActive;
		public bool OblivionisSetActive;

		public override void ResetEffects() {
			OblivionisHelmetActive = false;
			OblivionisSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 OblivionisHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			OblivionisHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<OblivionisHead>();
			OblivionisSetActive = OblivionisHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<OblivionisBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<OblivionisLegs>();
		}

		public static bool IsAnyOblivionisSetActive() {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (player.active && !player.dead && player.GetModPlayer<OblivionisSetPlayer>().OblivionisSetActive)
					return true;
			}

			return false;
		}
	}

	internal class OblivionisAllyAttackSpeedPlayer : ModPlayer
	{
		private void ApplyArmorAttackSpeed() {
			if (OblivionisSetPlayer.IsAnyOblivionisSetActive())
				Player.GetAttackSpeed(DamageClass.Generic) *= 1.15f;
		}

		public override void PostUpdateMiscEffects() {
			ApplyArmorAttackSpeed();
		}
	}
}
