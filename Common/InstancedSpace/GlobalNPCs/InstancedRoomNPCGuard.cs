using ArknightsMod.Systems.InstancedSpace;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Common.InstancedSpace.GlobalNPCs
{
	public class InstancedRoomNPCGuard : GlobalNPC
	{
		public override void OnSpawn(NPC npc, IEntitySource source)
		{
			if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
				return;

			_ = InstancedRoomSystem.GetRoomIdAtWorldPosition(npc.Center);
		}
	}
}
