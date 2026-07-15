using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Amiya
{
	internal class AmiyaSetPlayer : ArknightsArmorPlayer
	{
		public bool AmiyaHelmetActive;
		public bool AmiyaSetActive;

		public override void ResetEffects() {
			AmiyaHelmetActive = false;
			AmiyaSetActive = false;
		}

		public override void PostUpdateEquips() {
			AmiyaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<AmiyaHead>());
			AmiyaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<AmiyaHead>(),
				ModContent.ItemType<AmiyaBody>(),
				ModContent.ItemType<AmiyaLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, AmiyaSetActive, "Mods.ArknightsMod.ArmorSets.Amiya.SetBonus");
		}

		public override void ModifyManaCost(Item item, ref float reduce, ref float mult) {
			if (AmiyaHelmetActive)
				mult *= 0.8f;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryGainSpOnMagicHit(item, target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryGainSpOnMagicHit(proj, target, damageDone);
		}

		private void TryGainSpOnMagicHit(Item item, NPC target, int damageDone) {
			if (!AmiyaSetActive || damageDone <= 0 || !item.DamageType.CountsAsClass(DamageClass.Magic))
				return;

			int gain = target.boss ? 4 : 2;
			OperatorSPHelper.TryGainSP(Player, gain);

			if (target.life <= 0)
				OperatorSPHelper.TryGainSP(Player, 8);
		}

		private void TryGainSpOnMagicHit(Projectile proj, NPC target, int damageDone) {
			if (!AmiyaSetActive || damageDone <= 0 || !proj.DamageType.CountsAsClass(DamageClass.Magic))
				return;

			int gain = target.boss ? 4 : 2;
			OperatorSPHelper.TryGainSP(Player, gain);

			if (target.life <= 0)
				OperatorSPHelper.TryGainSP(Player, 8);
		}
	}
}
