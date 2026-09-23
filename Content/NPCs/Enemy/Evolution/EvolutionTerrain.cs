using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionTerrain
{
    internal static bool SupportsRock(Tile tile) => tile.HasTile && !tile.IsActuated &&
        (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType] && tile.TileFrameY == 0);
    internal static bool TrySurface(Vector2 near, out Vector2 surface, int rows = 38)
    {
        surface = near;
        int x = Math.Clamp((int)(near.X / 16), 1, Main.maxTilesX - 2);
        int start = Math.Clamp((int)(near.Y / 16), 1, Main.maxTilesY - 2);
        for (int y = start; y < Math.Min(Main.maxTilesY - 1, start + rows); y++)
        {
            Tile tile = Main.tile[x, y];
            if (!SupportsRock(tile)) continue;
            surface = new Vector2(near.X, y * 16 + (tile.IsHalfBlock ? 8 : 0));
            return true;
        }
        return false;
    }
}

public sealed partial class Evolution
{
    internal void ThrowBomb(Vector2 origin, Vector2 destination, bool fragments = false, int fuse = 84)
    {
        int flight = Math.Max(1, fuse - 36);
        float travel = .955f * (1 - MathF.Pow(.955f, flight - 1)) / .045f;
        Vector2 velocity = (destination - origin) / Math.Max(1, travel);
        if (velocity.Length() > 24) velocity = velocity.SafeNormalize(Vector2.UnitY) * 24;
        Shoot(EvolutionShot.CrimsonBomb, origin, velocity, fragments ? -140 : 150, fuse, fuse + 36);
    }
    internal void EruptAt(Vector2 near, int warning = 48)
    {
        if (!EvolutionTerrain.TrySurface(near, out Vector2 ground))
        {
            // No imaginary floor in a sky arena: a stationary, visibly warned air mine takes its place.
            Shoot(EvolutionShot.CrimsonBomb, near + new Vector2(0, 240), Vector2.Zero, 130, warning + 20, warning + 56);
            return;
        }
        float height = MathHelper.Clamp(ground.Y - near.Y + 180, 300, 780);
        Shoot(EvolutionShot.Eruption, ground - new Vector2(0, 4), -Vector2.UnitY, height, warning, warning + 52);
    }
    private void GroundWave(Vector2 center, int direction)
    {
        for (int i = -3; i <= 3; i++)
        {
            if (i == 0) continue;
            EruptAt(center + new Vector2(i * 150, 24), 42 + (direction > 0 ? i + 3 : 3 - i) * 5);
        }
    }
}
