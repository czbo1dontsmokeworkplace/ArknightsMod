using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Steward
{
	internal class StewardSetPlayer : ArknightsArmorPlayer
	{
		public bool StewardHelmetActive;
		public bool StewardSetActive;

		public override void ResetEffects() {
			StewardHelmetActive = false;
			StewardSetActive = false;
		}

		public override void PostUpdateEquips() {
			StewardHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<StewardHelmet>());
			StewardSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<StewardHelmet>(),
				ModContent.ItemType<StewardChestplate>(),
				ModContent.ItemType<StewardGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, StewardSetActive, "Mods.ArknightsMod.ArmorSets.Steward.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (StewardHelmetActive && item.DamageType.CountsAsClass(DamageClass.Magic))
				damage *= 1.04f;
		}
	}
}
