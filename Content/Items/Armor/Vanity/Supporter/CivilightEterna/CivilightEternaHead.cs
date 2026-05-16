using ArknightsMod.Common;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Supporter.CivilightEterna
{
	[AutoloadEquip(EquipType.Head)]
	public class CivilightEternaHead: ArknightsVanityHead
	{
		public override int Rarity => 6;

		public override bool IsArmorSet(Item head, Item body, Item legs) {
			return body.type == ModContent.ItemType<CivilightEternaBody>() && legs.type == ModContent.ItemType<CivilightEternaLegs>();
		}
		internal class CivilightEternaHeadLayer : PlayerDrawLayer
		{
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<CivilightEternaHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}

			protected override void Draw(ref PlayerDrawSet drawInfo) {

				Texture2D texture = ModContent.Request<Texture2D>
					("ArknightsMod/Content/Items/Armor/Vanity/Supporter/CivilightEterna/CivilightEternaHead_BackHair").Value;

				var offset = new Vector2(0, -3);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
		}
	}
}
