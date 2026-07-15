using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Plume
{
	internal class PlumeSetPlayer : ArknightsArmorPlayer
	{
		public bool PlumeHelmetActive;
		public bool PlumeSetActive;

		public override void ResetEffects() {
			PlumeHelmetActive = false;
			PlumeSetActive = false;
		}

		public override void PostUpdateEquips() {
			PlumeHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<PlumeHead>());
			PlumeSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<PlumeHead>(),
				ModContent.ItemType<PlumeBody>(),
				ModContent.ItemType<PlumeLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, PlumeSetActive, "Mods.ArknightsMod.ArmorSets.Plume.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (PlumeHelmetActive && item.DamageType.CountsAsClass(DamageClass.Melee))
				damage *= 1.03f;
		}

		public override float UseSpeedMultiplier(Item item) {
			if (PlumeSetActive && item.DamageType.CountsAsClass(DamageClass.Melee))
				return 1.1f;

			return 1f;
		}

		public override void PostUpdate() {
			if (!PlumeSetActive)
				return;

			Player.moveSpeed += 0.1f;
			Player.maxRunSpeed += 0.1f;
			Player.accRunSpeed += 0.1f;
		}
	}
}
