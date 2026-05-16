using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Systems.InstancedSpace;

namespace ArknightsMod.Content.Players.InstancedSpace
{
	public class InstancedRoomNoSpawnGlobalNPC : GlobalNPC
	{
		public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
		{
			if (InstancedRoomSystem.GetRoomIdForPlayer(player) == 0)
				return;

			spawnRate = int.MaxValue;
			maxSpawns = 0;
		}

		public override void EditSpawnPool(IDictionary<int, float> pool, NPC.Spawner spawner)
		{
			if (InstancedRoomSystem.GetRoomIdForPlayer(spawner.Player) == 0)
				return;

			pool.Clear();
		}
	}
}
