using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Sniper.Melanite
{
    [AutoloadEquip(EquipType.Head)]
    public class MelaniteHead : NeoArmorHead
    {
		public override int Rarity => 5;
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<MelaniteBody>() && legs.type == ModContent.ItemType<MelaniteLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
        }
    } 
}
