using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Catapult
{
	internal class CatapultSetPlayer : ArknightsArmorPlayer
	{
		public bool CatapultHelmetActive;
		public bool CatapultSetActive;

		private bool spawnSpGranted;

		public override void ResetEffects() {
			CatapultHelmetActive = false;
			CatapultSetActive = false;
		}

		public override void PostUpdateEquips() {
			CatapultHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<CatapultHead>());
			CatapultSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<CatapultHead>(),
				ModContent.ItemType<CatapultBody>(),
				ModContent.ItemType<CatapultLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, CatapultSetActive, "Mods.ArknightsMod.ArmorSets.Catapult.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (CatapultSetActive && item.DamageType.CountsAsClass(DamageClass.Ranged))
				damage *= 1.03f;
		}

		public override void PostUpdate() {
			if (!CatapultSetActive)
				return;

			if (!Player.dead && !spawnSpGranted) {
				OperatorSPHelper.TryGainSP(Player, 5);
				spawnSpGranted = true;
			}

			if (Player.dead)
				spawnSpGranted = false;
		}

		public override void OnRespawn() {
			if (CatapultSetActive)
				OperatorSPHelper.TryGainSP(Player, 5);
		}
	}
}
