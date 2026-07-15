using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Manticore
{
	internal class ManticoreSetPlayer : ArknightsArmorPlayer
	{
		public bool ManticoreHelmetActive;
		public bool ManticoreSetActive;

		public bool Stealthed;
		public bool BreakStealthBonus;
		private int noAttackTimer;

		public override void ResetEffects() {
			ManticoreHelmetActive = false;
			ManticoreSetActive = false;
		}

		public override void PostUpdateEquips() {
			ManticoreHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<ManticoreHead>());
			ManticoreSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<ManticoreHead>(),
				ModContent.ItemType<ManticoreBody>(),
				ModContent.ItemType<ManticoreLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, ManticoreSetActive, "Mods.ArknightsMod.ArmorSets.Manticore.SetBonus");
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (ManticoreHelmetActive && Main.rand.NextFloat() < 0.1f)
				modifiers.Cancel();
		}

		public override void PostUpdate() {
			if (!ManticoreSetActive)
				return;

			bool attacking = Player.itemAnimation > 0;
			if (attacking) {
				if (Stealthed)
					BreakStealthBonus = true;

				Stealthed = false;
				noAttackTimer = 0;
			}
			else {
				noAttackTimer++;
				if (noAttackTimer >= 150)
					Stealthed = true;
			}

			if (Stealthed)
				Player.aggro -= 1200;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (ManticoreSetActive && BreakStealthBonus) {
				damage *= 1.5f;
				BreakStealthBonus = false;
			}
		}
	}
}
