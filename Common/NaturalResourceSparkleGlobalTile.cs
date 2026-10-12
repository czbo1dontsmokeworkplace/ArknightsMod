using ArknightsMod.Content.Tiles.Natural.Endfield;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace ArknightsMod.Common;

// EmitParticles 是客户端屏幕绘制阶段的粒子钩子，不依赖服务器随机地块更新。
public sealed class NaturalResourceSparkleGlobalTile : GlobalTile
{
    public override void EmitParticles(int i, int j, Tile tile, ushort type,
        short tileFrameX, short tileFrameY, Color tileLight, bool visible)
    {
        if (!visible || Main.dedServ || Main.netMode == NetmodeID.Server)
            return;
        ModTile modTile = TileLoader.GetTile(type);
        if (modTile is null)
            return;
        bool collectible = modTile is EndfieldNaturalTile
            || modTile is Content.Tiles.BloodMushroom
            || modTile is Content.Tiles.Natural.BoardVine
            || modTile is Content.Tiles.Natural.EchoCorn
            || modTile is Content.Tiles.Natural.FrostCrystalTree
            || modTile is Content.Tiles.Natural.GlowingTruffle
            || modTile is Content.Tiles.Natural.HomesickFruit
            || modTile is Content.Tiles.Natural.ProliferatingMoss
            || modTile is Content.Tiles.Natural.WaveSpray
            || modTile is Content.Tiles.Natural.WitheredMossBall
            || modTile is Content.Tiles.Natural.FogRollingGrass
            || modTile is Content.Tiles.Grindstone
            || modTile is Content.Tiles.ManganeseOre
            || modTile is Content.Tiles.OrirockCube
            || modTile is Content.Tiles.RMA7012;
        if (!collectible)
            return;

        TileObjectData data = TileObjectData.GetTileData(tile);
        if (data is null)
            RareCollectibleVisuals.EmitAmbientSparkle(i, j, 1, 1);
        else
        {
            var origin = TileObjectData.TopLeft(i, j);
            RareCollectibleVisuals.EmitAmbientSparkle(origin.X, origin.Y, data.Width, data.Height);
        }
    }
}
