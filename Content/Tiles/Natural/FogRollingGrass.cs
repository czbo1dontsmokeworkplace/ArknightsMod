using ArknightsMod.Common;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace ArknightsMod.Content.Tiles.Natural
{
	// 捕获后的雾滚草可作为摆设。复用物品贴图，不改变野外飘动的 Critter NPC。
	public class FogRollingGrass : ModTile
	{
		public override string Texture => "ArknightsMod/Content/Items/Material/FogRollingGrass";

		public override void SetStaticDefaults() {
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style1x1);
			TileObjectData.newTile.CoordinateWidth = 28;
			TileObjectData.newTile.CoordinateHeights = new[] { 28 };
			TileObjectData.newTile.CoordinatePadding = 0;
			TileObjectData.newTile.AnchorBottom = new AnchorData(AnchorType.SolidTile | AnchorType.SolidWithTop | AnchorType.SolidSide, 1, 0);
			TileObjectData.newTile.DrawXOffset = -6;
			TileObjectData.newTile.DrawYOffset = -10;
			TileObjectData.addTile(Type);

			AddMapEntry(new Color(160, 170, 160), Language.GetText("Mods.ArknightsMod.Tiles.FogRollingGrass.MapEntry"));
			DustType = DustID.Smoke;
			HitSound = SoundID.Grass;
			RegisterItemDrop(ModContent.ItemType<Items.Material.FogRollingGrass>());
		}

		public override void RandomUpdate(int i, int j) {
			RareCollectibleVisuals.EmitAmbientSparkle(i, j, 1, 1);
		}
	}
}
