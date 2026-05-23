using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Specialist.Manticore
{
    [AutoloadEquip(EquipType.Head)]
    public class ManticoreHead : NeoArmorHead
    {
		public override int Rarity => 5;
		public override void Load()
        {
		}
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<ManticoreBody>() && legs.type == ModContent.ItemType<ManticoreLegs>();
        }
    } 
}
