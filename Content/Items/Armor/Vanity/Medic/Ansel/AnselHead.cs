using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Microsoft.Xna.Framework;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Medic.Ansel
{
    [AutoloadEquip(EquipType.Head)]
    public class AnselHead : ArknightsVanityHead
    {
        public override int Rarity => 3;
        internal class AnselHeadLayer : PlayerDrawLayer
        {
            public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
            public override bool GetDefaultVisibility(PlayerDrawSet drawInfo)
            {
                Item head = new(ModContent.ItemType<AnselHead>());
                return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
            }
            protected override void Draw(ref PlayerDrawSet drawInfo)
            {
                Texture2D texture = ModContent.Request<Texture2D>
                    ("ArknightsMod/Content/Items/Armor/Vanity/Medic/Ansel/AnselHead_Ear").Value;

                var offset = new Vector2(0, -3);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
        }
    }
}
