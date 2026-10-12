using System.Linq;
using ArknightsMod.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace ArknightsMod.Content.Tiles.Natural.Endfield;

public abstract class EndfieldNaturalTile : ModTile
{
    public abstract int ResourceIndex { get; }
    public override string LocalizationCategory => "Tiles.EndfieldNatural";
    public override string Texture => $"ArknightsMod/Content/Tiles/Natural/Endfield/{EndfieldNaturalCatalog.Resources[ResourceIndex].Name}";

    public override void SetStaticDefaults()
    {
        var resource = EndfieldNaturalCatalog.Resources[ResourceIndex];
        Main.tileFrameImportant[Type] = true;
        Main.tileNoAttach[Type] = true;
        Main.tileSpelunker[Type] = true;
        Main.tileOreFinderPriority[Type] = (short)(210 + ResourceIndex % 6 * 10);

        TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
        TileObjectData.newTile.Width = resource.Width;
        TileObjectData.newTile.Height = resource.Height;
        TileObjectData.newTile.Origin = new Point16(resource.Width / 2 - 1, resource.Height - 1);
        TileObjectData.newTile.CoordinateWidth = 16;
        TileObjectData.newTile.CoordinatePadding = 2;
        TileObjectData.newTile.CoordinateHeights = Enumerable.Repeat(16, resource.Height).ToArray();
        TileObjectData.newTile.AnchorBottom = new AnchorData(
            AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, resource.Width, 0);
        // 原稿底部 4 行绿色像素是补地底座，2× 后占 8px。
        // 整体下移这 8px，使主体底端落在支撑地面顶端，底座延伸进地皮缝隙。
        // 保留底座贴图；已有自然采集物仍沿用各自的绘制偏移。
        TileObjectData.newTile.DrawYOffset = 8;
        TileObjectData.addTile(Type);

        Color mapColor = (ResourceIndex / 6) switch
        {
            0 => new Color(211, 131, 113),
            1 => new Color(158, 177, 105),
            _ => new Color(160, 165, 178)
        };
        AddMapEntry(mapColor, Language.GetText($"Mods.ArknightsMod.Tiles.EndfieldNatural.{Name}.MapEntry"));
        DustType = ResourceIndex >= 12 ? DustID.Stone : DustID.Grass;
        HitSound = ResourceIndex >= 12 ? SoundID.Tink : SoundID.Grass;
        MineResist = ResourceIndex >= 12 ? 1f : 0.5f;
        MinPick = 0;
        RegisterItemDrop(EndfieldNaturalCatalog.ItemType(ResourceIndex));
    }
}
