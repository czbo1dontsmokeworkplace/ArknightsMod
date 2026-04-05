using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using Terraria;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Specialist.ExusiaiAlter
{
    [AutoloadEquip(EquipType.Body)]
    public class ExusiaiAlterBody : ArknightsVanityBody
    {
		public override int Rarity => 6;
		internal class ExusiaiAlterWingLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<ExusiaiAlterBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Specialist/ExusiaiAlter/ExusiaiAlter_Wings").Value;

				var offset = new Vector2(1, -3) + new Vector2(-2, 8);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	} 
}
