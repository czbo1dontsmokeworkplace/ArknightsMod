using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Fang
{
	internal class FangSetPlayer : ArknightsArmorPlayer
	{
		public bool FangHelmetActive;
		public bool FangSetActive;

		private bool spawnSpGranted;
		private int noDamageTimer;

		public override void ResetEffects() {
			FangHelmetActive = false;
			FangSetActive = false;
		}

		public override void PostUpdateEquips() {
			FangHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<FangHead>());
			FangSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<FangHead>(),
				ModContent.ItemType<FangBody>(),
				ModContent.ItemType<FangLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, FangSetActive, "Mods.ArknightsMod.ArmorSets.Fang.SetBonus");
		}

		public override void PostUpdate() {
			if (FangHelmetActive && !Player.dead && !spawnSpGranted) {
				OperatorSPHelper.TryGainSP(Player, 4);
				spawnSpGranted = true;
			}

			if (Player.dead)
				spawnSpGranted = false;

			if (!FangSetActive)
				return;

			noDamageTimer++;
			if (noDamageTimer >= 4 * 60) {
				OperatorSPHelper.TryGainSP(Player, 2);
				noDamageTimer = 0;
			}
		}

		public override void OnRespawn() {
			if (FangHelmetActive)
				OperatorSPHelper.TryGainSP(Player, 4);
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (FangSetActive)
				noDamageTimer = 0;
		}
	}
}
