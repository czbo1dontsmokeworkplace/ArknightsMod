using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mostima
{
	internal class MostimaSetPlayer : ArknightsArmorPlayer
	{
		public bool MostimaHelmetActive;
		public bool MostimaSetActive;

		public override void ResetEffects() {
			MostimaHelmetActive = false;
			MostimaSetActive = false;
		}

		public override void PostUpdateEquips() {
			MostimaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<MostimaHead>());
			MostimaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<MostimaHead>(),
				ModContent.ItemType<MostimaBody>(),
				ModContent.ItemType<MostimaLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, MostimaSetActive, "Mods.ArknightsMod.ArmorSets.Mostima.SetBonus");

			if (ShouldReceiveMostimaSpRegen(Player))
				Player.GetModPlayer<WeaponPlayer>().SPRegenMultiplier += 0.4f; // +0.4 技力/秒
		}

		public static bool ShouldReceiveMostimaSpRegen(Player player) {
			MostimaSetPlayer self = player.GetModPlayer<MostimaSetPlayer>();
			if (self.MostimaSetActive)
				return true;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player other = Main.player[i];
				if (!other.active || other.dead || other.whoAmI == player.whoAmI)
					continue;

				MostimaSetPlayer otherSet = other.GetModPlayer<MostimaSetPlayer>();
				if (otherSet.MostimaSetActive
					&& OperatorTeammateHelper.HasTeammates(other)
					&& OperatorCasterSetHelper.WearsFullCasterSet(player)) {
					return true;
				}
			}

			return false;
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryApplySlow(item, target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryApplySlow(proj, target, damageDone);
		}

		private void TryApplySlow(Item item, NPC target, int damageDone) {
			if (!MostimaHelmetActive || damageDone <= 0)
				return;

			ApplySlow(target);
		}

		private void TryApplySlow(Projectile proj, NPC target, int damageDone) {
			if (!MostimaHelmetActive || damageDone <= 0)
				return;

			ApplySlow(target);
		}

		private static void ApplySlow(NPC target) {
			if (Main.netMode == NetmodeID.MultiplayerClient)
				return;

			if (target.friendly || target.lifeMax <= 5 || target.dontTakeDamage)
				return;

			target.AddBuff(ModContent.BuffType<MostimaSlowDebuff>(), 120);
		}
	}
}
