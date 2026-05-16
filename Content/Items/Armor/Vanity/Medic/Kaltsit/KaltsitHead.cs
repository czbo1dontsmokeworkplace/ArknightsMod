using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Medic.Kaltsit
{
    [AutoloadEquip(EquipType.Head)]
    public class KaltsitHead : ArknightsVanityHead
    {
		public override int Rarity => 6;
		public override void Load()
        {
        }
        public override void UpdateEquip(Player player)
        {
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<KaltsitBody>() && legs.type == ModContent.ItemType<KaltsitLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "钢板";
        }
    } 
}
