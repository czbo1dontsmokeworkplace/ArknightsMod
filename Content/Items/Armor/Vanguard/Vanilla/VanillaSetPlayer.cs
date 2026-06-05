using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Vanilla
{
	internal class VanillaSetPlayer : ArknightsArmorPlayer
	{
		public bool VanillaHelmetActive;
		public bool VanillaSetActive;

		public override void ResetEffects() {
			VanillaHelmetActive = false;
			VanillaSetActive = false;
		}

		public override void PostUpdateEquips() {
			VanillaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<VanillaHelmet>());
			VanillaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<VanillaHelmet>(),
				ModContent.ItemType<VanillaChestplate>(),
				ModContent.ItemType<VanillaGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, VanillaSetActive, "Mods.ArknightsMod.ArmorSets.Vanilla.SetBonus");

			if (VanillaSetActive && Player.GetModPlayer<OperatorDeployCostPlayer>().DeployCost > 50)
				Player.statDefense += 8;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (VanillaHelmetActive && item.DamageType.CountsAsClass(DamageClass.Melee))
				damage *= 1.03f;
		}
	}
}
