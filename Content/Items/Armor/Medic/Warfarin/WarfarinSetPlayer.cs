using System;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Medic.Warfarin
{
	internal class WarfarinSetPlayer : ArknightsArmorPlayer
	{
		public bool WarfarinHelmetActive;
		public bool WarfarinSetActive;

		public override void ResetEffects() {
			WarfarinHelmetActive = false;
			WarfarinSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 WarfarinHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			WarfarinHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<WarfarinHead>();
			WarfarinSetActive = WarfarinHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<WarfarinBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<WarfarinLegs>();
		}

		public override void UpdateLifeRegen() {
			if (!Player.active || Player.dead || Player.statLife >= Player.statLifeMax2 * .5d)
				return;

			// Reevaluate the world-wide aura on the recipient every tick. No buff,
			// duration, cooldown, distance, team or PvP condition is involved.
			int helmetType = NeoArmorReforgeSetLoader.GetSetType<WarfarinHead>();
			foreach (Player source in Main.ActivePlayers) {
				if (source.dead || source.armor[0].type <= 0 || source.armor[0].type != helmetType)
					continue;
				Player.lifeRegen += 20; // lifeRegen units: 20 = 10 HP per second.
				break;
			}
		}

		public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) {
			TryKillSpBonus(target, damageDone);
		}

		public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) {
			TryKillSpBonus(target, damageDone);
		}

		private void TryKillSpBonus(NPC target, int damageDone) {
			if (!WarfarinSetActive || damageDone <= 0)
				return;

			if (target.friendly || target.lifeMax <= 5 || target.life > 0)
				return;

			if (Vector2.Distance(Player.Center, target.Center) > 800f)
				return;

			OperatorSPHelper.TryGainSP(Player, 2);

			Player ally = FindRandomNearbyAlly();
			if (ally != null)
				OperatorSPHelper.TryGainSP(ally, 2);
		}

		private Player FindRandomNearbyAlly() {
			const float maxRange = 800f;
			Player chosen = null;
			int count = 0;

			for (int i = 0; i < Main.maxPlayers; i++) {
				Player other = Main.player[i];
				if (!other.active || other.dead || other.whoAmI == Player.whoAmI)
					continue;

				if (Vector2.Distance(Player.Center, other.Center) > maxRange)
					continue;

				count++;
				if (Main.rand.Next(count) == 0)
					chosen = other;
			}

			return chosen;
		}
	}
}
