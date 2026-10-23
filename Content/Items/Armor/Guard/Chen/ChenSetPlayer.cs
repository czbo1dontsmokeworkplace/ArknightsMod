using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Players;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Guard.Chen
{
	internal class ChenSetPlayer : ArknightsArmorPlayer, global::ArknightsMod.Players.IProbabilityDodgeSource
	{
    public string DodgeName => "陈头盔（仅NPC接触）";
    public double DodgeChance => ChenHelmetActive ? .10 : 0;
    public bool CanDodge(Player.HurtInfo info) => info.DamageSource != null
        && info.DamageSource.SourceNPCIndex >= 0
        && info.DamageSource.SourceProjectileLocalIndex < 0;

		public bool ChenHelmetActive;
		public bool ChenSetActive;

		private int spTimer;

		public override void ResetEffects() {
			ChenHelmetActive = false;
			ChenSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 ChenHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			ChenHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<ChenHead>();
			ChenSetActive = ChenHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<ChenBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<ChenLegs>();

			if (ChenHelmetActive) {
				Player.GetDamage(DamageClass.Generic) += 0.05f;
				Player.statDefense.AdditiveBonus += 0.05f;
			}
		}



		// Run from the world hook: another mod throwing during Player.ResetEffects
		// must not prevent the server from advancing this equipment aura.
		internal void UpdateSpPulse() {
			if (Main.netMode == NetmodeID.MultiplayerClient) return;
			int amount = GetChenSpAmount(Player);
			if (amount == 0) { spTimer = 0; return; }
			if (++spTimer < 3 * 60) return;
			spTimer = 0;
			if (Main.netMode == NetmodeID.SinglePlayer)
				ApplyGrant(Player, amount);
			else {
				// Charge lives on the recipient client; the server authorizes each pulse.
				var packet = Mod.GetPacket();
				packet.Write((short)ArknightsMod.ArkMessageID.ChenSkillPointGrant);
				packet.Write((byte)amount);
				packet.Send(Player.whoAmI);
			}
		}

		public override void UpdateDead() => spTimer = 0;

		internal static int GetChenSpAmount(Player player) {
			if (player == null || !player.active || player.dead) return 0;
			if (WearsFullSet(player)) return 2;
			for (int i = 0; i < Main.maxPlayers; i++) {
				Player other = Main.player[i];
				if (other == null || !other.active || other.dead || other.whoAmI == player.whoAmI)
					continue;
				bool allied = (!other.hostile && !player.hostile)
					|| (other.team != 0 && other.team == player.team);
				if (allied && WearsFullSet(other)) return 1;
			}
			return 0;
		}

		private static bool WearsFullSet(Player player) =>
			player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<ChenHead>()
			&& player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<ChenBody>()
			&& player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<ChenLegs>();

		internal static void ReceiveGrant(BinaryReader reader) {
			int amount = reader.ReadByte();
			if (Main.netMode != NetmodeID.MultiplayerClient || (amount != 1 && amount != 2)) return;
			Player player = Main.LocalPlayer;
			if (player.active && !player.dead) ApplyGrant(player, amount);
		}

		private static void ApplyGrant(Player player, int amount) {
			var weapon = player.GetModPlayer<WeaponPlayer>();
			// The wearer's base point and extra point each show their own +1.
			for (int i = 0; i < amount; i++)
				if (!weapon.TryAddSP(1)) break;
		}
	}

	public sealed class ChenSkillPointSystem : ModSystem
	{
		public override void PostUpdatePlayers() {
			if (Main.netMode == NetmodeID.MultiplayerClient) return;
			foreach (Player player in Main.ActivePlayers)
				player.GetModPlayer<ChenSetPlayer>().UpdateSpPulse();
		}
	}
}
