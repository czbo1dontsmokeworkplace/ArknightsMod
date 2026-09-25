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
    private void DoInfernoSupport(Player player, int timer)
    {
        int wave = EvolutionRules.TransitionSupportWave(timer);
        if (wave < 0) return;
        // Every volley snapshots a fresh location. Neither a warned bomb nor a rolling rock retargets.
        Vector2 center = player.Center + player.velocity * 10;
        switch (wave % 3)
        {
            case 0:
                for (int i = -6; i <= 6; i++)
                {
                    if (Math.Abs(i) <= 1) continue;
                    Shoot(EvolutionShot.Blood, center + new Vector2(i * 115, -460 - Math.Abs(i) % 2 * 60),
                        new Vector2(-Math.Sign(i) * .7f, 3), lifetime: 190, peripheral: Math.Abs(i) > 4);
                }
                break;
            case 1:
                for (int side = -1; side <= 1; side += 2)
                for (int row = 0; row < 2; row++)
                    ThrowBomb(center + new Vector2(side * 520, -260 + row * 380),
                        center + new Vector2(side * 250, -100 + row * 240), row == 1, 78 + row * 12);
                break;
            default:
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 lane = center + new Vector2(side * 440, 16);
                    if (EvolutionTerrain.TrySurface(lane, out Vector2 ground))
                        Shoot(EvolutionShot.Rock, ground - new Vector2(0, 26), new Vector2(-side * 7, 0), 1, 42, 280);
                    else
                        Shoot(EvolutionShot.Rock, center + new Vector2(side * 380, -420), new Vector2(-side * 3.4f, 1), 2, 42, 240);
                }
                break;
        }
    }
}
