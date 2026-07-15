using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Orchid
{
	internal class OrchidSetPlayer : ArknightsArmorPlayer
	{
		public bool OrchidHelmetActive;
		public bool OrchidSetActive;

		public override void ResetEffects() {
			OrchidHelmetActive = false;
			OrchidSetActive = false;
		}

		public override void PostUpdateEquips() {
			OrchidHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<OrchidHead>());
			OrchidSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<OrchidHead>(),
				ModContent.ItemType<OrchidBody>(),
				ModContent.ItemType<OrchidLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, OrchidSetActive, "Mods.ArknightsMod.ArmorSets.Orchid.SetBonus");
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (OrchidHelmetActive && item.DamageType.CountsAsClass(DamageClass.Magic))
				damage *= 1.1f;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TrySlow(item, target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TrySlow(proj, target, damageDone);
		}

		private void TrySlow(Item item, NPC target, int damageDone) {
			if (!OrchidSetActive || damageDone <= 0 || !item.DamageType.CountsAsClass(DamageClass.Magic))
				return;

			ApplySlow(target);
		}

		private void TrySlow(Projectile proj, NPC target, int damageDone) {
			if (!OrchidSetActive || damageDone <= 0 || !proj.DamageType.CountsAsClass(DamageClass.Magic))
				return;

			ApplySlow(target);
		}

		private static void ApplySlow(NPC target) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (target.friendly || target.lifeMax <= 5 || target.dontTakeDamage || target.boss)
				return;

			target.AddBuff(ModContent.BuffType<OrchidSlowDebuff>(), 48);
		}
	}
}
