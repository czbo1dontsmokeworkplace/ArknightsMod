using ArknightsMod.Systems.InstancedSpace;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Common.InstancedSpace.GlobalProjectiles
{
	public class InstancedRoomProjectileGuard : GlobalProjectile
	{
		public override void OnSpawn(Projectile projectile, IEntitySource source)
		{
			if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
				return;
			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
				return;

			Player owner = Main.player[projectile.owner];
			if (owner == null || !owner.active)
				return;

			int ownerRoom = InstancedRoomSystem.GetRoomIdForPlayer(owner);
			if (ownerRoom == 0)
				return;

			int projRoom = InstancedRoomSystem.GetRoomIdAtWorldPosition(projectile.Center);
			if (projRoom != ownerRoom)
				projectile.Kill();
		}

		public override void PostAI(Projectile projectile)
		{
			if (Main.netMode == Terraria.ID.NetmodeID.MultiplayerClient)
				return;
			if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
				return;

			Player owner = Main.player[projectile.owner];
			if (owner == null || !owner.active)
				return;

			int ownerRoom = InstancedRoomSystem.GetRoomIdForPlayer(owner);
			if (ownerRoom == 0)
				return;

			int projRoom = InstancedRoomSystem.GetRoomIdAtWorldPosition(projectile.Center);
			if (projRoom != ownerRoom)
				projectile.Kill();
		}
	}
}
