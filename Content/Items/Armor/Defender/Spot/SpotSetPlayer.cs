using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Spot
{
	internal class SpotSetPlayer : ArknightsArmorPlayer
	{
		public bool SpotHelmetActive;
		public bool SpotSetActive;

		public override void ResetEffects() {
			SpotHelmetActive = false;
			SpotSetActive = false;
		}

		public override void PostUpdateEquips() {
			SpotHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<SpotHelmet>());
			SpotSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<SpotHelmet>(),
				ModContent.ItemType<SpotChestplate>(),
				ModContent.ItemType<SpotGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, SpotSetActive, "Mods.ArknightsMod.ArmorSets.Spot.SetBonus");
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!Player.HasBuff<SpotHealDodgeBuff>())
				return;

			if (Main.rand.NextFloat() < 0.17f)
				modifiers.Cancel();
		}
	}
}
