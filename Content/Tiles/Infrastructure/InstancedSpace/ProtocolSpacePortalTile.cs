using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent.ObjectInteractions;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ObjectData;
using ArknightsMod.Systems.InstancedSpace;

namespace ArknightsMod.Content.Tiles.Infrastructure.InstancedSpace
{
	public class ProtocolSpacePortalTile : ModTile
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
			RegisterItemDrop(ModContent.ItemType<global::ArknightsMod.Content.Items.Placeable.Infrastructure.InstancedSpace.ProtocolSpacePortal>());
			AddMapEntry(new Color(160, 90, 255), Language.GetText("Mods.ArknightsMod.Tiles.ProtocolSpacePortalTile.MapEntry"));
		}

		public override bool CanKillTile(int i, int j, ref bool blockDamaged) => !ProtocolSpaceEventSystem.IsEventActive;

		public override bool CanExplode(int i, int j) => !ProtocolSpaceEventSystem.IsEventActive;

		public override bool HasSmartInteract(int i, int j, SmartInteractScanSettings settings) => true;

		public override bool RightClick(int i, int j)
		{
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				ProtocolSpaceEventSystem.RequestStartFromClient(i, j);
				return true;
			}

			Player player = Main.LocalPlayer;
			if (player == null || !player.active)
				return true;
			ProtocolSpaceEventSystem.StartForPlayer(player);
			return true;
		}
	}
}
