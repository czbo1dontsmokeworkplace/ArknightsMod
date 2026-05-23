using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Sniper.Rosmontis
{
    [AutoloadEquip(EquipType.Head)]
    public class RosmontisHead : NeoArmorHead
    {
		public override int Rarity => 6;
		public override void Load()
        {
        }
        public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<RosmontisBody>() && legs.type == ModContent.ItemType<RosmontisLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "Kiss~喵";
        }
    } 
}
