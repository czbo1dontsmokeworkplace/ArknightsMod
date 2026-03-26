using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using ArknightsMod.Content.Players.InstancedSpace;
using ArknightsMod.Systems.InstancedSpace;

namespace ArknightsMod.Content.Tiles.Infrastructure.InstancedSpace
{
	public class ProtocolSpaceExitPortalTile : ModTile
	{
		public override string Texture => "ArknightsMod/Assets/Textures/InstancedSpace/ProtocolSpacePortal_Gap";
		public override string HighlightTexture => "ArknightsMod/Assets/Textures/InstancedSpace/ProtocolSpacePortal_HoverGap";

		public override void SetStaticDefaults()
		{
			Main.tileFrameImportant[Type] = true;
			Main.tileNoAttach[Type] = true;
			Main.tileLavaDeath[Type] = false;

			TileObjectData.newTile.CopyFrom(TileObjectData.Style3x3);
			TileObjectData.newTile.Origin = new Point16(1, 2);
			TileObjectData.newTile.CoordinateWidth = 16;
			TileObjectData.newTile.CoordinatePadding = 0;
			TileObjectData.newTile.CoordinateHeights = new[] { 16, 16, 16 };
			TileObjectData.addTile(Type);

			TileID.Sets.HasOutlines[Type] = true;
			AddMapEntry(new Color(120, 255, 200), Language.GetText("Mods.ArknightsMod.Tiles.ProtocolSpaceExitPortalTile.MapEntry"));
		}

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		public override bool RightClick(int i, int j)
		{
			if (!ProtocolSpaceEventSystem.IsEventCompleted)
				return true;

			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				Player mpPlayer = Main.LocalPlayer;
				if (mpPlayer != null && mpPlayer.active)
					mpPlayer.GetModPlayer<ProtocolSpacePlayer>().TryRequestExit();
				return true;
			}

			Player player = Main.LocalPlayer;
			if (player == null || !player.active)
				return true;
			player.GetModPlayer<ProtocolSpacePlayer>().TryRequestExit();
			return true;
		}
	}
}
