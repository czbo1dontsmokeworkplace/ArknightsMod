using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Vanguard.Bagpipe
{
	[AutoloadEquip(EquipType.Head)]
	public class BagpipeHead : ArknightsVanityHead
	{
		public override int Rarity => 6;
		internal class BagpipeHeadLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<BagpipeHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>
					("ArknightsMod/Content/Items/Armor/Vanity/Vanguard/Bagpipe/BagpipeHead_BackHair").Value;

				var offset = new Vector2(0, -3);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
		}
	}
}
