using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Saria
{
	internal class SariaSetPlayer : ArknightsArmorPlayer
	{
		public bool SariaHelmetActive;
		public bool SariaSetActive;

		public int GuardStacks;
		private int stackTimer;

		public override void ResetEffects() {
			SariaHelmetActive = false;
			SariaSetActive = false;
		}

		public override void PostUpdateEquips() {
			SariaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<SariaHelmet>());
			SariaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<SariaHelmet>(),
				ModContent.ItemType<SariaChestplate>(),
				ModContent.ItemType<SariaGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, SariaSetActive, "Mods.ArknightsMod.ArmorSets.Saria.SetBonus");

			if (SariaSetActive && GuardStacks > 0) {
				Player.GetDamage(DamageClass.Generic) += 0.05f * GuardStacks;
				extraDefenseBonus += 0.04f * GuardStacks;
			}
		}

		public override void PostUpdate() {
			if (!SariaSetActive) {
				GuardStacks = 0;
				stackTimer = 0;
				return;
			}

			if (!OperatorSetBossHelper.AnyBossActive()) {
				GuardStacks = 0;
				stackTimer = 0;
				return;
			}

			stackTimer++;
			if (stackTimer >= 10 * 60 && GuardStacks < 5) {
				GuardStacks++;
				stackTimer = 0;
			}
		}

		public override void UpdateDead() {
			GuardStacks = 0;
			stackTimer = 0;
		}
	}
}
