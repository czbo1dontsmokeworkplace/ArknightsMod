using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fiammetta
{
	internal class FiammettaSetPlayer : ArknightsArmorPlayer
	{
		public bool FiammettaHelmetActive;
		public bool FiammettaSetActive;

		private int drainTimer;

		public override void ResetEffects() {
			FiammettaHelmetActive = false;
			FiammettaSetActive = false;
		}

		public override void PostUpdateEquips() {
			FiammettaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<FiammettaHead>());
			FiammettaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<FiammettaHead>(),
				ModContent.ItemType<FiammettaBody>(),
				ModContent.ItemType<FiammettaLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, FiammettaSetActive, "Mods.ArknightsMod.ArmorSets.Fiammetta.SetBonus");
		}

		public override float UseSpeedMultiplier(Item item) {
			if (FiammettaHelmetActive
				&& !Player.GetModPlayer<WeaponPlayer>().SkillActive
				&& item.DamageType.CountsAsClass(DamageClass.Ranged)) {
				return 1.27f;
			}

			return 1f;
		}

		public override void PostUpdate() {
			if (!FiammettaSetActive || Player.dead)
				return;

			drainTimer++;
			if (drainTimer >= 60 && Player.statLife > 1) {
				Player.statLife--;
				drainTimer = 0;
			}
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (!FiammettaSetActive || !item.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			float lifeRatio = Player.statLife / (float)Player.statLifeMax2;
			if (lifeRatio > 0.8f)
				damage *= 1.5f;
			else if (lifeRatio > 0.5f)
				damage *= 1.25f;
		}
	}
}
