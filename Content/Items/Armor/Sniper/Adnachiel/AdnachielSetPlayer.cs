using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Adnachiel
{
	internal class AdnachielSetPlayer : ArknightsArmorPlayer
	{
		public bool AdnachielHelmetActive;
		public bool AdnachielSetActive;

		public override void ResetEffects() {
			AdnachielHelmetActive = false;
			AdnachielSetActive = false;
		}

		public override void PostUpdateEquips() {
			AdnachielHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<AdnachielHelmet>());
			AdnachielSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<AdnachielHelmet>(),
				ModContent.ItemType<AdnachielChestplate>(),
				ModContent.ItemType<AdnachielGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, AdnachielSetActive, "Mods.ArknightsMod.ArmorSets.Adnachiel.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (AdnachielHelmetActive && item.DamageType.CountsAsClass(DamageClass.Ranged))
				damage *= 1.04f;
		}
	}
}
