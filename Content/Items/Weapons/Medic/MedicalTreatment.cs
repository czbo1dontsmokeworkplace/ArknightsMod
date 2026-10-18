using System;
using System.IO;
using ArknightsMod.Content.Projectiles.Medic.Arts;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic;

public sealed class MedicalTreatmentPlayer : ModPlayer
{
    internal readonly MedicationResistance Resistance = new();
    internal readonly MedicalRegenLayers Regeneration = new();
    internal ulong NextFieldTreatmentSound;

    internal int ApplyMedicalTreatment(int requestedAmount, int attackIntervalTicks, float reduction = 1f)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || Player.dead || !Player.active) return 0;
        int amount = Math.Min(Resistance.Scale(requestedAmount), Math.Max(0, Player.statLifeMax2 - Player.statLife));
        if (amount > 0) Resistance.Commit(attackIntervalTicks, 2, reduction);
        return amount;
    }
    public override void PostUpdate()
    {
        Resistance.Tick(); Regeneration.Tick();
        if (Resistance.Ticks > 0) Player.AddBuff(ModContent.BuffType<MedicationSickness>(), Resistance.Ticks);
        if (Regeneration.Layers.Count > 0)
        {
            Player.AddBuff(ModContent.BuffType<MedicalRejuvenation>(), 2);
            if (!Main.dedServ && Player.miscCounter % 6 == 0)
                MedicArtsVfx.Rejuvenation(Player, Regeneration.Layers[^1].Profile, Regeneration.Layers.Count);
        }
    }
    public override void UpdateLifeRegen() => Player.lifeRegen += (int)MathF.Round(Regeneration.Rate * 2);
    public override void UpdateDead()
    {
        bool changed = Resistance.Ticks > 0 || Regeneration.Layers.Count > 0;
        Resistance.Clear(); Regeneration.Layers.Clear();
        if (changed && Main.netMode == NetmodeID.Server) SendState();
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
    {
        if (Main.netMode == NetmodeID.Server) SendState(toWho);
    }
    internal void SendState(int toWho = -1, int heal = 0, int profile = -1, int[] removed = null)
    {
        if (Main.netMode != NetmodeID.Server) return;
        var packet = Mod.GetPacket();
        packet.Write((short)ArknightsMod.ArkMessageID.MedicalTreatmentState);
        packet.Write((byte)Player.whoAmI);
        packet.Write((short)heal); packet.Write((sbyte)profile);
        packet.Write(Resistance.Ticks); packet.Write(Resistance.Reduction);
        packet.Write((byte)Regeneration.Layers.Count);
        foreach (var layer in Regeneration.Layers)
        {
            packet.Write(layer.Ticks); packet.Write(layer.Rate); packet.Write((byte)layer.Profile);
        }
        packet.Write((byte)(removed?.Length ?? 0));
        if (removed != null) foreach (int type in removed) packet.Write(type);
        packet.Send(toWho);
    }
    internal static void Receive(BinaryReader reader)
    {
        int index = reader.ReadByte(), heal = reader.ReadInt16(), profile = reader.ReadSByte();
        int ticks = reader.ReadInt32(); float reduction = reader.ReadSingle();
        int count = reader.ReadByte();
        if (Main.netMode != NetmodeID.MultiplayerClient || !Main.player.IndexInRange(index)) return;
        var player = Main.player[index]; var state = player.GetModPlayer<MedicalTreatmentPlayer>();
        state.Resistance.Ticks = Math.Clamp(ticks, 0, 36000);
        state.Resistance.Reduction = Math.Clamp(reduction, 0, 1);
        state.Regeneration.Layers.Clear();
        for (int i = 0; i < count; i++)
        {
            int duration = reader.ReadInt32(); float rate = reader.ReadSingle(); int id = reader.ReadByte();
            if (i < 20) state.Regeneration.Layers.Add(new(duration, rate, id));
        }
        int removedCount = reader.ReadByte();
        for (int i = 0; i < removedCount; i++)
        {
            int type = reader.ReadInt32(); int slot = player.FindBuffIndex(type);
            if (slot >= 0) player.DelBuff(slot);
        }
        if (index == Main.myPlayer && player.active && !player.dead && heal != 0)
        {
            // Negative values carry full-health floating text without authorizing any life gain.
            int displayed = Math.Clamp(Math.Abs(heal), 0, 400);
            int actual = heal > 0 ? Math.Min(displayed, Math.Max(0, player.statLifeMax2 - player.statLife)) : 0;
            if (actual > 0)
            {
                player.Heal(actual);
                NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, index);
            }
            else if (!Main.dedServ) player.HealEffect(displayed);
        }
        if (profile >= 0)
        {
            MedicArtsAudio.Treatment(player, profile, removedCount);
            MedicArtsVfx.Treatment(player.Center, profile, removedCount);
        }
    }
}

public sealed class MedicationSickness : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.PotionSickness;
    public override void SetStaticDefaults()
    {
        Main.debuff[Type] = true; Main.buffNoSave[Type] = true;
        BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
    }
}

public sealed class MedicalRejuvenation : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Regeneration;
    public override void SetStaticDefaults() { Main.buffNoSave[Type] = true; Main.buffNoTimeDisplay[Type] = true; }
}

internal static class MedicalTreatment
{
    internal const int StandardAttackIntervalTicks = 96;
    internal const int InitialTreatmentDelayTicks = 30;
    internal static int Apply(Player target, int requestedAmount, int attackIntervalTicks = StandardAttackIntervalTicks,
        float medicationReduction = 1f) => target.GetModPlayer<MedicalTreatmentPlayer>()
        .ApplyMedicalTreatment(requestedAmount, attackIntervalTicks, medicationReduction);
}
