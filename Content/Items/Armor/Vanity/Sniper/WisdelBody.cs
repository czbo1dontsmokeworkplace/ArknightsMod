using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper;
using ArknightsMod.Content.Items.Armor.Vanity.Defender;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper
{
	[AutoloadEquip(EquipType.Body)]
	public class WisdelBody : ArknightsVanityBody
	{
		public override void SafeSetDefaults()
		{ 
			Item.hasVanityEffects = true;
		}
	}
}
public class WisdelWingLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
	public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
	{
		Item body = new(ModContent.ItemType<WisdelBody>());
		return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
	}

	protected override void Draw(ref PlayerDrawSet drawInfo) {

		Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Sniper/WisdelBody_Back").Value;

		Vector2 offset = new Vector2(1, -3);

		int drawX = (int)(drawInfo.drawPlayer.MountedCenter.X + offset.X * drawInfo.drawPlayer.direction - Main.screenPosition.X);
		int drawY = (int)(drawInfo.drawPlayer.MountedCenter.Y + offset.Y - Main.screenPosition.Y);
		int dyeShader = drawInfo.drawPlayer.dye?[1].dye ?? 0;
		float offsetY = 0;
		if ((drawInfo.drawPlayer.bodyFrame.Y >= 7 * drawInfo.drawPlayer.bodyFrame.Height &&
			drawInfo.drawPlayer.bodyFrame.Y <= 9 * drawInfo.drawPlayer.bodyFrame.Height) ||
			(drawInfo.drawPlayer.bodyFrame.Y >= 14 * drawInfo.drawPlayer.bodyFrame.Height &&
			drawInfo.drawPlayer.bodyFrame.Y <= 16 * drawInfo.drawPlayer.bodyFrame.Height)) {
			offsetY = -2;
		}
		drawInfo.DrawDataCache.Add(
			new DrawData(texture, new Vector2(drawX, drawY + offsetY + drawInfo.drawPlayer.gfxOffY),
			null, Color.White, 0f, texture.Size() * 0.5f, 1f, drawInfo.drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0)
			{
				shader = dyeShader
			});
	}
}
