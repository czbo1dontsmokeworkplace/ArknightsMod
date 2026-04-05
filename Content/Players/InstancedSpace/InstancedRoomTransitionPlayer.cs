using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Systems.InstancedSpace;

namespace ArknightsMod.Content.Players.InstancedSpace
{
	public class InstancedRoomTransitionPlayer : ModPlayer
	{
		public static float BlackOverlayOpacity;
		private int _lastRoom;

		public override void Initialize()
		{
			_lastRoom = 0;
			BlackOverlayOpacity = 0f;
		}

		public override void PostUpdate()
		{
			if (Main.dedServ || Player.whoAmI != Main.myPlayer)
				return;

			int room = InstancedRoomSystem.GetRoomIdForPlayer(Player);
			if (room != _lastRoom)
			{
				_lastRoom = room;
				BlackOverlayOpacity = 1f;
			}

			if (BlackOverlayOpacity <= 0f)
				return;

			BlackOverlayOpacity -= 0.06f;
			if (BlackOverlayOpacity < 0f)
				BlackOverlayOpacity = 0f;
		}
	}
}
