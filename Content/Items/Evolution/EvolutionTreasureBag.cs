using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Terraria;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution;

public sealed class EvolutionTreasureBag : ModItem
{
    // Reuse the supplied fleshy sac icon; no placeholder vanilla bag or new art dependency.
    public override string Texture => EvolutionVisuals.Root + "Excrescence";
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 3;
        ItemID.Sets.BossBag[Type] = true;
        // Deliberately suppress automatic developer armor: this bag contains ONLY the two accessories.
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
    }
}
