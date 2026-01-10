using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.Fartooth
{
    [AutoloadEquip(EquipType.Head)]
    public class FartoothHead : ArknightsVanityHead
    {
		public override int Rarity => 6;
		public override void UpdateEquip(Player player)
        {
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<FartoothBody>() && legs.type == ModContent.ItemType<FartoothLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "我讨厌血狼破军";
        }
    } 
}
