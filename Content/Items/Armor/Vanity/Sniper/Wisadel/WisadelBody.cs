using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Wisadel
{
	[AutoloadEquip(EquipType.Body)]
	public class WisadelBody : ArknightsVanityBody
	{
		public override int Rarity => 6;
		public override void SafeSetDefaults()
		{ 
			Item.hasVanityEffects = true;
		}
		internal class WisadelWingLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.Wings);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item body = new(ModContent.ItemType<WisadelBody>());
				return drawInfo.drawPlayer.body == body.bodySlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Sniper/Wisadel/WisadelBody_Back").Value;

				var offset = new Vector2(1, -3);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 1, offset);
			}
		}
	}
}

