using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Vanity.Caster.Lava
{
    [AutoloadEquip(EquipType.Body)]
    public class LavaBody : ArknightsVanityBody
    {
        public override int Rarity => 3;
    }
}
