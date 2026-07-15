using ArknightsMod.Common.GlobalNPCs;
using ArknightsMod.Content.Items.Armor;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.Radian
{
	internal class RaidianSetPlayer : ArknightsArmorPlayer
	{
		public bool RaidianHelmetActive;
		public bool RaidianSetActive;

		public int MinionBonusHealth;

		public override void ResetEffects() {
			RaidianHelmetActive = false;
			RaidianSetActive = false;
			MinionBonusHealth = 0;
		}

		public override void PostUpdateEquips() {
			RaidianHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<RaidianHead>());
			RaidianSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<RaidianHead>(),
				ModContent.ItemType<RaidianBody>(),
				ModContent.ItemType<RaidianLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, RaidianSetActive, "Mods.ArknightsMod.ArmorSets.Raidian.SetBonus");

			int slotBonus = 0;
			if (RaidianHelmetActive)
				slotBonus++;
			if (RaidianSetActive)
				slotBonus++;

			Player.maxMinions += slotBonus;

			if (RaidianHelmetActive)
				MinionBonusHealth = (int)(Player.statLifeMax2 * 0.2f);
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryMark(item, target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryMark(proj, target, damageDone);
		}

		private void TryMark(Item item, NPC target, int damageDone) {
			if (!RaidianSetActive || damageDone <= 0 || !item.DamageType.CountsAsClass(DamageClass.Summon))
				return;

			MarkTarget(target);
		}

		private void TryMark(Projectile proj, NPC target, int damageDone) {
			if (!RaidianSetActive || damageDone <= 0 || !proj.DamageType.CountsAsClass(DamageClass.Summon))
				return;

			MarkTarget(target);
		}

		private static void MarkTarget(NPC target) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (target.friendly || target.lifeMax <= 5)
				return;

			target.GetGlobalNPC<RaidianMarkGlobalNPC>().RaidianMarked = true;
		}
	}
}
