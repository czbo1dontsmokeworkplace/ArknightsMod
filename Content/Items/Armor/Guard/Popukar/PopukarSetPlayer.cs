using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Popukar
{
	internal class PopukarSetPlayer : ArknightsArmorPlayer
	{
		public bool PopukarHelmetActive;
		public bool PopukarSetActive;

		public override void ResetEffects() {
			PopukarHelmetActive = false;
			PopukarSetActive = false;
		}

		public override void PostUpdateEquips() {
			PopukarHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<PopukarHelmet>());
			PopukarSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<PopukarHelmet>(),
				ModContent.ItemType<PopukarChestplate>(),
				ModContent.ItemType<PopukarGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, PopukarSetActive, "Mods.ArknightsMod.ArmorSets.Popukar.SetBonus");
		}

		public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
			health = StatModifier.Default;
			mana = StatModifier.Default;

			if (PopukarSetActive)
				health.Base += 30;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (PopukarHelmetActive && item.DamageType.CountsAsClass(DamageClass.Melee))
				damage *= 1.03f;
		}
	}
}
