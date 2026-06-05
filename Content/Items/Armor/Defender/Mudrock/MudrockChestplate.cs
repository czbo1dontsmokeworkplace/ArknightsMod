using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Defender.Mudrock
{
	public class MudrockChestplate : MudrockSetBodyPiece
	{
		public override int Rarity => 6;
		protected override string VanityItemName => nameof(MudrockBody);
		protected override int VanityItemType => ModContent.ItemType<MudrockBody>();

		protected override void SetSetDefaults() {
			Item.defense = 0;
		}
	}

	internal class MudrockChestplateDrawLayer : PlayerDrawLayer
	{
		private Asset<Texture2D> texture;

		public override void Load() {
			texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Defender/Mudrock/MudrockChestplate_Body_EX");
		}

		public override void Unload() {
			texture = null;
		}

		public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
			Item body = new(ModContent.ItemType<MudrockChestplate>());
			return drawInfo.drawPlayer.body == body.bodySlot;
		}

		public override Position GetDefaultPosition() => new AfterParent(PlayerDrawLayers.Head);

		protected override void Draw(ref PlayerDrawSet drawInfo) {
			Player player = drawInfo.drawPlayer;
			if (player.dead || player.invis)
				return;

			int bodyFrameIndex = player.bodyFrame.Y / player.bodyFrame.Height;
			Vector2 headgearOffset = Main.OffsetsPlayerHeadgear[bodyFrameIndex];
			Texture2D tex = texture.Value;
			Vector2 position = drawInfo.Position - Main.screenPosition
				+ new Vector2(player.width / 2 - player.bodyFrame.Width / 2, player.height - player.bodyFrame.Height + 4f)
				+ player.bodyPosition;
			Vector2 origin = drawInfo.bodyVect;

			DrawData drawData = new(tex, position.Floor() + origin + headgearOffset + new Vector2(0, -2),
				tex.Frame(9, 4, 0, 1), drawInfo.colorArmorBody, player.bodyRotation, origin, 1f, drawInfo.playerEffect, 0) {
				shader = player.cBody
			};
			drawInfo.DrawDataCache.Add(drawData);
		}
	}
}
