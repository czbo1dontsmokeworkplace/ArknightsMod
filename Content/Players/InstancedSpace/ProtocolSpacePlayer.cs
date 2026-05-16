using System;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using ArknightsMod.Systems.InstancedSpace;

namespace ArknightsMod.Content.Players.InstancedSpace
{
	public class ProtocolSpacePlayer : ModPlayer
	{
		private int _exitConfirmTicks;
		private int _exitCountdownTicks;

		public override void Initialize()
		{
			_exitConfirmTicks = 0;
			_exitCountdownTicks = 0;
		}

		public override void ResetEffects()
		{
			if (_exitConfirmTicks > 0)
				_exitConfirmTicks--;
		}

		public override void PostUpdate()
		{
			if (_exitCountdownTicks <= 0)
				return;

			_exitCountdownTicks--;
			Player.controlLeft = false;
			Player.controlRight = false;
			Player.controlJump = false;
			Player.controlDown = false;
			Player.controlUp = false;
			Player.controlUseItem = false;
			Player.controlUseTile = false;
			Player.controlThrow = false;
			Player.velocity *= 0.85f;

			if (_exitCountdownTicks == 0)
				ProtocolSpaceEventSystem.ForceExit(Player);
		}

		public void ReceiveExitCountdown(int ticks)
		{
			_exitCountdownTicks = Math.Max(_exitCountdownTicks, ticks);
			_exitConfirmTicks = 0;
		}

		public void TryRequestExit()
		{
			if (!ProtocolSpaceEventSystem.IsEventCompleted || _exitCountdownTicks > 0)
				return;

			if (_exitConfirmTicks <= 0)
			{
				_exitConfirmTicks = 180;
				if (Main.netMode != NetmodeID.Server)
					Main.NewText(Language.GetTextValue("Mods.ArknightsMod.ProtocolSpace.ExitConfirm"));
				return;
			}

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ProtocolSpaceEventSystem.RequestExitCountdownFromClient();
				return;
			}

			ProtocolSpaceEventSystem.StartExitCountdownForPlayer(Player);
		}
	}
}
