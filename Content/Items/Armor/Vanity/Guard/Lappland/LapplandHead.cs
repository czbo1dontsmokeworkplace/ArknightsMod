using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Lappland
{
	[AutoloadEquip(EquipType.Head)]
	internal class LapplandHead:ArknightsVanityHead
	{
		public override int Rarity => 5;
		internal class LapplandHeadLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<LapplandHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>
					("ArknightsMod/Content/Items/Armor/Vanity/Guard/Lappland/Lappland_BackHair").Value;

				var offset = new Vector2(0, -3);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
		}
	}
}
