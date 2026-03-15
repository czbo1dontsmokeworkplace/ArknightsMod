using StructureHelper.Models;
using SubworldLibrary;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.IO;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.WorldBuilding;

namespace ArknightsMod.Subworlds.RhodesIsland;

public class RhodesIslandSubworld : Subworld
{
	private const int GroundHeight = 200;

	public override int Width => 1200;
	public override int Height => 500;

	public override List<GenPass> Tasks =>
	[
		new StartGenPass(),
		new GroundGenPass(),
		new LandshipGenPass(),
		new EndGenPass()
	];

	public override void OnEnter()
	{
		SubworldSystem.hideUnderworld = true;
	}

	private class StartGenPass() : GenPass("Start", 1)
	{
		protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
		{
			progress.Message = Language.GetTextValue("Mods.ArknightsMod.Subworlds.RhodesIsland.GenProgress.1");
			progress.Set(0f);

			// Temporary fix for Subworld Library issue.
			WorldGen.generatingWorld = true;

			Main.worldSurface = Main.maxTilesY - GroundHeight;

			Main.rockLayer = Main.maxTilesY;

			Main.spawnTileX = Main.maxTilesX /2;
			Main.spawnTileY = (Main.maxTilesY - GroundHeight) - 12;

			progress.Set(1f);
		}
	}

	private class GroundGenPass() : GenPass("Ground", 4)
	{
		protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
		{
			progress.Message = Language.GetTextValue("Mods.ArknightsMod.Subworlds.RhodesIsland.GenProgress.2");

			// Fill the entire map with stone blocks
			for (int x = 0; x < Main.maxTilesX; x++)
			{
				for (int y = Main.maxTilesY - GroundHeight; y < Main.maxTilesY; y++)
				{
					// Control the progress bar, should be set between 0f and 1f
					progress.Set(((y / 2f) + x * Main.maxTilesY) / (Main.maxTilesX * (Main.maxTilesY - GroundHeight)));

					// Fill tiles
					Tile tile = Main.tile[x, y];
					tile.HasTile = true;
					tile.TileType = TileID.Stone;
				}
			}
		}
	}

	private class LandshipGenPass() : GenPass("Landship", 4)
	{
		protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
		{
			progress.Message = Language.GetTextValue("Mods.ArknightsMod.Subworlds.RhodesIsland.GenProgress.3");

			StructureData landship = StructureHelper.API.Generator.GetStructureData(
				"Subworlds/RhodesIsland/Structures/Landship",
				ModContent.GetInstance<ArknightsMod>());

			Point16 position = new(
				Main.maxTilesX / 2 - landship.width / 2,
				Main.maxTilesY - GroundHeight - landship.height);

			StructureHelper.API.Generator.GenerateFromData(landship, position);
		}
	}

	private class EndGenPass() : GenPass("End", 1)
	{
		protected override void ApplyPass(GenerationProgress progress, GameConfiguration configuration)
		{
			progress.Message = Language.GetTextValue("Mods.ArknightsMod.Subworlds.RhodesIsland.GenProgress.4");

			// Temporary fix for Subworld Library issue.
			WorldGen.generatingWorld = false;
		}
	}
}