using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Rosmontis
{
    [AutoloadEquip(EquipType.Body)]
    public class RosmontisBody : ArknightsVanityBody
    {
		public override int Rarity => 6;
		internal class RosmontisBackLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<RosmontisBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Sniper/Rosmontis/RosmontisBody_Back").Value;

				var offset = new Vector2(0, -3) + new Vector2(0, 0);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	} 
}
