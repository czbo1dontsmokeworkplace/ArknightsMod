using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed class MaterialistUplink : ModItem
{
    public override string Texture => MaterialistAntagonizer.AssetRoot + "MaterialistAntagonizer";
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 1;
        ItemID.Sets.SortingPriorityBossSpawns[Type] = 14;
    }
    public override void SetDefaults()
    {
        Item.width = 36; Item.height = 22; Item.maxStack = 1;
        Item.rare = ItemRarityID.Lime; Item.useStyle = ItemUseStyleID.HoldUp;
        Item.scale = .18f;
        Item.useTime = Item.useAnimation = 45; Item.consumable = false;
    }
    public override bool CanUseItem(Player player) => NPC.downedPlantBoss &&
        (player.ZoneOverworldHeight || player.ZoneSkyHeight) && !NPC.AnyNPCs(ModContent.NPCType<MaterialistAntagonizer>());
    public override bool? UseItem(Player player)
    {
        if (player.whoAmI != Main.myPlayer) return true;
        SoundEngine.PlaySound(SoundID.Item93, player.Center);
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.SpawnBossUseLicenseStartEvent, number: player.whoAmI, number2: ModContent.NPCType<MaterialistAntagonizer>());
        else NPC.SpawnOnPlayer(player.whoAmI, ModContent.NPCType<MaterialistAntagonizer>());
        return true;
    }
    public override void AddRecipes() => CreateRecipe().AddIngredient(ItemID.ChlorophyteBar, 8)
        .AddIngredient(ItemID.Wire, 25).AddIngredient(ItemID.SoulofSight, 5)
        .AddTile(TileID.MythrilAnvil).AddCondition(Condition.DownedPlantera).Register();
    public override bool PreDrawInInventory(SpriteBatch batch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        Texture2D texture = MaterialistVisuals.Asset(Texture);
        batch.Draw(texture, position, null, Color.White, 0, texture.Size() * .5f, 42f / texture.Width, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch batch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        Texture2D texture = MaterialistVisuals.Asset(Texture);
        batch.Draw(texture, Item.Center - Main.screenPosition, null, lightColor, rotation, texture.Size() * .5f, 42f / texture.Width, SpriteEffects.None, 0);
        return false;
    }
}

public sealed class DroneCommandCore : ModItem
{
    public override string Texture => MaterialistAntagonizer.AssetRoot + "MaterialistAntagonizer";
    private static readonly Rectangle CoreFrame = new(100, 56, 35, 48);
    public override void SetStaticDefaults() => Item.ResearchUnlockCount = 1;
    public override void SetDefaults()
    {
        Item.width = 26; Item.height = 30; Item.accessory = true;
        Item.rare = ItemRarityID.Lime; Item.value = Item.sellPrice(gold: 6);
    }
    public override void UpdateAccessory(Player player, bool hideVisual)
    {
        player.maxMinions++;
        player.GetDamage(DamageClass.Summon) += .08f;
        player.statDefense += 4;
    }
    public override bool PreDrawInInventory(SpriteBatch batch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
    {
        batch.Draw(MaterialistVisuals.Asset(Texture), position, CoreFrame, Color.White, 0, CoreFrame.Size() * .5f, .7f, SpriteEffects.None, 0);
        return false;
    }
    public override bool PreDrawInWorld(SpriteBatch batch, Color lightColor, Color alphaColor, ref float rotation, ref float scale, int whoAmI)
    {
        batch.Draw(MaterialistVisuals.Asset(Texture), Item.Center - Main.screenPosition, CoreFrame, lightColor, rotation, CoreFrame.Size() * .5f, .7f, SpriteEffects.None, 0);
        return false;
    }
}

public sealed class MaterialistTreasureBag : ModItem
{
    public override string Texture => "Terraria/Images/Item_" + ItemID.PlanteraBossBag;
    public override void SetStaticDefaults()
    {
        Item.ResearchUnlockCount = 3;
        ItemID.Sets.BossBag[Type] = true;
    }
    public override void SetDefaults()
    {
        Item.width = 32; Item.height = 32; Item.maxStack = Item.CommonMaxStack;
        Item.consumable = true; Item.expert = true; Item.rare = ItemRarityID.Expert;
    }
    public override bool CanRightClick() => true;
    public override void ModifyItemLoot(ItemLoot loot)
    {
        loot.Add(ItemDropRule.Common(ModContent.ItemType<DroneCommandCore>()));
        loot.Add(ItemDropRule.Common(ItemID.ChlorophyteBar, 1, 18, 24));
        loot.Add(ItemDropRule.CoinsBasedOnNPCValue(ModContent.NPCType<MaterialistAntagonizer>()));
    }
}
