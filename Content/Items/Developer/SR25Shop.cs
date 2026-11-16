using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Developer;

public sealed class SR25Shop : GlobalNPC
{
    public override void ModifyShop(NPCShop shop)
    {
        if (shop.NpcType != NPCID.ArmsDealer) return;
        shop.Add(new Item(ModContent.ItemType<SR25>()) { shopCustomPrice = Item.buyPrice(gold: 55) }, Condition.DownedMechBossAny);
    }
}
