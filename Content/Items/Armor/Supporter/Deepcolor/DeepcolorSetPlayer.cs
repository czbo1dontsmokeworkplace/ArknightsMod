using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Deepcolor
{
	internal class DeepcolorSetPlayer : ArknightsArmorPlayer
	{
		public bool DeepcolorHelmetActive;
		public bool DeepcolorSetActive;

		public override void ResetEffects() {
			DeepcolorHelmetActive = false;
			DeepcolorSetActive = false;
		}

		public override void PostUpdateEquips() {
			DeepcolorSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<DeepcolorHead>(),
				ModContent.ItemType<DeepcolorBody>(),
				ModContent.ItemType<DeepcolorLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, DeepcolorSetActive, "Mods.ArknightsMod.ArmorSets.Deepcolor.SetBonus");
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!DeepcolorHelmetActive)
				return;

			if (Main.rand.NextFloat() < 0.07f)
				modifiers.Cancel();
		}
	}
}
