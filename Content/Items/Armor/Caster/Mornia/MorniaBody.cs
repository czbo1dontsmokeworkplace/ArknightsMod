using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.Tiles.Infrastructure;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Caster.Mornia
{
	[AutoloadEquip(EquipType.Body)]
	public class MorniaBody : NeoArmorBody
	{
		public override int Rarity => 4;
		public override int ArmorLifeBonus => 70;

		public override void SetArmorDefaults() {
			Item.defense = 14;
		}

		public override void AddRecipes() {
			CreateRecipe()
			.AddIngredient<MorniaBody>(1)
			.AddIngredient<Orundum>(40)
			.AddIngredient<ManganeseTrihydrate>(3)
			.AddIngredient<IntegratedDevice>(1)
			.AddTile(ModContent.TileType<FactoryTile>())
			.AddCondition(NeoArmorUtils.NeedVanity)
			.DisableDecraft()
			.Register();
		}

		internal class MorniaBodyLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<MorniaBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {
				Texture2D texture = ModContent.Request<Texture2D>
					("ArknightsMod/Content/Items/Armor/Caster/Mornia/MorniaBody_Tail").Value;

				var offset = new Vector2(-7, 12);

				int drawX = (int)(drawInfo.drawPlayer.MountedCenter.X + offset.X * drawInfo.drawPlayer.direction - Main.screenPosition.X);
				int drawY = (int)(drawInfo.drawPlayer.MountedCenter.Y + (int)drawInfo.drawPlayer.gravDir * offset.Y - Main.screenPosition.Y);
				int dyeShader = drawInfo.drawPlayer.dye?[0].dye ?? 0;
				float offsetY = 0;
				if (drawInfo.drawPlayer.bodyFrame.Y >= 7 * drawInfo.drawPlayer.bodyFrame.Height &&
					drawInfo.drawPlayer.bodyFrame.Y <= 9 * drawInfo.drawPlayer.bodyFrame.Height ||
					drawInfo.drawPlayer.bodyFrame.Y >= 14 * drawInfo.drawPlayer.bodyFrame.Height &&
					drawInfo.drawPlayer.bodyFrame.Y <= 16 * drawInfo.drawPlayer.bodyFrame.Height) {
					offsetY = -2;
				}
				int bodyframe = drawInfo.drawPlayer.bodyFrame.Y / drawInfo.drawPlayer.bodyFrame.Height;
				Rectangle sourceRect = new(0, bodyframe * (18 + 38) + 33, texture.Width, 18);
				Vector2 origin = sourceRect.Size() / 2;
				float h_pi = MathHelper.Pi / 2;
				float r = h_pi - h_pi * drawInfo.drawPlayer.gravDir;
				drawInfo.DrawDataCache.Add(
					new DrawData(texture, new Vector2(drawX, drawY + offsetY + drawInfo.drawPlayer.gfxOffY),
					sourceRect, drawInfo.colorArmorBody, r, origin, 1f,
					drawInfo.drawPlayer.gravDir * drawInfo.drawPlayer.direction == 1
						? SpriteEffects.None
						: SpriteEffects.FlipHorizontally, 0) {
						shader = dyeShader
					});
			}
		}
	}
}
