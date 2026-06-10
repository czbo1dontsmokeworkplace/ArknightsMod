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

		public override void PostUpdateEquips() {
			RosmontisHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<RosmontisHelmet>());
			RosmontisSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<RosmontisHelmet>(),
				ModContent.ItemType<RosmontisChestplate>(),
				ModContent.ItemType<RosmontisGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, RosmontisSetActive, "Mods.ArknightsMod.ArmorSets.Rosmontis.SetBonus");
		}

		public override void PostUpdate() {
			if (tacticalCooldown > 0)
				tacticalCooldown--;

			if (RosmontisSetActive && ArknightsKeybinds.RosmontisTacticalDeploy.JustPressed)
				TryDeployTacticalEquipment();
		}

		public override void ModifyHitNPCWithItem(Item item, NPC target, ref NPC.HitModifiers modifiers) {
			if (RosmontisHelmetActive && item.DamageType.CountsAsClass(DamageClass.Ranged))
				modifiers.ArmorPenetration += 25;
		}

		public override void ModifyHitNPCWithProj(Projectile proj, NPC target, ref NPC.HitModifiers modifiers) {
			if (RosmontisHelmetActive && proj.DamageType.CountsAsClass(DamageClass.Ranged))
				modifiers.ArmorPenetration += 25;
		}

		private void TryDeployTacticalEquipment() {
			if (tacticalCooldown > 0 || Player.dead)
				return;

			Vector2 spawn = Main.MouseWorld;
			Projectile.NewProjectile(
				Player.GetSource_FromThis(),
				spawn,
				Vector2.Zero,
				ModContent.ProjectileType<RosmontisTacticalEquipment>(),
				0,
				8f,
				Player.whoAmI);

			tacticalCooldown = 60 * 60;
		}
	}

	internal class RosmontisCasterBuffPlayer : ModPlayer
	{
		public static bool IsRosmontisSetOnField() {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (player.active && !player.dead && player.GetModPlayer<RosmontisSetPlayer>().RosmontisSetActive)
					return true;
			}

			return false;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (!IsRosmontisSetOnField())
				return;

			if (OperatorCasterSetHelper.WearsFullCasterSet(Player))
				damage *= 1.08f;
		}
	}
}
