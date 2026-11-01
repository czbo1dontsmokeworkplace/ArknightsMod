using System;
using System.IO;
using ArknightsMod.Content.Items.Armor.Defender.Spot;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor
{
	// 技能治疗友方时由各技能实现处调用。
	internal static class OperatorSkillHealHelper
	{
		public static int ApplyHeal(Player target, int baseAmount, Player healer = null) {
			if (Main.netMode == NetmodeID.MultiplayerClient || baseAmount <= 0 || !target.active || target.dead)
				return 0;

			if (target.statLife >= target.statLifeMax2)
				return 0;
			int healing = SpotHealingHelper.ModifyOutgoingHeal(healer, baseAmount);
			int finalAmount = Math.Min(target.GetModPlayer<IncomingHealingPlayer>().Modify(healing), target.statLifeMax2 - target.statLife);
			if (finalAmount <= 0)
				return 0;

			if (Main.netMode == NetmodeID.SinglePlayer)
				target.GetModPlayer<IncomingHealingPlayer>().HealFinal(finalAmount);
			else {
				target.statLife += finalAmount;
				var packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
				packet.Write((short)ArknightsMod.ArkMessageID.OperatorSkillHealingGrant);
				packet.Write((byte)target.whoAmI);
				packet.Write(finalAmount);
				packet.Send(target.whoAmI);
			}

			if (healer != null) {
				DirectHealingEffects.OnDirectHeal(healer, target, finalAmount);
			}

			return finalAmount;
		}

		internal static void ReceiveGrant(BinaryReader reader) {
			int index = reader.ReadByte(), amount = reader.ReadInt32();
			if (Main.netMode != NetmodeID.MultiplayerClient || index != Main.myPlayer || amount <= 0)
				return;
			Player target = Main.LocalPlayer;
			if (!target.active || target.dead)
				return;
			target.GetModPlayer<IncomingHealingPlayer>().HealFinal(Math.Min(amount, Math.Max(0, target.statLifeMax2 - target.statLife)));
			NetMessage.SendData(MessageID.PlayerLifeMana, number: index);
		}

	}
}
