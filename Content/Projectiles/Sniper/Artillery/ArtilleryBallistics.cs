using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Artillery;

internal static class ArtilleryBallistics
{
    internal const float MaximumRange = 650f;
    internal const float LaunchSpeed = 21.6f;

    internal static Vector2 LaunchDirection(Player player, Vector2 target)
    {
        Vector2 anchor = new(MathHelper.Lerp(target.X, player.Center.X, .55f),
            player.Center.Y - 500f * player.gravDir);
        return (anchor - player.Center).SafeNormalize(-Vector2.UnitY * player.gravDir);
    }
}
