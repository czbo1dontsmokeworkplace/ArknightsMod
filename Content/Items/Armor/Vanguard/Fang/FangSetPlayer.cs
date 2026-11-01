using ArknightsMod.Content.Items.Armor;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Players;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanguard.Fang
{
	internal class FangSetPlayer : ArknightsArmorPlayer
	{
		public bool FangHelmetActive;
		public bool FangSetActive;

		private bool spawnSpGranted;
		private bool helmetGrantSent;
		private bool helmetGrantAuthorized;
		private int noDamageTimer;
		private enum Message : byte { HelmetGrant, SetGrant, HelmetGranted, ResetNoDamageTimer }

		public override void ResetEffects() {
			FangHelmetActive = false;
			FangSetActive = false;
		}

		// NeoArmor Reforge：套装件是独立 ItemID，穿上它本身就代表"已经是套装形态"，
		// 不需要再查 hasUpgraded；player.setBonus 文本交给 FangHead 的
		// SetProfile.SetBonusKey 统一设置，这里不再重复设置一遍。
		public override void PostUpdateEquips() {
			FangHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<FangHead>();
			FangSetActive = FangHelmetActive
				&& Player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<FangBody>()
				&& Player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<FangLegs>();
		}

		public override void PostUpdate() {
			if (Player.dead)
				return;

			if (Main.netMode == NetmodeID.MultiplayerClient) {
				if (Player.whoAmI != Main.myPlayer)
					return;
				if (!FangHelmetActive)
					helmetGrantAuthorized = false;
				TryCompleteHelmetGrant();
				if (!FangSetActive)
					noDamageTimer = 0;
				return;
			}

			if (!FangHelmetActive)
				helmetGrantSent = false;
			else if (!spawnSpGranted) {
				if (Main.netMode == NetmodeID.SinglePlayer)
					spawnSpGranted = Player.GetModPlayer<WeaponPlayer>().TryAddSP(4);
				else if (!helmetGrantSent) {
					SendMessage(Message.HelmetGrant, Player.whoAmI);
					helmetGrantSent = true;
				}
			}

			if (!FangSetActive) {
				noDamageTimer = 0;
				return;
			}

			noDamageTimer++;
			if (noDamageTimer >= 4 * 60) {
				if (Main.netMode == NetmodeID.SinglePlayer)
					Player.GetModPlayer<WeaponPlayer>().TryAddSP(2);
				else
					SendMessage(Message.SetGrant, Player.whoAmI);
				noDamageTimer = 0;
			}
		}

		public override void UpdateDead() {
			spawnSpGranted = false;
			helmetGrantSent = false;
			helmetGrantAuthorized = false;
			noDamageTimer = 0;
		}

		public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
			if (FangSetActive) {
				noDamageTimer = 0;
				if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI == Main.myPlayer)
					SendMessage(Message.ResetNoDamageTimer);
			}
		}

		private void TryCompleteHelmetGrant() {
			if (!helmetGrantAuthorized || spawnSpGranted || !FangHelmetActive
				|| !Player.GetModPlayer<WeaponPlayer>().TryAddSP(4))
				return;
			// 资源由所属客户端维护，只有实际增加技力才确认本次部署奖励已发放。
			spawnSpGranted = true;
			helmetGrantAuthorized = false;
			SendMessage(Message.HelmetGranted);
		}

		private static void SendMessage(Message kind, int recipient = -1) {
			var packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
			packet.Write((short)ArknightsMod.ArkMessageID.FangSkillPointGrant);
			packet.Write((byte)kind);
			packet.Send(recipient);
		}

		internal static void ReceiveGrant(BinaryReader reader, int sender) {
			Message kind = (Message)reader.ReadByte();
			if (Main.netMode == NetmodeID.Server) {
				if (sender < 0 || sender >= Main.maxPlayers)
					return;
				FangSetPlayer state = Main.player[sender].GetModPlayer<FangSetPlayer>();
				if (kind == Message.HelmetGranted && state.helmetGrantSent)
					state.spawnSpGranted = true;
				else if (kind == Message.ResetNoDamageTimer)
					state.noDamageTimer = 0;
				return;
			}
			if (Main.netMode != NetmodeID.MultiplayerClient || !Main.LocalPlayer.active || Main.LocalPlayer.dead)
				return;
			Player player = Main.LocalPlayer;
			FangSetPlayer local = player.GetModPlayer<FangSetPlayer>();
			bool helmet = player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<FangHead>();
			if (kind == Message.HelmetGrant && helmet) {
				local.helmetGrantAuthorized = true;
				local.TryCompleteHelmetGrant();
			}
			else if (kind == Message.SetGrant && helmet
				&& player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<FangBody>()
				&& player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<FangLegs>())
				player.GetModPlayer<WeaponPlayer>().TryAddSP(2);
		}
	}
}
