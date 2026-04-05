using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Specialist.ExusiaiAlter
{
    [AutoloadEquip(EquipType.Head)]
    public class ExusiaiAlterHead : ArknightsVanityHead
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
            return body.type == ModContent.ItemType<ExusiaiAlterBody>() && legs.type == ModContent.ItemType<ExusiaiAlterLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "";
        }
		internal class ExusiaiAlterHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<ExusiaiAlterHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				Texture2D texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Specialist/ExusiaiAlter/ExusiaiAlter_Ring").Value;

				var offset = new Vector2(0, -4) + new Vector2(0, -26);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	} 
}
