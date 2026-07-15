using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Melanite
{
	internal class MelaniteSetPlayer : ArknightsArmorPlayer
	{
		public bool MelaniteHelmetActive;
		public bool MelaniteSetActive;

		private bool firstSkillBonusPending;
		private bool spawnGranted;
		private bool lastBossActive;
		private bool skillBonusActive;

		public override void ResetEffects() {
			MelaniteHelmetActive = false;
			MelaniteSetActive = false;
		}

		public override void PostUpdateEquips() {
			MelaniteHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<MelaniteHead>());
			MelaniteSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<MelaniteHead>(),
				ModContent.ItemType<MelaniteBody>(),
				ModContent.ItemType<MelaniteLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, MelaniteSetActive, "Mods.ArknightsMod.ArmorSets.Melanite.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (MelaniteHelmetActive && item.DamageType.CountsAsClass(DamageClass.Ranged))
				damage *= 1.2f;

			if (skillBonusActive && item.DamageType.CountsAsClass(DamageClass.Ranged))
				damage *= 1.2f;
		}

		public override void PostUpdate() {
			if (!MelaniteSetActive) {
				spawnGranted = false;
				lastBossActive = false;
				skillBonusActive = false;
				return;
			}

			bool bossActive = OperatorSetBossHelper.AnyBossActive();
			if (!spawnGranted || (bossActive && !lastBossActive)) {
				firstSkillBonusPending = true;
				spawnGranted = true;
			}

			lastBossActive = bossActive;

			WeaponPlayer wp = Player.GetModPlayer<WeaponPlayer>();
			if (firstSkillBonusPending && wp.SkillActive) {
				firstSkillBonusPending = false;
				skillBonusActive = true;
			}

			if (!wp.SkillActive)
				skillBonusActive = false;

			if (Player.dead) {
				spawnGranted = false;
				lastBossActive = false;
				firstSkillBonusPending = false;
				skillBonusActive = false;
			}
		}

		public override void OnRespawn() {
			if (MelaniteSetActive)
				firstSkillBonusPending = true;
		}
	}
}
