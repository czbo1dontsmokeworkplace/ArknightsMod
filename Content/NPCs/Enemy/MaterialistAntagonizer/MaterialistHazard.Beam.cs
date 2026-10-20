using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistHazard
{
    // Three rays keep the damaging core clear of terrain. No artificial firing-distance limit:
    // an unobstructed ray reaches the world boundary, while platforms/actuated tiles are ignored.
    internal static float TraceBeam(Vector2 origin, Vector2 direction, float width)
    {
        direction = direction.SafeNormalize(Vector2.UnitY);
        Vector2 normal = direction.RotatedBy(MathHelper.PiOver2) * (width * .5f);
        return Math.Max(0, Math.Min(TraceBeamRay(origin, direction),
            Math.Min(TraceBeamRay(origin - normal, direction), TraceBeamRay(origin + normal, direction))) - .5f);
    }

    private static float TraceBeamRay(Vector2 origin, Vector2 direction)
    {
        int x = (int)MathF.Floor(origin.X / 16), y = (int)MathF.Floor(origin.Y / 16);
        int stepX = Math.Sign(direction.X), stepY = Math.Sign(direction.Y);
        float deltaX = stepX == 0 ? float.PositiveInfinity : 16 / Math.Abs(direction.X);
        float deltaY = stepY == 0 ? float.PositiveInfinity : 16 / Math.Abs(direction.Y);
        float nextX = stepX == 0 ? float.PositiveInfinity : ((x + (stepX > 0 ? 1 : 0)) * 16 - origin.X) / direction.X;
        float nextY = stepY == 0 ? float.PositiveInfinity : ((y + (stepY > 0 ? 1 : 0)) * 16 - origin.Y) / direction.Y;
        float travelled = 0;
        while (WorldGen.InWorld(x, y, 1))
        {
            Tile tile = Main.tile[x, y];
            if (tile.HasTile && !tile.IsActuated && Main.tileSolid[tile.TileType] &&
                !Main.tileSolidTop[tile.TileType] && !TileID.Sets.Platforms[tile.TileType])
            {
                float enter = travelled, exit = Math.Min(nextX, nextY);
                Vector2 local = origin - new Vector2(x * 16, y * 16);
                bool hit = ClipBeamPlane(local, direction, new Vector2(-1, 0), 0, ref enter, ref exit) &&
                    ClipBeamPlane(local, direction, Vector2.UnitX, 16, ref enter, ref exit) &&
                    ClipBeamPlane(local, direction, new Vector2(0, -1), tile.IsHalfBlock ? -8 : 0, ref enter, ref exit) &&
                    ClipBeamPlane(local, direction, Vector2.UnitY, 16, ref enter, ref exit);
                if (hit)
                {
                    hit = tile.Slope switch {
                        SlopeType.SlopeDownLeft => ClipBeamPlane(local, direction, new Vector2(1, -1), 0, ref enter, ref exit),
                        SlopeType.SlopeDownRight => ClipBeamPlane(local, direction, new Vector2(-1, -1), -16, ref enter, ref exit),
                        SlopeType.SlopeUpLeft => ClipBeamPlane(local, direction, Vector2.One, 16, ref enter, ref exit),
                        SlopeType.SlopeUpRight => ClipBeamPlane(local, direction, new Vector2(-1, 1), 0, ref enter, ref exit),
                        _ => true
                    };
                    if (hit) return Math.Max(0, enter);
                }
            }
            travelled = Math.Min(nextX, nextY);
            if (nextX < nextY) { x += stepX; nextX += deltaX; }
            else if (nextY < nextX) { y += stepY; nextY += deltaY; }
            else { x += stepX; y += stepY; nextX += deltaX; nextY += deltaY; }
        }
        return travelled;
    }

    private static bool ClipBeamPlane(Vector2 origin, Vector2 direction, Vector2 normal, float limit,
        ref float enter, ref float exit)
    {
        float position = Vector2.Dot(origin, normal) - limit;
        float velocity = Vector2.Dot(direction, normal);
        if (Math.Abs(velocity) < .000001f) return position <= 0;
        float time = -position / velocity;
        if (velocity < 0) enter = Math.Max(enter, time);
        else exit = Math.Min(exit, time);
        return enter <= exit;
    }
}
