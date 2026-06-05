using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Projectiles.Supporter.CivilightEterna;
using ArknightsMod.Systems.Gameplay.OperatorTags;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Supporter.CivilightEterna
{
	internal class CivilightEternaSetPlayer : ArknightsArmorPlayer
	{
		public bool CivilightEternaHelmetActive;
		public bool CivilightEternaSetActive;

		public override void ResetEffects() {
			CivilightEternaHelmetActive = false;
			CivilightEternaSetActive = false;
		}

		public override void PostUpdateEquips() {
			CivilightEternaHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<CivilightEternaHelmet>());
			CivilightEternaSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<CivilightEternaHelmet>(),
				ModContent.ItemType<CivilightEternaChestplate>(),
				ModContent.ItemType<CivilightEternaGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, CivilightEternaSetActive, "Mods.ArknightsMod.ArmorSets.CivilightEterna.SetBonus");
		}

		public override void PostUpdate() {
			if (CivilightEternaSetActive)
				TryMaintainDustMotes();
		}

		public static bool IsCivilightHelmetOnField() {
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player player = Main.player[i];
				if (player.active && !player.dead && player.GetModPlayer<CivilightEternaSetPlayer>().CivilightEternaHelmetActive)
					return true;
			}

			return false;
		}

		private void TryMaintainDustMotes() {
			if (Main.netMode == NetmodeID.MultiplayerClient || Player.dead)
				return;

			int active = 0;
			for (int i = 0; i < Main.maxProjectiles; i++) {
				Projectile proj = Main.projectile[i];
				if (proj.active && proj.owner == Player.whoAmI && proj.type == ModContent.ProjectileType<CivilightDustMote>())
					active++;
			}

			for (int slot = active; slot < 3; slot++) {
				Projectile.NewProjectile(
					Player.GetSource_FromThis(),
					Player.Center,
					Vector2.Zero,
					ModContent.ProjectileType<CivilightDustMote>(),
					0,
					0f,
					Player.whoAmI,
					slot);
			}
		}
	}

	internal class CivilightEternaAllyDefensePlayer : ModPlayer
	{
		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!CivilightEternaSetPlayer.IsCivilightHelmetOnField())
				return;

			modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
				if (info.DamageSource.TryGetCausingEntity(out var entity) && entity is NPC npc
					&& OperatorTagHelper.NpcHasFaction(npc, OperatorFaction.Sarkaz)) {
					info.Damage = (int)(info.Damage * 0.9f);
				}
			};
		}
	}
}
