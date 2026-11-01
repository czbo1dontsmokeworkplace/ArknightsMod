using ArknightsMod.Content.Items.Armor.Specialist.ExusiaiAlter;
using ArknightsMod.Content.Items.Weapons.Defender.Horn;
using ArknightsMod.Content.Items.Weapons.Sniper.Exusiai;
using ArknightsMod.Players;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	internal static class OperatorAmmoConsumeHelper
	{
		internal enum AmmoSkillKind : byte { None, HornBarrage, VectorEmpowerment }

		// Called at the actual skill-round decrement, once per shot, never per projectile lane.
		public static void NotifyAmmoConsumed(Player consumer, AmmoSkillKind kind) {
			if (consumer == null || !consumer.active || consumer.dead || consumer.whoAmI != Main.myPlayer
				|| !MatchesWeapon(consumer, kind)) return;
			if (Main.netMode == NetmodeID.MultiplayerClient) {
				ModPacket packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
				packet.Write((short)ArknightsMod.ArkMessageID.OperatorAmmoConsumed);
				packet.Write((byte)kind);
				packet.Send();
			}
			else if (Main.netMode == NetmodeID.SinglePlayer) ApplyConsumed(consumer);
		}

		public static bool HasAmmoSkill(Player player) {
			int skill = player.GetModPlayer<WeaponPlayer>().Skill;
			return player.HeldItem.ModItem is HornGrenadeLauncher && skill == 1
				|| player.HeldItem.ModItem is ExusiaiVector && skill == 0;
		}

		private static bool MatchesWeapon(Player player, AmmoSkillKind kind) => kind switch {
			AmmoSkillKind.HornBarrage => player.HeldItem.ModItem is HornGrenadeLauncher,
			AmmoSkillKind.VectorEmpowerment => player.HeldItem.ModItem is ExusiaiVector,
			_ => false
		};

		private static void ApplyConsumed(Player consumer) {
			// Each recipient owns its HP and projectile damage, just like ordinary tML attacks.
			// The server relays one validated event rather than healing remote players twice.
			Player wearer = Main.LocalPlayer;
			if (wearer.active && !wearer.dead)
				wearer.GetModPlayer<ExusiaiAlterSetPlayer>().OnAllyAmmoConsumed(consumer);
		}

		internal static void Receive(BinaryReader reader, int sender) {
			AmmoSkillKind kind = (AmmoSkillKind)reader.ReadByte();
			if (Main.netMode == NetmodeID.Server) {
				if ((uint)sender >= Main.maxPlayers) return;
				Player consumer = Main.player[sender];
				if (!consumer.active || consumer.dead || !MatchesWeapon(consumer, kind)) return;
				// Do not require SkillActive: the last round and Horn's manual dump can end the skill first.
				ModPacket packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
				packet.Write((short)ArknightsMod.ArkMessageID.OperatorAmmoConsumed);
				packet.Write((byte)kind);
				packet.Write((byte)sender);
				packet.Send();
			}
			else if (Main.netMode == NetmodeID.MultiplayerClient) {
				int index = reader.ReadByte();
				if ((uint)index >= Main.maxPlayers || kind is not (AmmoSkillKind.HornBarrage or AmmoSkillKind.VectorEmpowerment)) return;
				Player consumer = Main.player[index];
				if (consumer.active && !consumer.dead) ApplyConsumed(consumer);
			}
		}
	}
}
