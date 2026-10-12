using System;
using System.Collections.Generic;
using ArknightsMod.Common;
using Terraria;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.Utilities;
using static ArknightsMod.Systems.EndfieldNaturalHabitatRules;

namespace ArknightsMod.Systems;

public sealed class EndfieldNaturalGrowthSystem : ModSystem
{
    private const int RefreshIntervalTicks = 300;
    private const int RefreshSamples = 16;

    public override void OnWorldLoad() => RareCollectibleVisuals.ClearSparkleCache();
    public override void OnWorldUnload() => RareCollectibleVisuals.ClearSparkleCache();

    public override void PostWorldGen()
    {
        // 只在实际的露天地面/洞穴地面尝试，不挖开方块或覆盖已有多格物体。
        for (int x = 20; x < Main.maxTilesX - 24; x++)
            for (int y = 20; y < Main.maxTilesY - 20; y++)
                if (Main.tile[x, y].HasTile && !Main.tile[x, y - 1].HasTile && GroundAt(x, y) != Ground.Other)
                    TryGenerateAtGround(x, y, WorldGen.genRand);
    }

    public override void PostUpdateWorld()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Main.GameUpdateCount % RefreshIntervalTicks != 0)
            return;

        for (int p = 0; p < Main.maxPlayers; p++)
        {
            Player player = Main.player[p];
            if (!player.active || player.dead)
                continue;
            int px = (int)(player.Center.X / 16);
            int py = (int)(player.Center.Y / 16);
            for (int n = 0; n < RefreshSamples; n++)
            {
                int x = px + Main.rand.Next(-80, 81);
                int y = py + Main.rand.Next(-50, 51);
                if (!WorldGen.InWorld(x, y, 20))
                    continue;
                for (int dy = 0; dy < 8 && WorldGen.InWorld(x, y + dy, 20); dy++)
                {
                    if (!Main.tile[x, y + dy].HasTile)
                        continue;
                    if (!Main.tile[x, y + dy - 1].HasTile)
                        TryGenerateAtGround(x, y + dy, Main.rand);
                    break;
                }
            }
        }
    }

    // 供现有 /plant 使用；自然生成不通过 /plant 的“尽可能种满”概率处理。
    public static IEnumerable<NaturalGrowthSystem.Entry> GrowthEntries()
    {
        for (int index = 0; index < EndfieldNaturalCatalog.Resources.Length; index++)
        {
            int resourceIndex = index;
            var resource = EndfieldNaturalCatalog.Resources[index];
            yield return new NaturalGrowthSystem.Entry(EndfieldNaturalCatalog.TileType(index), resource.Width, resource.Height,
                (x, top) => CanOccupy(resourceIndex, x, top + resource.Height));
        }
    }

    public static bool TryGenerateAtGround(int x, int y, UnifiedRandom random, bool ignoreChance = false)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !WorldGen.InWorld(x, y, 20))
            return false;

        Span<double> chances = stackalloc double[18];
        chances.Clear();
        double sum = 0;
        for (int index = 0; index < chances.Length; index++)
        {
            if (!CanOccupy(index, x, y))
                continue;
            chances[index] = ChanceAt(index, x, y);
            sum += chances[index];
        }
        if (sum == 0)
            return false;
        double roll = random.NextDouble() * (ignoreChance ? sum : 1);
        for (int index = 0; index < chances.Length; index++)
        {
            roll -= chances[index];
            if (roll >= 0 || chances[index] == 0)
                continue;
            return PlaceResource(index, x, y);
        }
        return false;
    }

    // 测试模式允许任意实心平地及室内背景墙，但仍不覆盖物体或清除液体。
    public static bool TryPlaceResourceAtGround(int index, int x, int groundY, bool bypassHabitat = false)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || index < 0
            || index >= EndfieldNaturalCatalog.Resources.Length || !CanOccupy(index, x, groundY, bypassHabitat))
            return false;
        return PlaceResource(index, x, groundY);
    }

    private static bool PlaceResource(int index, int x, int groundY)
    {
        var resource = EndfieldNaturalCatalog.Resources[index];
        int tileType = EndfieldNaturalCatalog.TileType(index);
        TileObjectData data = TileObjectData.GetTileData(tileType, 0);
        int top = groundY - resource.Height;
        bool placed = WorldGen.PlaceObject(x + data.Origin.X, top + data.Origin.Y, tileType, mute: true);
        if (placed && Main.netMode == NetmodeID.Server && !WorldGen.gen)
            NetMessage.SendTileSquare(-1, x, top, resource.Width, resource.Height);
        return placed;
    }

    private static bool CanOccupy(int index, int x, int groundY, bool bypassHabitat = false)
    {
        var resource = EndfieldNaturalCatalog.Resources[index];
        int top = groundY - resource.Height;
        if (!WorldGen.InWorld(x, top, 20) || !WorldGen.InWorld(x + resource.Width - 1, groundY, 20))
            return false;
        for (int dx = 0; dx < resource.Width; dx++)
        {
            Tile anchor = Main.tile[x + dx, groundY];
            if (!anchor.HasTile || anchor.IsActuated || anchor.IsHalfBlock || anchor.Slope != SlopeType.Solid
                || (bypassHabitat ? !Main.tileSolid[anchor.TileType] : ChanceAt(index, x + dx, groundY) == 0))
                return false;
            for (int dy = 0; dy < resource.Height; dy++)
            {
                Tile space = Main.tile[x + dx, top + dy];
                if (space.HasTile || space.LiquidAmount > 0 || (!bypassHabitat && Main.wallHouse[space.WallType]))
                    return false;
            }
        }
        return true;
    }

    private static double ChanceAt(int index, int x, int y)
    {
        Depth depth = y < Main.worldSurface * 0.35 ? Depth.Space
            : y < Main.worldSurface ? Depth.Surface
            : y < Main.rockLayer ? Depth.Underground
            : y < Main.UnderworldLayer ? Depth.Caverns : Depth.Mantle;
        // 普通地表草皮视作森林；世界两侧海滩不作为森林或沙漠的优先生境。
        bool inland = x >= 250 && x < Main.maxTilesX - 250;
        return SpawnChance(index, GroundAt(x, y), depth, inland);
    }

    private static Ground GroundAt(int x, int y) => Main.tile[x, y].TileType switch
    {
        TileID.Grass => Ground.Grass,
        TileID.CorruptGrass => Ground.CorruptGrass,
        TileID.CrimsonGrass => Ground.CrimsonGrass,
        TileID.HallowedGrass => Ground.HallowedGrass,
        TileID.JungleGrass => Ground.JungleGrass,
        TileID.Stone => Ground.Stone,
        TileID.Sand => Ground.Sand,
        _ => Ground.Other
    };
}
