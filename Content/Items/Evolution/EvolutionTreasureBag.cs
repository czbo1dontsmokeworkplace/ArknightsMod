using ArknightsMod.Content.NPCs.Enemy.Evolution;
using ArknightsMod.Content.Items.Evolution.Arsenal;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution;

public sealed class EvolutionTreasureBag : ModItem
{
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 3;
        ItemID.Sets.BossBag[Type] = true;
        // Every bag guarantees both accessories and all four weapons.
        ItemID.Sets.PreHardmodeLikeBossBag[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = 32; Item.height = 34;
        Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true; Item.expert = true; Item.rare = ItemRarityID.Expert;
    }
    public override bool CanRightClick() => true;
    public override void ModifyItemLoot(ItemLoot itemLoot)
    {
        itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<EvolutionOrigin>()));
        itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<EvolutionTerminus>()));
        itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<TerminusLight>()));
        itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<OriginSever>()));
        itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<NestOfLife>()));
        itemLoot.Add(ItemDropRule.Common(ModContent.ItemType<RushingEnd>()));
    }
}
