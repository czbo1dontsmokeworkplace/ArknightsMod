using System;
using System.IO;
using ArknightsMod.Content.Buffs.Medic.ReedFlameShadow;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Players;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Medic.ReedFlameShadow;

internal sealed class ReedFlameShadowSetPlayer : ModPlayer
{
    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone)
        => ReportHit(target, damageDone);

    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        => ReportHit(target, damageDone);

    private void ReportHit(NPC target, int damageDone)
    {
        if (damageDone <= 0 || Player.whoAmI != Main.myPlayer || !WearsHelmet(Player)
            || !CanApplyScar(target)) return;
        if (Main.netMode == NetmodeID.MultiplayerClient)
        {
            var packet = Mod.GetPacket();
            packet.Write((short)ArknightsMod.ArkMessageID.ReedHelmetScarHit);
            packet.Write((short)target.whoAmI);
            packet.Send();
        }
        else TryApplyScar(Player, target);
    }

    internal static bool WearsHelmet(Player player) => player.active && !player.dead
        && player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<ReedFlameShadowHead>();

    private static bool WearsSet(Player player) => WearsHelmet(player)
        && player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<ReedFlameShadowBody>()
        && player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<ReedFlameShadowLegs>();

    private static bool CanApplyScar(NPC target) => target.active && !target.friendly && !target.townNPC
        && !target.dontTakeDamage && target.life > 0 && !target.HasBuff<BurnedScar>();

    internal static void TryApplyScar(Player player, NPC target)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !WearsHelmet(player)
            || !CanApplyScar(target) || Main.rand.NextFloat() >= .30f) return;
        // Match the weapon's ordinary scar: eight seconds, no refresh and no S3 mark.
        target.AddBuff(ModContent.BuffType<BurnedScar>(), 8 * 60);
        target.netUpdate = true;
    }

    internal static void ReceiveScarHit(BinaryReader reader, int sender)
    {
        int target = reader.ReadInt16();
        if (Main.netMode != NetmodeID.Server || (uint)sender >= Main.maxPlayers
            || (uint)target >= Main.maxNPCs) return;
        TryApplyScar(Main.player[sender], Main.npc[target]);
    }

    internal static void OnDirectHeal(Player healer, Player patient, int actualAmount)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || actualAmount < 2
            || healer.whoAmI == patient.whoAmI || !WearsSet(healer)
            || healer.GetModPlayer<IncomingHealingPlayer>().HealingBlocked) return;
        // Copy half the final HP gain; do not multiply healing bonuses a second time.
        int amount = Math.Min(actualAmount / 2, Math.Max(0, healer.statLifeMax2 - healer.statLife));
        if (amount <= 0) return;
        if (Main.netMode == NetmodeID.SinglePlayer) healer.GetModPlayer<IncomingHealingPlayer>().HealFinal(amount);
        else
        {
            healer.statLife += amount;
            var packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
            packet.Write((short)ArknightsMod.ArkMessageID.ReedSelfHealingGrant);
            packet.Write(amount);
            packet.Send(healer.whoAmI);
        }
    }

    internal static void ReceiveSelfHealing(BinaryReader reader)
    {
        int amount = reader.ReadInt32();
        if (Main.netMode != NetmodeID.MultiplayerClient || amount <= 0) return;
        Player player = Main.LocalPlayer;
        if (!player.active || player.dead) return;
        player.GetModPlayer<IncomingHealingPlayer>().HealFinal(Math.Min(amount, Math.Max(0, player.statLifeMax2 - player.statLife)));
        NetMessage.SendData(MessageID.PlayerLifeMana, number: player.whoAmI);
    }
}
