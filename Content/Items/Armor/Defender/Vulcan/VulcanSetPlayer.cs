using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Vulcan
{
	internal class VulcanSetPlayer : ArknightsArmorPlayer
	{
		public bool VulcanHelmetActive;
		public bool VulcanSetActive;

		public override void ResetEffects() {
			VulcanHelmetActive = false;
			VulcanSetActive = false;
		}

		public override void PostUpdateEquips() {
			VulcanHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<VulcanHead>());
			VulcanSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<VulcanHead>(),
				ModContent.ItemType<VulcanBody>(),
				ModContent.ItemType<VulcanLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, VulcanSetActive, "Mods.ArknightsMod.ArmorSets.Vulcan.SetBonus");
		}

		private bool SkillActive => Player.GetModPlayer<WeaponPlayer>().SkillActive;

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!VulcanHelmetActive || !SkillActive)
				return;

			if (Main.rand.NextFloat() >= 0.25f)
				return;

			modifiers.Cancel();
		}

		public override void UpdateLifeRegen() {
			if (VulcanSetActive && SkillActive)
				Player.lifeRegen += (int)(Player.statLifeMax2 * 0.02f);
		}
	}
}
