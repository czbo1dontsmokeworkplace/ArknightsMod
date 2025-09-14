using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Items.Armor.Vanity.Sniper;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper
{
	[AutoloadEquip(EquipType.Body)]
	public class WisdelBody : ModItem
	{
		public override void SetStaticDefaults() {
			Item.ResearchUnlockCount = 1;
			if (Main.netMode == NetmodeID.Server)
				return;
			ArmorIDs.Body.Sets.HidesTopSkin[Item.bodySlot] = true;
			ArmorIDs.Body.Sets.HidesArms[Item.bodySlot] = true;
		}

		public override void SetDefaults() {
			Item.width = 28;
			Item.height = 24;
			Item.rare = ItemRarityID.LightPurple;
			Item.vanity = true;
			Item.hasVanityEffects = true;
		}

		public override void UpdateEquip(Player player)
		{
			player.GetModPlayer<WisdelBodyPlayer>().wisdel_back = true;
		}
		public override void UpdateVanitySet(Player player)
		{
			player.GetModPlayer<WisdelBodyPlayer>().wisdel_back = true;
		}
		public override bool IsVanitySet(int head, int body, int legs) {
			return body == EquipLoader.GetEquipSlot(Mod, Name, EquipType.Body) &&
				head == EquipLoader.GetEquipSlot(Mod, "WisdelHead", EquipType.Head) &&
				legs == EquipLoader.GetEquipSlot(Mod, "WisdelLegs", EquipType.Legs);
		}
		public override void UpdateVanity(Player player)
		{
			player.GetModPlayer<WisdelBodyPlayer>().wisdel_back = true;
		}
	}
	public class WisdelBodyPlayer : ModPlayer
	{
		public bool wisdel_back = false;
		public override void ResetEffects() {
			wisdel_back = false;
		}
	}
}
public class WisdelWingLayer : PlayerDrawLayer
{
	public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
	public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) => drawInfo.drawPlayer.GetModPlayer<WisdelBodyPlayer>().wisdel_back && !drawInfo.drawPlayer.dead;

	protected override void Draw(ref PlayerDrawSet drawInfo) {

		Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Sniper/WisdelBody_Back").Value;

		Vector2 offset = new Vector2(1, -3);

		int drawX = (int)(drawInfo.drawPlayer.MountedCenter.X + offset.X * drawInfo.drawPlayer.direction - Main.screenPosition.X);
		int drawY = (int)(drawInfo.drawPlayer.MountedCenter.Y + offset.Y - Main.screenPosition.Y);
		
		float offsetY = 0;
		if ((drawInfo.drawPlayer.bodyFrame.Y >= 7 * drawInfo.drawPlayer.bodyFrame.Height &&
			drawInfo.drawPlayer.bodyFrame.Y <= 9 * drawInfo.drawPlayer.bodyFrame.Height) ||
			(drawInfo.drawPlayer.bodyFrame.Y >= 14 * drawInfo.drawPlayer.bodyFrame.Height &&
			drawInfo.drawPlayer.bodyFrame.Y <= 16 * drawInfo.drawPlayer.bodyFrame.Height)) {
			offsetY = -2;
		}
		drawInfo.DrawDataCache.Add(new DrawData(texture, new Vector2(drawX, drawY + offsetY + drawInfo.drawPlayer.gfxOffY),
			null, Color.White, 0f,
			texture.Size() * 0.5f, 1f,
				drawInfo.drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0));
	}
}
