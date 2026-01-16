using Terraria.ModLoader;
using Terraria;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria.DataStructures;
using ArknightsMod.Common;

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster.Mostima
{
    [AutoloadEquip(EquipType.Head)]
    public class MostimaHead : ArknightsVanityHead
    {
		public override int Rarity => 6;
		public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MostimaBody>() && legs.type == ModContent.ItemType<MostimaLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
        }
		internal class MostimaHeadLayer : PlayerDrawLayer
		{
			public override bool GetDefaultVisibility(PlayerDrawSet drawInfo) {
				Item head = new(ModContent.ItemType<MostimaHead>());
				return drawInfo.drawPlayer.head == head.headSlot && !drawInfo.drawPlayer.dead;
			}
			protected override void Draw(ref PlayerDrawSet drawInfo) {
				var texture = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Armor/Vanity/Caster/Mostima/MostimaHead_Ring").Value;
				var offset = new Vector2(0, -3) + new Vector2(2, -28);
				PlayerLayerHelper.AddPlayerDrawLayer(ref drawInfo, texture, 0, offset);
			}
			public override Position GetDefaultPosition() => new BeforeParent(PlayerDrawLayers.BackAcc);
		}
	} 
}
