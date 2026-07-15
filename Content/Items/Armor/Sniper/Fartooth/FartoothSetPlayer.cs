using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Players;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Fartooth
{
	internal class FartoothSetPlayer : ArknightsArmorPlayer
	{
		public bool FartoothHelmetActive;
		public bool FartoothSetActive;

		private int noDamageTimer;
		private bool bossRangedBoostActive;

		public override void ResetEffects() {
			FartoothHelmetActive = false;
			FartoothSetActive = false;
		}

		public override void PostUpdateEquips() {
			FartoothHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<FartoothHead>());
			FartoothSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<FartoothHead>(),
				ModContent.ItemType<FartoothBody>(),
				ModContent.ItemType<FartoothLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, FartoothSetActive, "Mods.ArknightsMod.ArmorSets.Fartooth.SetBonus");
		}

		private bool SkillActive => Player.GetModPlayer<WeaponPlayer>().SkillActive;

		public override void PostUpdate() {
			if (FartoothHelmetActive && SkillActive)
				Player.aggro -= 750;

			if (FartoothSetActive) {
				noDamageTimer++;
				if (noDamageTimer >= 10 * 60)
					noDamageTimer = 10 * 60;

				if (OperatorSetBossHelper.AnyBossActive())
					bossRangedBoostActive = true;
				else
					bossRangedBoostActive = false;
			}
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (FartoothHelmetActive && SkillActive)
				modifiers.ScalingArmorPenetration += 1f;

			ApplySetRangedBonus(item, ref modifiers);
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (FartoothHelmetActive && SkillActive)
				modifiers.ScalingArmorPenetration += 1f;

			ApplySetRangedBonus(proj, ref modifiers);
		}

		private void ApplySetRangedBonus(Item item, ref NPC.HitModifiers modifiers) {
			if (!FartoothSetActive || !item.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			if (noDamageTimer >= 10 * 60)
				modifiers.SourceDamage *= 1.2f;
			else if (bossRangedBoostActive)
				modifiers.SourceDamage *= 1.3f;
		}

		private void ApplySetRangedBonus(Projectile proj, ref NPC.HitModifiers modifiers) {
			if (!FartoothSetActive || !proj.DamageType.CountsAsClass(DamageClass.Ranged))
				return;

			if (noDamageTimer >= 10 * 60)
				modifiers.SourceDamage *= 1.2f;
			else if (bossRangedBoostActive)
				modifiers.SourceDamage *= 1.3f;
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!FartoothSetActive)
				return;

			modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
				noDamageTimer = 0;
				bossRangedBoostActive = false;
			};
		}
	}
}
