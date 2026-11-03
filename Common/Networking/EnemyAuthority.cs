using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace ArknightsMod.Common.Networking;

/// <summary>Enemy-created entities belong to the server, including chained hazards.</summary>
internal static class EnemyAuthority
{
    internal static int NewProjectile(IEntitySource source, Vector2 position, Vector2 velocity,
        int type, int damage, float knockBack, int owner = -1, float ai0 = 0, float ai1 = 0, float ai2 = 0)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return Main.maxProjectiles;
        return Projectile.NewProjectile(source, position, velocity, type, damage, knockBack, Main.myPlayer, ai0, ai1, ai2);
    }

    internal static int NewProjectile(IEntitySource source, float x, float y, float speedX, float speedY,
        int type, int damage, float knockBack, int owner = -1, float ai0 = 0, float ai1 = 0, float ai2 = 0) =>
        NewProjectile(source, new Vector2(x, y), new Vector2(speedX, speedY), type, damage, knockBack, owner, ai0, ai1, ai2);

    internal static int NewNPC(IEntitySource source, int x, int y, int type, int start = 0,
        float ai0 = 0, float ai1 = 0, float ai2 = 0, float ai3 = 0, int Target = 255)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return Main.maxNPCs;
        return NPC.NewNPC(source, x, y, type, start, ai0, ai1, ai2, ai3, Target);
    }
}
