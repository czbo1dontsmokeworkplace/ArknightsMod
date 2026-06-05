using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Haze
{
	internal class HazeSetPlayer : ArknightsArmorPlayer
	{
		public bool HazeHelmetActive;
		public bool HazeSetActive;

		public override void ResetEffects() {
			HazeHelmetActive = false;
			HazeSetActive = false;
		}

		public override void PostUpdateEquips() {
			HazeHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<HazeHelmet>());
			HazeSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<HazeHelmet>(),
				ModContent.ItemType<HazeChestplate>(),
				ModContent.ItemType<HazeGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, HazeSetActive, "Mods.ArknightsMod.ArmorSets.Haze.SetBonus");

			if (HazeSetActive) {
				int critBonus = !Main.dayTime ? 12 : 6;
				Player.GetCritChance(DamageClass.Magic) += critBonus;
			}
		}

		public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
			health = StatModifier.Default;
			mana = StatModifier.Default;

			if (HazeSetActive)
				mana.Base += !Main.dayTime ? 100 : 50;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryApplyFragile(item, target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryApplyFragile(proj, target, damageDone);
		}

		private void TryApplyFragile(Item item, NPC target, int damageDone) {
			if (!HazeHelmetActive || damageDone <= 0 || !item.DamageType.CountsAsClass(DamageClass.Magic))
				return;

			ApplyFragile(target);
		}

		private void TryApplyFragile(Projectile proj, NPC target, int damageDone) {
			if (!HazeHelmetActive || damageDone <= 0 || !proj.DamageType.CountsAsClass(DamageClass.Magic))
				return;

			ApplyFragile(target);
		}

		private static void ApplyFragile(NPC target) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (target.friendly || target.lifeMax <= 5 || target.dontTakeDamage)
				return;

			target.AddBuff(ModContent.BuffType<HazeMagicFragileDebuff>(), 60);
		}
	}
}
