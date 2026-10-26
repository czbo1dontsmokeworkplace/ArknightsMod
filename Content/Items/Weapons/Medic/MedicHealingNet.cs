using System;
using System.IO;
using ArknightsMod.Content.Items.Armor.Defender.Saria;
using ArknightsMod.Content.Items.Armor.Defender.Spot;
using ArknightsMod.Content.Projectiles.Medic.Warfarin;
using ArknightsMod.Content.Projectiles.Medic.Sussurro;
using ArknightsMod.Content.Projectiles.Medic.Shining;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Medic;

// Only the server (or single player) grants healing. The recipient applies one
// grant locally and publishes the resulting life value through vanilla networking.
internal static class MedicHealingNet
{
    internal static bool Allied(Player healer, Player patient) => healer.active && !healer.dead
        && patient.active && !patient.dead && (healer.whoAmI == patient.whoAmI
            || (!healer.hostile && !patient.hostile) || (healer.team != 0 && healer.team == patient.team));

    internal static int Heal(Player healer, Player patient, int requested, int interval = 60)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !Allied(healer, patient)
            || patient.statLife >= patient.statLifeMax2) return 0;
        MedicHealingPlayer state = patient.GetModPlayer<MedicHealingPlayer>();
        if (state.Cooldown > 0) return 0;
        int healing = SpotHealingHelper.ModifyOutgoingHeal(healer, Math.Clamp(requested, 0, 48));
        int amount = Math.Min(patient.GetModPlayer<global::ArknightsMod.Players.IncomingHealingPlayer>().Modify(healing), patient.statLifeMax2 - patient.statLife);
        if (amount <= 0) return 0;
        state.Cooldown = Math.Max(1, interval);
        if (Main.netMode == NetmodeID.SinglePlayer) patient.GetModPlayer<global::ArknightsMod.Players.IncomingHealingPlayer>().HealFinal(amount);
        else
        {
            patient.statLife = Math.Min(patient.statLifeMax2, patient.statLife + amount);
            ModPacket packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
            packet.Write((short)ArknightsMod.ArkMessageID.MedicHealingGrant);
            packet.Write((byte)patient.whoAmI);
            packet.Write((short)amount);
            packet.Send(patient.whoAmI);
        }
        global::ArknightsMod.Content.Items.Armor.DirectHealingEffects.OnDirectHeal(healer, patient, amount);
        return amount;
    }

    internal static void Receive(BinaryReader reader)
    {
        int patient = reader.ReadByte(), amount = reader.ReadInt16();
        if (Main.netMode != NetmodeID.MultiplayerClient || patient != Main.myPlayer
            || amount <= 0) return;
        Player player = Main.LocalPlayer;
        if (player.dead || !player.active) return;
        player.GetModPlayer<global::ArknightsMod.Players.IncomingHealingPlayer>().HealFinal(Math.Min(amount, Math.Max(0, player.statLifeMax2 - player.statLife)));
        NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, patient);
    }
}

public sealed class MedicHealingPlayer : ModPlayer
{
    internal int Cooldown;
    public override void PostUpdate() { if (Cooldown > 0) Cooldown--; }
    public override void UpdateDead() => Cooldown = 0;
}

public sealed class MedicContactHealing : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private readonly System.Collections.Generic.HashSet<int> treated = new();
    public override void PostAI(Projectile projectile)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !projectile.active
            || !Main.player.IndexInRange(projectile.owner)) return;
        int amount, interval = 60;
        if (projectile.ModProjectile is WarfarinBloodBolt) amount = 2;
        else if (projectile.ModProjectile is SussurroNeedle or SussurroCompressionRay)
        {
            amount = projectile.ai[0] > 0 ? 9 : 5;
            interval = projectile.ai[0] > 0 ? 30 : 60;
        }
        else if (projectile.ModProjectile is ShiningBladeProjectile) amount = 2;
        else return;
        Player healer = Main.player[projectile.owner];
        foreach (Player patient in Main.ActivePlayers)
        {
            // Ordinary attacks pass the caster on departure; self healing is
            // handled by Sussurro's butterflies and Shining's returning wisps.
            if (patient.whoAmI == healer.whoAmI || treated.Contains(patient.whoAmI)
                || !MedicHealingNet.Allied(healer, patient) || patient.DistanceSQ(healer.Center) > 1200f * 1200f
                || !projectile.Colliding(projectile.Hitbox, patient.Hitbox)) continue;
            if (MedicHealingNet.Heal(healer, patient, amount, interval) > 0) treated.Add(patient.whoAmI);
        }
    }
    public override bool CanHitPvp(Projectile projectile, Player target) =>
        projectile.ModProjectile is not (WarfarinBloodBolt or SussurroNeedle or SussurroCompressionRay or ShiningBladeProjectile);
}
