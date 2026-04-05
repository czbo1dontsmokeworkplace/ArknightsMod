using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Systems.InstancedSpace;

namespace ArknightsMod.Content.Globals.InstancedSpace
{
	public class ProtocolSpaceGlobalNPC : GlobalNPC
	{
		public override void AI(NPC npc)
		{
			if (!ProtocolSpaceEventSystem.IsEventActive)
				return;
			if (!ProtocolSpaceEventSystem.IsOriginiumSlug(npc.type))
				return;
			if (!ProtocolSpaceEventSystem.IsInProtocolRoom(npc.Center))
			{
				npc.active = false;
				npc.netSkip = -1;
				npc.life = 0;
				npc.checkDead();
			}
		}

		public override void OnKill(NPC npc)
		{
			if (!ProtocolSpaceEventSystem.IsEventActive)
				return;
			if (!ProtocolSpaceEventSystem.IsOriginiumSlug(npc.type))
				return;
			if (!ProtocolSpaceEventSystem.IsInProtocolRoom(npc.Center))
				return;
			ProtocolSpaceEventSystem.NotifySlugKilled();
		}
	}
}
