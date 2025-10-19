using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;
using Terraria.ID;
using Terraria;
using Microsoft.Xna.Framework;
using Terraria.GameContent.Creative;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster.Mostima
{
    [AutoloadEquip(EquipType.Head)]
    public class MostimaHead : ArknightsVanityHead
    {
		public override int Rarity => 6;
		public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MostimaBody>() && legs.type == ModContent.ItemType<MostimaLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
        }
		internal class MostimaHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<MostimaHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				var texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Caster/Mostima/MostimaHead_Ring").Value;
				var offset = new Vector2(0, -3) + new Vector2(2, -28);

				int drawX = (int)(drawInfo.drawPlayer.MountedCenter.X + offset.X * drawInfo.drawPlayer.direction - Main.screenPosition.X);
				int drawY = (int)(drawInfo.drawPlayer.MountedCenter.Y + offset.Y - Main.screenPosition.Y);
				int dyeShader = drawInfo.drawPlayer.dye?[0].dye ?? 0;
				float offsetY = 0;
				if (drawInfo.drawPlayer.bodyFrame.Y >= 7 * drawInfo.drawPlayer.bodyFrame.Height &&
					drawInfo.drawPlayer.bodyFrame.Y <= 9 * drawInfo.drawPlayer.bodyFrame.Height ||
					drawInfo.drawPlayer.bodyFrame.Y >= 14 * drawInfo.drawPlayer.bodyFrame.Height &&
					drawInfo.drawPlayer.bodyFrame.Y <= 16 * drawInfo.drawPlayer.bodyFrame.Height) {
					offsetY = -2;
				}
				drawInfo.DrawDataCache.Add(
					new DrawData(texture, new Vector2(drawX, drawY + offsetY + drawInfo.drawPlayer.gfxOffY),
					null, drawInfo.colorArmorBody, 0f, texture.Size() * 0.5f, 1f, drawInfo.drawPlayer.direction == 1 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0) {
						shader = dyeShader
					});
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	} 
}
