using Terraria.ModLoader;
using Terraria;

namespace ArknightsMod.Content.Items.Armor.Vanity.Sniper.KroosAlter
{
    [AutoloadEquip(EquipType.Head)]
    public class KkdyAlterHead : ArknightsVanityHead
    {
		public override int Rarity => 5;
		public override void UpdateEquip(Player player)
        {
        }
        
        public override void UpdateArmorSet(Player player)
        {
            player.setBonus = "单手扫射M4，大家叫我克洛丝！";
        }
    } 
}
