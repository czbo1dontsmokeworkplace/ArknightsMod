using System;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionCorridorPlacement
{
    internal const int HalfScanTiles = 100, MaximumRaiseTiles = 15;
    internal static bool Acceptable(int blocked, int sampled) => sampled > 0 && blocked * 5 < sampled;
    internal static int CountBlocks(float centerX, float lineY, out int sampled)
    {
        int center = (int)MathF.Floor(centerX / 16), row = (int)MathF.Floor(lineY / 16);
        int blocked = 0; sampled = 0;
        if (row < 1 || row >= Main.maxTilesY - 1) return 0;
        for (int x = Math.Max(1, center - HalfScanTiles); x <= Math.Min(Main.maxTilesX - 2, center + HalfScanTiles); x++)
        {
            sampled++;
            Tile tile = Main.tile[x, row];
            if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]) blocked++;
        }
        return blocked;
    }
    internal static float SelectCenter(float defaultCenter, float playerX)
    {
        float bottom = defaultCenter + EvolutionRules.CorridorHeight * .5f;
        int maximum = Math.Min(MaximumRaiseTiles, Math.Max(0, (int)MathF.Floor((bottom - EvolutionRules.CorridorHeight - 32) / 16)));
        for (int lift = 0; lift <= maximum; lift++)
        {
            int blocked = CountBlocks(playerX, bottom - lift * 16, out int sampled);
            if (Acceptable(blocked, sampled)) return defaultCenter - lift * 16;
        }
        // A dense underground arena never triggers an unbounded search toward the sky.
        return defaultCenter - maximum * 16;
    }
}
