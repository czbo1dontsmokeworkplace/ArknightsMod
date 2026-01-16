using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Provence
{
	[AutoloadEquip(EquipType.Body)]
	internal class ProvenceBody:ArknightsVanityBody
	{
		public override int Rarity => 5;
		internal class ProvenceBodyLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<ProvenceBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>
					("ArknightsMod/Content/Items/Armor/Vanity/Sniper/Provence/ProvenceBody_Tail").Value;

				var offset = new Vector2(-7, 12);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	}
}
