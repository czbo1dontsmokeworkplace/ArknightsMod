using System;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Systems.InstancedSpace;

namespace ArknightsMod.Content.Commands
{
	public class InstancedSpaceCommand : ModCommand
	{
		public override string Command => "arkspace";
		public override CommandType Type => CommandType.World;
		public override string Usage => "/arkspace enter [roomId=1] | /arkspace exit | /arkspace rebuild [roomId=1] | /arkspace reset";
		public override string Description => "Instanced room command.";

		public override void Action(CommandCaller caller, string input, string[] args)
		{
			if (args.Length == 0 || args[0].Equals("help", StringComparison.OrdinalIgnoreCase))
				throw new UsageException(Usage);

			string sub = args[0].ToLowerInvariant();
			if (sub == "reset")
			{
				InstancedRoomSystem.ResetRooms();
				caller.Reply("Rooms reset. Re-enter to teleport to the hidden location.");
				return;
			}
			if (sub == "exit")
			{
				var player = caller.Player;
				if (player == null || !player.active)
					return;
				InstancedRoomSystem.ExitRoom(player, true);
				caller.Reply("Exited room");
				return;
			}

			int roomId = 1;
			if (args.Length >= 2 && !int.TryParse(args[1], out roomId))
				throw new UsageException(Usage);

			if (sub == "enter")
			{
				var player = caller.Player;
				if (player == null || !player.active)
					return;
				if (!InstancedRoomSystem.Rooms.ContainsKey(roomId))
					throw new UsageException("Unknown roomId: " + roomId);
				bool ok = InstancedRoomSystem.TryEnterRoom(player, roomId);
				caller.Reply(ok ? $"Entered room {roomId}" : $"Failed to enter room {roomId}");
				return;
			}

			if (sub == "rebuild")
			{
				bool ok = InstancedRoomSystem.ResetRoomToBaseTemplate(roomId, out string details);
				caller.Reply(ok ? $"Room {roomId} rebuilt ({details})" : $"Unknown roomId: {roomId} ({details})");
				return;
			}

			throw new UsageException(Usage);
		}
	}
}
