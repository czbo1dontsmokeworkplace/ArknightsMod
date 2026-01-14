using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster.Haze
{
    [AutoloadEquip(EquipType.Head)]
    public class HazeHead : ArknightsVanityHead
    {
		public override int Rarity => 4;
		public override bool IsArmorSet(Item head, Item body, Item legs)
        {
            return body.type == ModContent.ItemType<HazeBody>() && legs.type == ModContent.ItemType<HazeLegs>();
        }
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "维多利亚的囚服怎么这么好看？";
        }
    } 
}
