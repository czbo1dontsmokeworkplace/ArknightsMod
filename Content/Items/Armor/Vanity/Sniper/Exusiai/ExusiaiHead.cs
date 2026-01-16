using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Exusiai
{
    [AutoloadEquip(EquipType.Head)]
    public class ExusiaiHead : ArknightsVanityHead
    {
		public override int Rarity => 6;
		public override void UpdateEquip(Player player)
        {
            Lighting.AddLight(player.Center, new Vector3(1f, 1f, 1f));
        }
        public override void UpdateVanity(Player player)
        {
            Lighting.AddLight(player.Center, new Vector3(1f, 1f, 1f));
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ExusiaiBody>() && legs.type == ModContent.ItemType<ExusiaiLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "No party no life";
        }
		internal class ExusiaiHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<ExusiaiHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Sniper/Exusiai/ExusiaiHead_Ring").Value;

				var offset = new Vector2(1, -3) + new Vector2(0, -26);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	} 
}
