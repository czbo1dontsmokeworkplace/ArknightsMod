using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Necrass
{
	[AutoloadEquip(EquipType.Head)]
	public class NecrassHead : NeoArmorHead
	{
		public override int Rarity => 5;
		public override int ArmorLifeBonus => 166;

		public override void Load() {
			if (Main.netMode == NetmodeID.Server)
				return;
		}

		public override void SetArmorDefaults() {
			Item.defense = 0;
		}

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<NecrassBody>() && body.neoarmor().hasUpgraded &&
				legs.type == ModContent.ItemType<NecrassLegs>() && legs.neoarmor().hasUpgraded;
		}

		public override void UpdateArmorSet(Player player) {
			player.setBonus = "";
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<NecrassHead>(1)
			.AddIngredient<Orundum>(50)
			.AddIngredient<OrirockConcentration>(10)
			.AddIngredient<LoxicKohl>(10)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		// 后发：叠加在头部帧动画下层的补充图层，与 NecrassHead_Head 共用同样的 28 帧竖排结构，
		// 按当前头部动画帧号同步取帧绘制。
		internal class NecrassHeadBackHairLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Head);

			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<NecrassHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {
				Player p = drawInfo.drawPlayer;

				Texture2D texture = ModContent.Request<Texture2D>
					("ArknightsMod/Content/Items/Armor/Caster/Necrass/NecrassHead_BackHair").Value;

				Rectangle headFrame = p.headFrame;
				if (headFrame.Width <= 0 || headFrame.Height <= 0)
					return;

				int frameIndex = headFrame.Y / headFrame.Height;
				Rectangle sourceRect = new(0, frameIndex * headFrame.Height, texture.Width, headFrame.Height);
				if (sourceRect.Bottom > texture.Height)
					return;

				Vector2 position = drawInfo.Position - Main.screenPosition + drawInfo.headVect + drawInfo.helmetOffset;
				Vector2 origin = new(sourceRect.Width * 0.5f, sourceRect.Height * 0.5f);

				SpriteEffects effects = p.direction == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
				if (p.gravDir == -1)
					effects |= SpriteEffects.FlipVertically;

				drawInfo.DrawDataCache.Add(
					new DrawData(texture, position, sourceRect, drawInfo.colorArmorHead, p.headRotation, origin, 1f, effects, 0) {
						shader = drawInfo.cHead
					});
			}
		}
	}
}
