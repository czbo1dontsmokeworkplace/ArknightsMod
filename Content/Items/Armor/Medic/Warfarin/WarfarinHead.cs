using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Medic.Warfarin
{
    [AutoloadEquip(EquipType.Head)]
    public class WarfarinHead : NeoArmorHead
    {
		public override int Rarity => 5;
		public override void Load()
        {
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<WarfarinBody>() &&
				legs.type == ModContent.ItemType<WarfarinLegs>();
        }
    } 
}
