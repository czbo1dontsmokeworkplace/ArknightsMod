using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Defender.Mudrock
{
	[AutoloadEquip(EquipType.Head)]
	internal class MudrockHelmet : ArknightsVanityHead {
		public override int Rarity => 6;
	}

	[AutoloadEquip(EquipType.Body)]
	internal class MudrockChestplate : ArknightsVanityBody
	{
		public override int Rarity => 6;
		internal class MudrockChestplate_EX : PlayerDrawLayer
		{
			private Asset<Texture2D> MudrockChestplate_EX_Texture;

			public override void Load()
				=> MudrockChestplate_EX_Texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Defender/Mudrock/MudrockChestplate_Body_EX");
			public override void Unload()
				=> MudrockChestplate_EX_Texture = null;
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<MudrockChestplate>());
				return drawInfo.drawPlayer.body == body.bodySlot;
			}
			public override Position GetDefaultPosition()
			=> new AfterParent(PlayerDrawLayers.Head);
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				Player player = drawInfo.drawPlayer;
				if (player.dead || player.invis)
					return;

				int BodyFrameIndex = player.bodyFrame.Y / player.bodyFrame.Height;
				Vector2 HeadgearOffset = Main.OffsetsPlayerHeadgear[BodyFrameIndex];
				Texture2D texture = MudrockChestplate_EX_Texture.Value;
				Vector2 position = drawInfo.Position - Main.screenPosition + new Vector2(player.width / 2 - player.bodyFrame.Width / 2, player.height - player.bodyFrame.Height + 4f) + player.bodyPosition;
				Vector2 origin = drawInfo.bodyVect;

				DrawData drawData = new(texture, position.Floor() + origin + HeadgearOffset + new Vector2(0, -2), texture.Frame(9, 4, 0, 1), drawInfo.colorArmorBody, player.bodyRotation, origin, 1f, drawInfo.playerEffect, 0) {
					shader = player.cBody
				};
				drawInfo.DrawDataCache.Add(drawData);
			}
		}
	}
	internal class MudrockItemChange : GlobalItem
	{
		public override void RightClick(Item item, Player player)
		{
			if (item.type == ModContent.ItemType<MudrockHelmet>() || item.type == ModContent.ItemType<MudrockHead>())
			{
				int targetType = item.type == ModContent.ItemType<MudrockHelmet>()
						? ModContent.ItemType<MudrockHead>()
						: ModContent.ItemType<MudrockHelmet>();

				item.ChangeItemType(targetType);
				item.stack++;
				return;
			}

			if (item.type == ModContent.ItemType<MudrockChestplate>() || item.type == ModContent.ItemType<MudrockBody>())
			{
				int targetType = item.type == ModContent.ItemType<MudrockChestplate>()
						? ModContent.ItemType<MudrockBody>()
						: ModContent.ItemType<MudrockChestplate>();

				item.ChangeItemType(targetType);
				item.stack++;
				return;
			}
		}
		public override bool CanRightClick(Item item)
		{
			if (item.type == ModContent.ItemType<MudrockHelmet>() || item.type == ModContent.ItemType<MudrockHead>()||
				item.type == ModContent.ItemType<MudrockChestplate>() || item.type == ModContent.ItemType<MudrockBody>()) {
				return true;
			}
			return base.CanRightClick(item);
		}
	}
}
