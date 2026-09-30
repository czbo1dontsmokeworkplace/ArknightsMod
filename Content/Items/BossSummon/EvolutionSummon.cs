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
    public override string Texture => EvolutionVisuals.Root + "Specimen";
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        ItemID.Sets.SortingPriorityBossSpawns[Type] = 13;
    }
    public override void SetDefaults()
    {
        Item.width = 52; Item.height = 40;
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
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.HallowedBar, 8).AddIngredient(ItemID.SoulofLight, 6)
        .AddIngredient(ItemID.SoulofNight, 6).AddIngredient<OriginiumShard>(20).AddTile(TileID.MythrilAnvil).Register();
}
