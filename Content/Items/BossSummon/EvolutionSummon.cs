using ArknightsMod.Content.Items.Material;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Boss = ArknightsMod.Content.NPCs.Enemy.Evolution.Evolution;

namespace ArknightsMod.Content.Items.BossSummon;

public sealed class EvolutionSummon : ModItem
{
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        ItemID.Sets.SortingPriorityBossSpawns[Type] = 13;
    }
    public override void SetDefaults()
    {
        Item.width = 32; Item.height = 32;
        Item.maxStack = 1; Item.rare = ItemRarityID.Pink;
        Item.useAnimation = Item.useTime = 45;
        Item.useStyle = ItemUseStyleID.HoldUp;
        Item.consumable = false;
    }
    public override bool CanUseItem(Player player) => NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3 && !NPC.AnyNPCs(ModContent.NPCType<Boss>());
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI == Main.myPlayer)
        {
            SoundEngine.PlaySound(SoundID.Roar, player.Center);
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: ModContent.NPCType<Boss>());
            else NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<Boss>());
        }
        return true;
    }
    public override void AddRecipes()
    {
        // 凝胶水晶是史莱姆皇后的召唤物；两种邪恶材料任选其一。
        foreach (int material in new[] { ItemID.TissueSample, ItemID.ShadowScale })
            CreateRecipe().AddIngredient(ItemID.QueenSlimeCrystal)
                .AddIngredient(ItemID.SoulofMight)
                .AddIngredient(ItemID.SoulofSight)
                .AddIngredient(ItemID.SoulofFright)
                .AddIngredient(material, 5)
                .AddTile(TileID.FleshCloningVat).Register();
    }
}
