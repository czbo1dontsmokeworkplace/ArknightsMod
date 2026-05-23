using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Guard.LaPluma
{
    [AutoloadEquip(EquipType.Head)]
    public class LaPlumaHead : NeoArmorHead
    {
		public override int Rarity => 5;
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
