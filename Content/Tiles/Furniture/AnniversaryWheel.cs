using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Items.Material.T2;
using ArknightsMod.Content.Items.Material.T3;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;

namespace ArknightsMod.Content.Tiles.Furniture
{
	public class AnniversaryWheel : ModTile
	{
		public const int NextStyleHeight = 42; // Calculated by adding all CoordinateHeights{16, 18} + CoordinatePaddingFix.Y{2} applied to all of them + 2

		public override void SetStaticDefaults() {
			// Properties
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;
			TileID.Sets.DisableSmartCursor[Type] = true;

			// DustType = ModContent.DustType<Sparkle>();

			// AdjTiles = new int[] { TileID.Chairs };

			// Names
			LocalizedText name = CreateMapEntryName();
			AddMapEntry(new Color(183, 57, 76), name);

			// Placement
			TileObjectData.newTile.CopyFrom(TileObjectData.Style2x2);
			// TileObjectData.newTile.Direction = TileObjectDirection.PlaceLeft;

			// The following 3 lines are needed if you decide to add more styles and stack them vertically
			// TileObjectData.newTile.StyleWrapLimit = 2;
			// TileObjectData.newTile.StyleMultiplier = 2;
			// TileObjectData.newTile.StyleHorizontal = true;

			// TileObjectData.newAlternate.CopyFrom(TileObjectData.newTile);
			// TileObjectData.newAlternate.Direction = TileObjectDirection.PlaceRight;
			// TileObjectData.addAlternate(1); // Facing right will use the second texture style
			TileObjectData.addTile(Type);
		}

		//public override void NumDust(int i, int j, bool fail, ref int num) {
		//	num = fail ? 1 : 3;
		//}

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) {
			return settings.player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance); // Avoid being able to trigger it from long range
		}

		public override bool RightClick(int i, int j) {
			Player player = Main.LocalPlayer;

			if (player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance)) { // Avoid being able to trigger it from long range
				player.GamepadEnableGrappleCooldown();
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<Orundum>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<OrirockCube>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<Grindstone>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<ManganeseOre>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<RMA7012>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<Device>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<LoxicKohl>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<Oriron>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<Polyketon>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<Sugar>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<Polyester>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<CoagulatingGel>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<IncandescentAlloy>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<CrystallineComponent>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<CompoundCuttingFluid>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<SemiSyntheticSolvent>(), 100);
				player.QuickSpawnItem(new EntitySource_TileBreak(i, j), ModContent.ItemType<TransmutedSalt>(), 100);
			}

			return true;
		}

		public override void MouseOver(int i, int j) {
			Player player = Main.LocalPlayer;

			if (!player.IsWithinSnappngRangeToTile(i, j, PlayerSittingHelper.ChairSittingMaxDistance)) { // Match condition in RightClick. Interaction should only show if clicking it does something
				return;
			}

			player.noThrow = 2;
			player.cursorItemIconEnabled = true;
			player.cursorItemIconID = ModContent.ItemType<Items.Placeable.Furniture.AnniversaryWheel>();

			//if (Main.tile[i, j].TileFrameX / 18 < 1) {
			//	player.cursorItemIconReversed = true;
			//}
		}
	}
}
