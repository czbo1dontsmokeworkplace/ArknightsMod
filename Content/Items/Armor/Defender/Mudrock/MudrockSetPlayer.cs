using System;
using ArknightsMod.Content.Buffs.ArmorSets;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Systems.Gameplay.OperatorTags;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	internal class MudrockSetPlayer : ArknightsArmorPlayer
	{
		public bool MudrockHelmetActive;
		public bool MudrockSetActive;

		public int ShieldLayers;
		private int shieldTimer;

		public override void ResetEffects() {
			MudrockHelmetActive = false;
			MudrockSetActive = false;
		}

		public override void PostUpdateEquips() {
			MudrockHelmetActive = OperatorSetEquipHelper.HasHelmet(Player, ModContent.ItemType<MudrockHelmet>());
			MudrockSetActive = OperatorSetEquipHelper.HasFullSet(
				Player,
				ModContent.ItemType<MudrockHelmet>(),
				ModContent.ItemType<MudrockChestplate>(),
				ModContent.ItemType<MudrockGreaves>());
			OperatorSetEquipHelper.ApplySetBonusText(Player, MudrockSetActive, "Mods.ArknightsMod.ArmorSets.Mudrock.SetBonus");
		}

		public override void PostUpdate() {
			if (!MudrockSetActive) {
				shieldTimer = 0;
				return;
			}

			if (Player.dead) {
				ShieldLayers = 0;
				shieldTimer = 0;
				return;
			}

			shieldTimer++;
			if (shieldTimer >= 9 * 60) {
				shieldTimer = 0;
				ShieldLayers = Math.Min(3, ShieldLayers + 1);
			}

			if (ShieldLayers > 0)
				Player.AddBuff(ModContent.BuffType<NianShieldBuff>(), 2);
		}

		public override void OnRespawn() {
			shieldTimer = 0;
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (MudrockHelmetActive) {
				modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
					if (info.DamageSource.TryGetCausingEntity(out var entity) && entity is NPC npc
						&& OperatorTagHelper.NpcHasFaction(npc, OperatorFaction.Sarkaz)) {
						info.Damage = (int)(info.Damage * 0.7f);
					}
				};
			}

			if (!MudrockSetActive || ShieldLayers <= 0)
				return;

			modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
				if (info.Damage <= 50 || ShieldLayers <= 0)
					return;

				info.Damage = (int)(info.Damage * 0.8f);
				ShieldLayers--;
				int heal = Math.Max(1, (int)(Player.statLifeMax2 * 0.1f));
				Player.Heal(heal);
				Player.HealEffect(heal);
			};
		}
	}
}
