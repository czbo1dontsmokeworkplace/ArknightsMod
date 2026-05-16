using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.Utage
{
    [AutoloadEquip(EquipType.Head)]
    public class UtageHead : ArknightsVanityHead
    {
		public override int Rarity => 4;
		public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<UtageBody>() && legs.type == ModContent.ItemType<UtageLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "超大杯（各种意义上的）";
        }
    } 
}
