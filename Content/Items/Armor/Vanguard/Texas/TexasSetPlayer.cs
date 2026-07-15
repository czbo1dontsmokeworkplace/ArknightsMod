using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Texas
{
	internal class TexasSetPlayer : ArknightsArmorPlayer
	{
		public bool TexasHelmetActive;
		public bool TexasSetActive;

		private bool deployBonusGranted;

		public override void ResetEffects() {
			TexasHelmetActive = false;
			TexasSetActive = false;
		}

		public override void PostUpdateEquips() {
			TexasHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<TexasHead>());
			TexasSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<TexasHead>(),
				ModContent.ItemType<TexasBody>(),
				ModContent.ItemType<TexasLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, TexasSetActive, "Mods.ArknightsMod.ArmorSets.Texas.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (!TexasSetActive)
				return;

			if (Player.GetModPlayer<WeaponPlayer>().SkillActive)
				damage *= OperatorSetSkillEffectHelper.GetSkillDamageMultiplier(Player);
		}

		public override void PostUpdate() {
			if (!TexasHelmetActive || Player.dead)
				return;

			if (!deployBonusGranted) {
				Player.GetModPlayer<OperatorDeployCostPlayer>().DeployCost += 2;
				deployBonusGranted = true;
			}
		}

		public override void OnRespawn() {
			if (TexasHelmetActive)
				Player.GetModPlayer<OperatorDeployCostPlayer>().DeployCost += 2;
		}

		public override void UpdateDead() {
			deployBonusGranted = false;
		}
	}
}
