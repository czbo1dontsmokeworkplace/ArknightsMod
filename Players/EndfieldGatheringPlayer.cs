using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Content.Items;
using ArknightsMod.Content.Items.Consumables;
using ArknightsMod.Content.NPCs.Friendly;
using ArknightsMod.Systems;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Players;

public sealed class EndfieldGatheringPlayer : ModPlayer
{
    private const int InventorySlots = 58;
    public int QuestIndex { get; private set; } = -1;
    private ulong nextInteraction;

    public override void Initialize()
    {
        QuestIndex = -1;
        nextInteraction = 0;
    }

    public override void SaveData(TagCompound tag)
    {
        if (QuestIndex >= 0)
            tag["EndfieldGatheringQuest"] = QuestIndex;
    }

    public override void LoadData(TagCompound tag)
    {
        int saved = tag.ContainsKey("EndfieldGatheringQuest") ? tag.GetInt("EndfieldGatheringQuest") : -1;
        QuestIndex = saved is >= 0 and < 18 ? saved : -1;
    }

    public void Interact(int npcIndex)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.EndfieldGatheringRequest);
            packet.Write(npcIndex);
            packet.Send();
            return;
        }
        ProcessInteraction(npcIndex);
    }

    private void ProcessInteraction(int npcIndex)
    {
        if ((uint)npcIndex >= Main.maxNPCs || Player.dead || !Player.active)
            return;
        NPC npc = Main.npc[npcIndex];
        if (!npc.active || npc.type != ModContent.NPCType<Closure>() || Vector2.DistanceSquared(Player.Center, npc.Center) > 320f * 320f
            || Main.GameUpdateCount < nextInteraction)
            return;
        nextInteraction = Main.GameUpdateCount + 15;

        int completedIndex = -1;
        if (QuestIndex < 0)
            QuestIndex = PickQuest();
        else if (TryConsumeQuestMaterial())
        {
            completedIndex = QuestIndex;
            int orundum = EndfieldNaturalCatalog.Resources[QuestIndex].OrundumValue * EndfieldGatheringRules.RequiredAmount;
            var bonus = BonusFor(QuestIndex);
            Player.QuickSpawnItem(Player.GetSource_Misc("ClosureEndfieldGathering"), ModContent.ItemType<Orundum>(), orundum);
            Player.QuickSpawnItem(Player.GetSource_Misc("ClosureEndfieldGathering"), bonus.Type, bonus.Stack);
            QuestIndex = -1;
        }

        if (Main.netMode == NetmodeID.Server)
        {
            ModPacket reply = Mod.GetPacket();
            reply.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.EndfieldGatheringReply);
            reply.Write(npcIndex);
            reply.Write(QuestIndex);
            reply.Write(completedIndex);
            reply.Send(Player.whoAmI);
        }
        else
            ShowReply(npcIndex, completedIndex);
    }

    private int PickQuest()
    {
        List<int> owned = [];
        for (int index = 0; index < EndfieldNaturalCatalog.Resources.Length; index++)
        {
            if (index == 4 && !Main.hardMode)
                continue;
            int type = EndfieldNaturalCatalog.ItemType(index);
            int count = 0;
            for (int slot = 0; slot < InventorySlots; slot++)
                if (Player.inventory[slot].type == type)
                    count += Player.inventory[slot].stack;
            if (count >= EndfieldGatheringRules.RequiredAmount)
                owned.Add(index);
        }
        if (owned.Count > 0)
            return owned[Main.rand.Next(owned.Count)];
        // 没有现成材料时，派发森林/地下基础资源；不会先派无法获取的神圣菌。
        int[] basic = [0, 1, 2, 6, 7, 8, 12, 13, 14];
        return basic[Main.rand.Next(basic.Length)];
    }

    private bool TryConsumeQuestMaterial()
    {
        int type = EndfieldNaturalCatalog.ItemType(QuestIndex);
        Span<int> stacks = stackalloc int[InventorySlots];
        Span<int> consumed = stackalloc int[InventorySlots];
        for (int slot = 0; slot < InventorySlots; slot++)
            stacks[slot] = Player.inventory[slot].type == type ? Player.inventory[slot].stack : 0;
        if (!EndfieldGatheringRules.TryPlanConsumption(stacks, EndfieldGatheringRules.RequiredAmount, consumed))
            return false;
        for (int slot = 0; slot < InventorySlots; slot++)
        {
            if (consumed[slot] == 0)
                continue;
            Player.inventory[slot].stack -= consumed[slot];
            if (Player.inventory[slot].stack <= 0)
                Player.inventory[slot].TurnToAir();
            if (Main.netMode == NetmodeID.Server)
                NetMessage.SendData(MessageID.SyncEquipment, -1, -1, null, Player.whoAmI, slot);
        }
        return true;
    }

    private static (int Type, int Stack) BonusFor(int index) => (index / 6) switch
    {
        0 => (ModContent.ItemType<RAMeatchip>(), 1),
        1 => (ModContent.ItemType<SeasonedRations>(), 1),
        _ => (index % 3) switch
        {
            0 => (ItemID.SpelunkerPotion, 2),
            1 => (ItemID.MiningPotion, 2),
            _ => (ItemID.RecallPotion, 3)
        }
    };

    private void ShowReply(int npcIndex, int completedIndex)
    {
        if (Main.LocalPlayer.TalkNPC?.whoAmI != npcIndex)
            return;
        int index = completedIndex >= 0 ? completedIndex : QuestIndex;
        if ((uint)index >= EndfieldNaturalCatalog.Resources.Length)
            return;
        int type = EndfieldNaturalCatalog.ItemType(index);
        var bonus = BonusFor(index);
        int orundum = EndfieldNaturalCatalog.Resources[index].OrundumValue * EndfieldGatheringRules.RequiredAmount;
        string material = $"[i:{type}] {Lang.GetItemNameValue(type)}";
        string gift = $"[i:{bonus.Type}] {Lang.GetItemNameValue(bonus.Type)}";
        Main.npcChatText = completedIndex >= 0
            ? Language.GetTextValue("Mods.ArknightsMod.EndfieldNatural.GatheringThanks", material, orundum, gift, bonus.Stack)
            : Language.GetTextValue("Mods.ArknightsMod.EndfieldNatural.GatheringRequest", material, EndfieldGatheringRules.RequiredAmount, orundum, gift, bonus.Stack);
        Main.npcChatCornerItem = completedIndex >= 0 ? 0 : type;
    }

    public static void ReceiveRequest(BinaryReader reader, int sender)
    {
        int npcIndex = reader.ReadInt32();
        if (Main.netMode == NetmodeID.Server && (uint)sender < Main.maxPlayers && Main.player[sender].active)
            Main.player[sender].GetModPlayer<EndfieldGatheringPlayer>().ProcessInteraction(npcIndex);
    }

    public static void ReceiveReply(BinaryReader reader)
    {
        int npcIndex = reader.ReadInt32();
        int questIndex = reader.ReadInt32();
        int completedIndex = reader.ReadInt32();
        if (Main.netMode != NetmodeID.MultiplayerClient || questIndex < -1 || questIndex >= 18
            || completedIndex < -1 || completedIndex >= 18)
            return;
        var player = Main.LocalPlayer.GetModPlayer<EndfieldGatheringPlayer>();
        player.QuestIndex = questIndex;
        player.ShowReply(npcIndex, completedIndex);
    }
}
