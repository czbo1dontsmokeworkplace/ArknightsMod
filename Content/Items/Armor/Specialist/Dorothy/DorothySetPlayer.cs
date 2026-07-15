using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Projectiles.Specialist.Dorothy;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Dorothy
{
	internal class DorothySetPlayer : ArknightsArmorPlayer
	{
		public bool DorothyHelmetActive;
		public bool DorothySetActive;

		public int UnusedSlotStacks;

		public override void ResetEffects() {
			DorothyHelmetActive = false;
			DorothySetActive = false;
		}

		public override void PostUpdateEquips() {
			DorothyHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<DorothyHead>());
			DorothySetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<DorothyHead>(),
				ModContent.ItemType<DorothyBody>(),
				ModContent.ItemType<DorothyLegs>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, DorothySetActive, "Mods.ArknightsMod.ArmorSets.Dorothy.SetBonus");

			if (DorothyHelmetActive)
				Player.maxMinions++;

			if (DorothySetActive)
				Player.maxMinions++;
		}

		public override void ModifyWeaponDamage(Item item, ref StatModifier damage) {
			if (!item.DamageType.CountsAsClass(DamageClass.Summon))
				return;

			if (DorothyHelmetActive)
				damage *= 1.15f;

			if (DorothySetActive && UnusedSlotStacks > 0)
				damage *= 1f + 0.03f * UnusedSlotStacks;
		}

		public override void PostUpdate() {
			if (DorothySetActive) {
				int unused = OperatorMinionSlotHelper.CountUnusedMinionSlots(Player);
				UnusedSlotStacks = System.Math.Min(10, unused);
				TrySpawnSweeper();
			}

			if (Player.dead)
				UnusedSlotStacks = 0;
		}

		public override void OnRespawn() {
			UnusedSlotStacks = 0;
		}

		private void TrySpawnSweeper() {
			if (Main.netMode == NetmodeID.MultiplayerClient || Player.dead)
				return;

			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (proj.active && proj.owner == Player.whoAmI && proj.type == ModContent.ProjectileType<DorothySweeper>())
					return;
			}

			Projectile.NewProjectile(
				Player.GetSource_FromThis(),
				Player.Center,
				Vector2.Zero,
				ModContent.ProjectileType<DorothySweeper>(),
				(int)Player.GetTotalDamage(DamageClass.Summon).ApplyTo(12f),
				2f,
				Player.whoAmI);
		}
	}
}
