using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionQuadrantRules
{
    internal static (int First, int Second) Pair(int first, int other)
    {
        first = Math.Clamp(first, 0, 3); other = Math.Clamp(other, 0, 2);
        return (first, other >= first ? other + 1 : other);
    }
    internal static Vector2 Offset(int quadrant, float x, float y) =>
        new((quadrant % 2 == 0 ? -1 : 1) * x, (quadrant < 2 ? -1 : 1) * y);
    internal static Vector2 TurnBattery(Vector2 direction, Vector2 origin, Vector2 target) =>
        direction.ToRotation().AngleTowards((target - origin).ToRotation(), MathHelper.ToRadians(5)).ToRotationVector2();
    internal static bool BatteryFire(int age, float boost)
    {
        int previous = (int)MathF.Floor((age - 61) * boost), now = (int)MathF.Floor((age - 60) * boost);
        for (int tick = Math.Max(0, previous + 1); tick <= now; tick++)
        {
            int beat = tick % 174;
            if (beat >= 20 && beat <= 110 && (beat - 20) % 18 == 0) return true;
        }
        return false;
    }
}

public sealed partial class Evolution
{
    private void SpawnQuadrantBatteries(Player player)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var pair = EvolutionQuadrantRules.Pair(Main.rand.Next(4), Main.rand.Next(3));
        foreach (int quadrant in new[] { pair.First, pair.Second })
            SpawnBrood(EvolutionBrood.Puppet, player.Center + EvolutionQuadrantRules.Offset(quadrant,
                Main.rand.NextFloat(480, 680), Main.rand.NextFloat(260, 440)));
    }
    private void ThrowQuadrantBombs(Vector2 center)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        var pair = EvolutionQuadrantRules.Pair(Main.rand.Next(4), Main.rand.Next(3));
        foreach (int quadrant in new[] { pair.First, pair.Second })
        for (int index = 0; index < 2; index++)
        {
            Vector2 offset = EvolutionQuadrantRules.Offset(quadrant, Main.rand.NextFloat(300 + index * 160, 440 + index * 160),
                Main.rand.NextFloat(200 + index * 100, 340 + index * 100));
            ThrowBomb(center + offset * 1.25f, center + offset, fragments: true, fuse: 74 + index * 8);
        }
    }
}
