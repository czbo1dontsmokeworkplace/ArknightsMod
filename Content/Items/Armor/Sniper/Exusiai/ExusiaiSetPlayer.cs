using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Exusiai
{
	internal class ExusiaiSetPlayer : ArknightsArmorPlayer
	{
		public bool ExusiaiHelmetActive;
		public bool ExusiaiSetActive;

		public override void ResetEffects() {
			ExusiaiHelmetActive = false;
			ExusiaiSetActive = false;
		}

		public override void PostUpdateEquips() {
			ExusiaiHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<ExusiaiHelmet>());
			ExusiaiSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<ExusiaiHelmet>(),
				ModContent.ItemType<ExusiaiChestplate>(),
				ModContent.ItemType<ExusiaiGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, ExusiaiSetActive, "Mods.ArknightsMod.ArmorSets.Exusiai.SetBonus");

		}

		public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
			health = StatModifier.Default;
			mana = StatModifier.Default;

			if (ShouldReceiveExusiaiBonus(Player))
				health *= 1.1f;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (ShouldReceiveExusiaiBonus(Player))
				damage *= 1.06f;
		}

		public static bool ShouldReceiveExusiaiBonus(Player player) {
			ExusiaiSetPlayer self = player.GetModPlayer<ExusiaiSetPlayer>();
			if (self.ExusiaiSetActive)
				return true;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player other = Main.player[i];
				if (!other.active || other.dead || other.whoAmI == player.whoAmI)
					continue;

				ExusiaiSetPlayer otherSet = other.GetModPlayer<ExusiaiSetPlayer>();
				if (otherSet.ExusiaiSetActive && OperatorTeammateHelper.HasTeammates(other))
					return true;
			}

			return false;
		}

		public override float UseSpeedMultiplier(Item item) {
			if (ExusiaiHelmetActive && item.DamageType.CountsAsClass(DamageClass.Ranged))
				return 1.2f;

			return 1f;
		}

	}
}
