using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.GameContent;
using Terraria.GameContent.Personalities;
using Terraria.Localization;
using System.Collections.Generic;
using Terraria.DataStructures;
using Terraria.GameContent.Bestiary;
using Terraria.Utilities;

namespace ArknightsMod.Common.Items
{
    public class KingsGlobalItem : GlobalItem
    {
        public bool isKingItem;

        public override bool InstancePerEntity => true;

        public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
        {
            if (isKingItem)
            {
                string txt = Language.GetTextValue("Mods.ArknightsMod.Dialogue.King");
                tooltips.Add(new TooltipLine(Mod, "KingItem", txt));
            }
        }
    }
}