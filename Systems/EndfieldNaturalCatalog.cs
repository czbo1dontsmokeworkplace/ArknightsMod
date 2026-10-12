using System.Collections.Generic;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Material.Endfield;
using ArknightsMod.Content.Tiles.Natural.Endfield;

namespace ArknightsMod.Systems;

public static class EndfieldNaturalCatalog
{
    public readonly record struct Resource(string Name, int Width, int Height, int OrundumValue);
    public static readonly Resource[] Resources =
    [
        new("LightRedColumnFungus", 4, 2, 8),
        new("MediumRedColumnFungus", 4, 3, 12),
        new("HeavyRedColumnFungus", 4, 3, 18),
        new("BloodFungus", 3, 3, 28),
        new("StargateFungus", 2, 3, 36),
        new("TalosFungus", 2, 2, 45),
        new("CrystallizedMultiLeaf", 3, 2, 8),
        new("PureCrystalMultiLeaf", 3, 3, 12),
        new("PerfectCrystalMultiLeaf", 3, 3, 18),
        new("AngelicLeaf", 3, 3, 28),
        new("CorrodedJadeLeaf", 3, 3, 36),
        new("RedSpearLeaf", 2, 3, 45),
        new("LightUmbralStone", 3, 3, 10),
        new("MediumUmbralStone", 3, 3, 15),
        new("HeavyUmbralStone", 3, 4, 20),
        new("IgnitedStone", 2, 3, 30),
        new("WulingStone", 3, 3, 40),
        new("ProtocolPatternStone", 2, 3, 50),
    ];

    public static int ItemType(int index) => index switch
    {
        0 => ModContent.ItemType<LightRedColumnFungus>(),
        1 => ModContent.ItemType<MediumRedColumnFungus>(),
        2 => ModContent.ItemType<HeavyRedColumnFungus>(),
        3 => ModContent.ItemType<BloodFungus>(),
        4 => ModContent.ItemType<StargateFungus>(),
        5 => ModContent.ItemType<TalosFungus>(),
        6 => ModContent.ItemType<CrystallizedMultiLeaf>(),
        7 => ModContent.ItemType<PureCrystalMultiLeaf>(),
        8 => ModContent.ItemType<PerfectCrystalMultiLeaf>(),
        9 => ModContent.ItemType<AngelicLeaf>(),
        10 => ModContent.ItemType<CorrodedJadeLeaf>(),
        11 => ModContent.ItemType<RedSpearLeaf>(),
        12 => ModContent.ItemType<LightUmbralStone>(),
        13 => ModContent.ItemType<MediumUmbralStone>(),
        14 => ModContent.ItemType<HeavyUmbralStone>(),
        15 => ModContent.ItemType<IgnitedStone>(),
        16 => ModContent.ItemType<WulingStone>(),
        17 => ModContent.ItemType<ProtocolPatternStone>(),
        _ => throw new System.ArgumentOutOfRangeException(nameof(index))
    };

    public static int TileType(int index) => index switch
    {
        0 => ModContent.TileType<LightRedColumnFungusTile>(),
        1 => ModContent.TileType<MediumRedColumnFungusTile>(),
        2 => ModContent.TileType<HeavyRedColumnFungusTile>(),
        3 => ModContent.TileType<BloodFungusTile>(),
        4 => ModContent.TileType<StargateFungusTile>(),
        5 => ModContent.TileType<TalosFungusTile>(),
        6 => ModContent.TileType<CrystallizedMultiLeafTile>(),
        7 => ModContent.TileType<PureCrystalMultiLeafTile>(),
        8 => ModContent.TileType<PerfectCrystalMultiLeafTile>(),
        9 => ModContent.TileType<AngelicLeafTile>(),
        10 => ModContent.TileType<CorrodedJadeLeafTile>(),
        11 => ModContent.TileType<RedSpearLeafTile>(),
        12 => ModContent.TileType<LightUmbralStoneTile>(),
        13 => ModContent.TileType<MediumUmbralStoneTile>(),
        14 => ModContent.TileType<HeavyUmbralStoneTile>(),
        15 => ModContent.TileType<IgnitedStoneTile>(),
        16 => ModContent.TileType<WulingStoneTile>(),
        17 => ModContent.TileType<ProtocolPatternStoneTile>(),
        _ => throw new System.ArgumentOutOfRangeException(nameof(index))
    };
}
