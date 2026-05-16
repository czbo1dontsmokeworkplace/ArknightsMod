using ArknightsMod.Systems.InstancedSpace;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.InstancedSpace.GlobalTiles
{
	public class InstancedRoomTileGuard : GlobalTile
	{
		public override bool CanPlace(int i, int j, int type)
		{
			Player player = Main.LocalPlayer;
			if (player == null)
				return true;
			if (IsProtectedBoundary(i, j))
				return false;
			return CanInteract(i, j, player);
		}

		public override bool CanKillTile(int i, int j, int type, ref bool blockDamaged)
		{
			Player player = Main.LocalPlayer;
			if (player == null)
				return true;
			if (IsProtectedBoundary(i, j))
				return false;
			return CanInteract(i, j, player);
		}

		public override bool CanExplode(int i, int j, int type)
		{
			Player player = Main.LocalPlayer;
			if (player == null)
				return true;
			if (IsProtectedBoundary(i, j))
				return false;
			return CanInteract(i, j, player);
		}

		private static bool CanInteract(int i, int j, Player player)
		{
			int playerRoom = InstancedRoomSystem.GetRoomIdForPlayer(player);
			int tileRoom = InstancedRoomSystem.GetRoomIdAtTile(i, j);
			if (tileRoom != 0 && !InstancedRoomSystem.IsRoomBuilt(tileRoom))
				tileRoom = 0;

			if (playerRoom == 0)
				return tileRoom == 0;

			return tileRoom == playerRoom;
		}

		private static bool IsProtectedBoundary(int i, int j)
		{
			foreach (var kv in InstancedRoomSystem.Rooms)
			{
				if (!InstancedRoomSystem.IsRoomBuilt(kv.Key))
					continue;
				var a = kv.Value.Area;
				bool inside = a.Contains(i, j);
				bool onInnerBorder = inside && (i == a.Left || i == a.Right - 1 || j == a.Top || j == a.Bottom - 1);
				if (onInnerBorder)
					return true;

				var expanded = a;
				expanded.Inflate(1, 1);
				bool inOuterRing = expanded.Contains(i, j) && !inside;
				if (inOuterRing)
					return true;
			}
			return false;
		}
	}
}
