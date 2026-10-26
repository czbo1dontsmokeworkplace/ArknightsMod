using System;
using System.IO;
using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Systems.Gameplay.OperatorTags;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Ulpianus
{
	internal class UlpianusSetPlayer : ArknightsArmorPlayer
	{
		public bool UlpianusHelmetActive;
		public bool UlpianusSetActive;
		public int KillStacks { get; private set; }

		internal static bool WearsHelmet(Player player) => player.active && !player.dead
			&& player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<UlpianusHead>();

		private static int HighestActiveStacks() {
			int highest = 0;
			foreach (Player player in Main.ActivePlayers) {
				if (WearsHelmet(player))
					highest = Math.Max(highest, player.GetModPlayer<UlpianusSetPlayer>().KillStacks);
			}
			return highest;
		}

		private int SharedStackUnits() {
			if (!Player.active || Player.dead)
				return 0;
			int share = WearsHelmet(Player) ? 2
				: OperatorTagHelper.PlayerHasFaction(Player, OperatorFaction.AbyssalHunter) ? 1 : 0;
			return share == 0 ? 0 : HighestActiveStacks() * share;
		}

		public override void ResetEffects() {
			UlpianusHelmetActive = false;
			UlpianusSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 UlpianusHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			UlpianusHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<UlpianusHead>();
			UlpianusSetActive = UlpianusHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<UlpianusBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<UlpianusLegs>();

			if (!UlpianusHelmetActive)
				ClearKillStacks();
		}

		public override void ModifyMaxStats(out StatModifier health, out StatModifier mana) {
			health = StatModifier.Default;
			mana = StatModifier.Default;
			// Read current equipment and shared stacks before equipment hooks run.
			health.Base += SharedStackUnits() * 6;
		}

		private void ApplyArmorDamageStats() {
			Player.GetDamage(DamageClass.Generic).Flat += SharedStackUnits() * 2;
		}

		internal void AddKillStack() {
			if (!WearsHelmet(Player) || KillStacks >= 9 || Main.netMode == NetmodeID.MultiplayerClient)
				return;
			KillStacks++;
			SyncStacks();
		}

		private void ClearKillStacks() {
			if (KillStacks == 0)
				return;
			KillStacks = 0;
			SyncStacks();
		}

		public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource) => ClearKillStacks();
		public override void UpdateDead() => ClearKillStacks();
		public override void OnEnterWorld() => ClearKillStacks();
		public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SyncStacks(toWho);

		private void SyncStacks(int toWho = -1) {
			if (Main.netMode != NetmodeID.Server)
				return;
			ModPacket packet = Mod.GetPacket();
			packet.Write((short)ArknightsMod.ArkMessageID.UlpianusKillStacks);
			packet.Write((byte)Player.whoAmI);
			packet.Write((byte)KillStacks);
			packet.Send(toWho);
		}

		internal static void ReceiveStacks(BinaryReader reader) {
			// Only the server awards kills and publishes personal counters.
			if (Main.netMode != NetmodeID.MultiplayerClient)
				return;
			int playerIndex = reader.ReadByte();
			int stacks = reader.ReadByte();
			if (playerIndex < Main.maxPlayers && stacks <= 9)
				Main.player[playerIndex].GetModPlayer<UlpianusSetPlayer>().KillStacks = stacks;
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (!UlpianusSetActive)
				return;

			modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
				if (info.Damage <= 0)
					return;

				float lifeRatio = Player.statLife / (float)Math.Max(1, Player.statLifeMax2);
				int heal = lifeRatio < 0.5f ? 16 : 12;
				if (HoldsSignatureAnchor())
					heal *= 2;

				Player.Heal(heal);
				Player.HealEffect(heal, false);
			};
		}

		private bool HoldsSignatureAnchor() => Player.HeldItem.ModItem is UlpianusAnchor;

		public override void PostUpdateMiscEffects() {
			ApplyArmorDamageStats();
		}
	}
}
