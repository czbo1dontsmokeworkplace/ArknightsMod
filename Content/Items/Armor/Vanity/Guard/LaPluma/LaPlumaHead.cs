using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Guard.LaPluma
{
    [AutoloadEquip(EquipType.Head)]
    public class LaPlumaHead : ArknightsVanityHead
    {
		public override int Rarity => 5;
		public override void UpdateEquip(Player player)
        {
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<LaPlumaBody>() && legs.type == ModContent.ItemType<LaPlumaLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "现在还不能休息哦";
        }
    } 
}
