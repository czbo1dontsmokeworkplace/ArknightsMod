using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	internal class MorniaSetPlayer : ArknightsArmorPlayer
	{
		public bool MorniaHelmetActive;
		public bool MorniaSetActive;

		public override void ResetEffects() {
			MorniaHelmetActive = false;
			MorniaSetActive = false;
		}

		public override void PostUpdateEquips() {
			MorniaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<MorniaHelmet>());
			MorniaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<MorniaHelmet>(),
				ModContent.ItemType<MorniaChestplate>(),
				ModContent.ItemType<MorniaGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, MorniaSetActive, "Mods.ArknightsMod.ArmorSets.Mornia.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (MorniaHelmetActive && item.DamageType.CountsAsClass(DamageClass.Magic))
				damage *= 1.06f;
		}
	}
}
