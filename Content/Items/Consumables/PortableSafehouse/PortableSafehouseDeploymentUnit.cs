using ArknightsMod.Systems;
using Microsoft.Xna.Framework;
using System.IO;
using Terraria;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Consumables.PortableSafehouse
{
	public class PortableSafehouseDeploymentUnit : ModItem
	{
		internal const string DeploySuccessKey = "Mods.ArknightsMod.Items.PortableSafehouseDeploymentUnit.Deploy.Success";
		internal const string DeployClientFailureKey = "Mods.ArknightsMod.Items.PortableSafehouseDeploymentUnit.Deploy.Failure.Client";
		internal const string DeployOutOfBoundsFailureKey = "Mods.ArknightsMod.Items.PortableSafehouseDeploymentUnit.Deploy.Failure.OutOfBounds";
		internal const string DeployChestFailureKey = "Mods.ArknightsMod.Items.PortableSafehouseDeploymentUnit.Deploy.Failure.Chest";
		internal const string DeployTileEntityFailureKey = "Mods.ArknightsMod.Items.PortableSafehouseDeploymentUnit.Deploy.Failure.TileEntity";
		internal const string DeployOreFailureKey = "Mods.ArknightsMod.Items.PortableSafehouseDeploymentUnit.Deploy.Failure.Ore";

		public override void SetDefaults()
		{
			Item.width = 40;
			Item.height = 40;
			Item.maxStack = 99;
			Item.consumable = true;
			Item.useStyle = ItemUseStyleID.HoldUp;
			Item.useTime = 30;
			Item.useAnimation = 30;
			Item.UseSound = SoundID.Item4;
			Item.noMelee = true;
			Item.rare = ItemRarityID.LightPurple;
		}

		public override bool? UseItem(Player player)
		{
			Point topLeft = PortableSafehouseSystem.GetDeploymentTopLeft();
			if (Main.netMode == NetmodeID.MultiplayerClient)
			{
				RequestDeploy(topLeft);
				return false;
			}

			if (Main.netMode == NetmodeID.Server)
				return false;

			if (!PortableSafehouseSystem.TryDeploy(topLeft, out _, out string failure))
			{
				Main.NewText(Language.GetTextValue(failure), Color.OrangeRed);
				return false;
			}

			Main.NewText(Language.GetTextValue(DeploySuccessKey), Color.LightGreen);
			return true;
		}

		private static void RequestDeploy(Point topLeft)
		{
			ModPacket packet = ModContent.GetInstance<global::ArknightsMod.ArknightsMod>().GetPacket();
			packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.PortableSafehouseRequestDeploy);
			packet.Write((short)topLeft.X);
			packet.Write((short)topLeft.Y);
			packet.Send();
		}

		internal static void ReceiveDeployRequest(BinaryReader reader, int whoAmI)
		{
			if (Main.netMode != NetmodeID.Server)
				return;

			Point topLeft = new(reader.ReadInt16(), reader.ReadInt16());
			if (whoAmI < 0 || whoAmI >= Main.maxPlayers)
				return;

			Player player = Main.player[whoAmI];
			if (player == null || !player.active || player.HeldItem.type != ModContent.ItemType<PortableSafehouseDeploymentUnit>() || player.HeldItem.stack <= 0)
				return;

			if (!PortableSafehouseSystem.TryDeploy(topLeft, out _, out string failure))
			{
				ChatHelper.SendChatMessageToClient(NetworkText.FromKey(failure), Color.OrangeRed, whoAmI);
				return;
			}

			player.HeldItem.stack--;
			if (player.HeldItem.stack <= 0)
				player.HeldItem.TurnToAir();
			NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, player.whoAmI, player.selectedItem);
			ChatHelper.SendChatMessageToClient(NetworkText.FromKey(DeploySuccessKey), Color.LightGreen, whoAmI);
		}
	}
}
