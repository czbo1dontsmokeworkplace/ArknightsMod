using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArknightsMod.Content.NPCs.Friendly;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Systems;

public class CannotProgressionSystem : ModSystem
{
    public CannotStockLedger Stock { get; private set; } = new();
    private bool initializeLegacyWorld;
    private bool stockChanged;
    public static CannotProgressionSystem Instance => ModContent.GetInstance<CannotProgressionSystem>();
    public static int CurrentTier => Math.Max(Instance.Stock.HighestTier,
        NPC.downedFishron ? 5 : NPC.downedPlantBoss ? 4 : NPC.downedBoss3 ? 3 : 1);

    // The former third pool is split explicitly; accessory stats and ordinary sale prices stay intact.
    private static readonly HashSet<string> Price12Items = [
        "AbyssalWyrdmask", "FallenSovereignForm", "KingsArmor", "KingsStaff", "MonarchSidearm",
        "KnightlyCodexRenewed", "NobleSwords", "OldSteamArmor", "SarkazKingRegalRest",
        "SarkazKingTornBanner", "SuspendedSilkPuppet", "TheProfoundSilence",
        "UnknownInstrument", "VieuxVanguardsBlade"
    ];

    public override void OnWorldLoad() {
        Stock = new();
        initializeLegacyWorld = true;
        stockChanged = true;
        NPCShopSystem.CannotShopItems.Clear();
        NPCShopSystem.OldCannotShopCount = -1;
        Cannot.RespawnCooldown = 0;
    }

    public override void OnWorldUnload() {
        Stock = new();
        NPCShopSystem.CannotShopItems.Clear();
        Cannot.RespawnCooldown = 0;
    }

    public override void SaveWorldData(TagCompound tag) {
        tag["CannotStockV1"] = Stock.Slots.Select(s => new TagCompound {
            ["Boss"] = s.BossKey, ["Tier"] = s.Tier, ["Item"] = ItemIO.Save(CreateShopItem(s))
        }).ToList();
    }

    public override void LoadWorldData(TagCompound tag) {
        if (!tag.ContainsKey("CannotStockV1")) return;
        initializeLegacyWorld = false;
        foreach (var entry in tag.GetList<TagCompound>("CannotStockV1")) {
            int tier = Math.Clamp(entry.GetInt("Tier"), 1, 5);
            Stock.Slots.Add(new(entry.GetString("Boss"), tier, ItemIO.Load(entry.GetCompound("Item")).type));
        }
    }

    public override void NetSend(BinaryWriter writer) {
        writer.Write(Stock.Slots.Count);
        foreach (var slot in Stock.Slots) {
            writer.Write(slot.BossKey);
            writer.Write((byte)slot.Tier);
            writer.Write(slot.ItemType);
        }
    }

    public override void NetReceive(BinaryReader reader) {
        Stock = new();
        int count = reader.ReadInt32();
        for (int i = 0; i < count; i++)
            Stock.Slots.Add(new(reader.ReadString(), reader.ReadByte(), reader.ReadInt32()));
        initializeLegacyWorld = false;
        NPCShopSystem.CannotShopItems = BuildInventory();
    }

    public override void PreUpdateWorld() {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (initializeLegacyWorld) {
            initializeLegacyWorld = false;
            ImportLegacyVictories();
        }
        if (!stockChanged) return;
        stockChanged = false;
        NPCShopSystem.TryUpdateCannotShop(Mod, true);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.WorldData);
    }

    public void RecordVictory(string key, int minimumTier = 1) {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        Stock.RecordVictory(key, Math.Max(CurrentTier, minimumTier), PickItem);
        stockChanged = true;
    }

    private static int PickItem(int tier, HashSet<int> used, int previous) {
        string ns = "ArknightsMod.Content.Items.Accessories.Rogue.Rarity_l" + (tier <= 2 ? tier : tier <= 4 ? 3 : 4);
        var pool = ModContent.GetContent<ModItem>()
            .Where(i => i.GetType().Namespace == ns && (tier is not (3 or 4) || Price12Items.Contains(i.Name) == (tier == 4)))
            .Select(i => i.Type).ToList();
        var available = pool.Where(t => !used.Contains(t) && t != previous).ToList();
        if (available.Count == 0) available = pool.Where(t => !used.Contains(t)).ToList();
        if (available.Count == 0) available = pool;
        return available.Count == 0 ? 0 : available[Main.rand.Next(available.Count)];
    }

    private static Item CreateShopItem(CannotStockLedger.Slot slot) => new(slot.ItemType) {
        shopSpecialCurrency = ArknightsMod.OriginiumIngotCurrencyId,
        value = CannotStockLedger.Prices[slot.Tier] * 50000
    };

    public List<Item> BuildInventory() => Stock.Slots.Where(s => s.ItemType > 0)
        .OrderBy(s => s.Tier).Select(CreateShopItem).ToList();

    private void ImportLegacyVictories() {
        // Old saves contain victory flags, but no kill order. Use normal progression for migration only.
        var victories = new (bool downed, string key, int tier)[] {
            (NPC.downedSlimeKing, "Terraria/KingSlime", 2), (NPC.downedBoss1, "Terraria/EyeofCthulhu", 2),
            (NPC.downedBoss2, WorldGen.crimson ? "Terraria/BrainofCthulhu" : "Terraria/EaterofWorlds", 2),
            (NPC.downedQueenBee, "Terraria/QueenBee", 2),
            (NPC.downedDeerclops, "Terraria/Deerclops", 2),
            (DownedBossSystem.DownedPompeii, "ArknightsMod/Pompeii", 2),
            (DownedBossSystem.DownedTheFirstToTalk, "ArknightsMod/TheFirstToTalk", 2),
            (NPC.downedBoss3, "Terraria/SkeletronHead", 3),
            (DownedBossSystem.DownedW, "ArknightsMod/WBoss", 3),
            (DownedBossSystem.DownedFaustAndMephisto, "ArknightsMod/FaustAndMephisto", 3),
            (Main.hardMode, "Terraria/WallofFlesh", 3), (NPC.downedQueenSlime, "Terraria/QueenSlimeBoss", 3),
            (NPC.downedMechBoss1, "Terraria/TheDestroyer", 3), (NPC.downedMechBoss2, "Terraria/Twins", 3),
            (NPC.downedMechBoss3, "Terraria/SkeletronPrime", 3),
            (DownedBossSystem.DownedFrostNova, "ArknightsMod/FrostNova", 3),
            (DownedBossSystem.DownedAACT, "ArknightsMod/AACT", 3),
            (DownedBossSystem.DownedEvolution, "ArknightsMod/Evolution", 3),
            (NPC.downedPlantBoss, "Terraria/Plantera", 4), (NPC.downedGolemBoss, "Terraria/Golem", 4),
            (DownedBossSystem.DownedMaterialistAntagonizer, "ArknightsMod/MaterialistAntagonizer", 4),
            (NPC.downedEmpressOfLight, "Terraria/HallowBoss", 4),
            (NPC.downedFishron, "Terraria/DukeFishron", 5), (NPC.downedAncientCultist, "Terraria/CultistBoss", 5),
            (NPC.downedMoonlord, "Terraria/MoonLordCore", 5)
        };
        foreach (var v in victories)
            if (v.downed) Stock.RecordVictory(v.key, v.tier, PickItem);
    }
}

public class CannotBossVictoryTracker : GlobalNPC
{
    public override void OnKill(NPC npc) {
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        string key;
        int minimumTier = 1;
        if (npc.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail) {
            if (Main.npc.Any(n => n.active && n.whoAmI != npc.whoAmI && n.life > 0 &&
                n.type is NPCID.EaterofWorldsHead or NPCID.EaterofWorldsBody or NPCID.EaterofWorldsTail)) return;
            key = "Terraria/EaterofWorlds";
        }
        else if (npc.type is NPCID.Retinazer or NPCID.Spazmatism) {
            if (Main.npc.Any(n => n.active && n.whoAmI != npc.whoAmI && n.life > 0 && n.type is NPCID.Retinazer or NPCID.Spazmatism)) return;
            key = "Terraria/Twins";
        }
        else {
            if (!npc.boss || npc.realLife >= 0 && npc.realLife != npc.whoAmI) return;
            if (npc.type == NPCID.BrainofCthulhu) key = "Terraria/BrainofCthulhu";
            else if (npc.ModNPC?.Name is "Faust" or "Mephisto") {
                if (Main.npc.Any(n => n.active && n.whoAmI != npc.whoAmI && n.life > 0 && n.ModNPC?.Name is "Faust" or "Mephisto")) return;
                key = "ArknightsMod/FaustAndMephisto";
            }
            else key = npc.ModNPC?.FullName ?? "Terraria/" + NPCID.Search.GetName(npc.type);
        }
        if (npc.type == NPCID.SkeletronHead) minimumTier = 3;
        if (npc.type == NPCID.Plantera) minimumTier = 4;
        if (npc.type == NPCID.DukeFishron) minimumTier = 5;
        CannotProgressionSystem.Instance.RecordVictory(key, minimumTier);
    }
}
