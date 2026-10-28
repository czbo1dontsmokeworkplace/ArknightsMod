using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Projectiles.Sniper.Rosmontis;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
	internal class RosmontisSetPlayer : ArknightsArmorPlayer
	{
		public bool RosmontisHelmetActive;
		public bool RosmontisSetActive;

		private int tacticalCooldown;

		public override void ResetEffects() {
			RosmontisHelmetActive = false;
			RosmontisSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本也交给 RosmontisHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍
		// （旧代码这里和 RosmontisHead.UpdateArmorSet 各设置一次，两个不同的文本
		// 谁执行在后谁生效，是一个没被注意到的 bug）。
		public override void PostUpdateEquips() {
			RosmontisHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<RosmontisHead>();
			RosmontisSetActive = RosmontisHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<RosmontisBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<RosmontisLegs>();
			if (RosmontisHelmetActive)
				Player.GetArmorPenetration(DamageClass.Ranged) += 25;
		}

		public override void PostUpdate() {
			if (tacticalCooldown > 0)
				tacticalCooldown--;

		}

		internal void TryDeployTacticalEquipment() {
			if (tacticalCooldown > 0 || Player.dead)
				return;

			Vector2 destination = Main.MouseWorld;
			Vector2 spawn = new(destination.X, System.Math.Min(Main.screenPosition.Y, destination.Y) - 128f);
			int index = Projectile.NewProjectile(
				Player.GetSource_FromThis(),
				spawn,
				Vector2.Zero,
				ModContent.ProjectileType<RosmontisTacticalEquipment>(),
				0,
				8f,
				Player.whoAmI);
			if (index < 0 || index >= Main.maxProjectiles)
				return;
			((RosmontisTacticalEquipment)Main.projectile[index].ModProjectile).BeginDeployment(destination);

			tacticalCooldown = 60 * 60;
		}
	}

	internal class RosmontisCasterBuffPlayer : ModPlayer
	{
		public static bool IsRosmontisSetOnField() {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				// 直接检查当前装备，避免依赖其他玩家本帧是否已更新套装状态。
				if (player.active && !player.dead
					&& player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<RosmontisHead>()
					&& OperatorSniperSetHelper.WearsFullSniperSet(player))
					return true;
			}

			return false;
		}

		private void ApplyArmorDamageStats() {
			if (!Player.active || Player.dead || !IsRosmontisSetOnField())
				return;

			if (OperatorCasterSetHelper.WearsFullCasterSet(Player)
				|| OperatorSniperSetHelper.WearsFullSniperSet(Player))
				Player.GetDamage(DamageClass.Generic) += 0.08f;
		}

		public override void PostUpdateMiscEffects() {
			ApplyArmorDamageStats();
		}
	}
}
