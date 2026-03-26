using ArknightsMod.Systems.InstancedSpace;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Common.InstancedSpace.GlobalItems
{
	public class InstancedRoomDropSuppressor : GlobalItem
	{
		public override void OnSpawn(Item item, IEntitySource source)
		{
			if (!InstancedRoomSystem.SuppressItemDrops)
				return;

			Rectangle? area = InstancedRoomSystem.CurrentMutationArea;
			if (!area.HasValue)
				return;

			int x = (int)(item.position.X / 16f);
			int y = (int)(item.position.Y / 16f);
			if (!area.Value.Contains(x, y))
				return;

			item.TurnToAir();
			item.active = false;
		}
	}
}
