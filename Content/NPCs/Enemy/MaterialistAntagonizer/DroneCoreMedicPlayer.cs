using System;
using System.IO;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;
using ArknightsMod.Content.Items.Evolution;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed class DroneCoreMedicPlayer : ModPlayer
{
    internal bool Equipped;
    internal int Cooldown;
    public override void ResetEffects() => Equipped = false;
    public override void SaveData(TagCompound tag) => tag["DroneCoreHealCooldown"] = Cooldown;
    public override void LoadData(TagCompound tag) => Cooldown = Math.Clamp(tag.GetInt("DroneCoreHealCooldown"), 0, 900);
    public override void PostUpdate()
    {
        if (Cooldown > 0) Cooldown--;
        if (!Equipped || Player.dead || !Player.active || Main.netMode == NetmodeID.MultiplayerClient) return;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Player.whoAmI && p.ModProjectile is DroneCoreMedic) return;
        Projectile.NewProjectile(Player.GetSource_Misc("DroneCoreMedic"), Player.Center + new Microsoft.Xna.Framework.Vector2(-70, -95),
            Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<DroneCoreMedic>(), 0, 0, Player.whoAmI);
    }
    internal bool CanTreat => Equipped && !Player.dead && Player.active && Player.statLife < Player.statLifeMax2 &&
        Player.GetModPlayer<EvolutionAccessoryPlayer>().DyingTicks <= 0;
    internal void Treat()
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !CanTreat || Cooldown > 0) return;
        int amount = Math.Min(120, Player.statLifeMax2 - Player.statLife);
        Cooldown = 900;
        if (Main.netMode == NetmodeID.SinglePlayer) Player.Heal(amount);
        else
        {
            Player.statLife = Math.Min(Player.statLifeMax2, Player.statLife + amount);
            var packet = Mod.GetPacket();
            packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.DroneCoreHealingGrant);
            packet.Write((byte)Player.whoAmI); packet.Write((short)amount);
            packet.Send(Player.whoAmI);
        }
    }
    internal static void ReceiveHealing(BinaryReader reader)
    {
        int owner = reader.ReadByte(), amount = reader.ReadInt16();
        if (Main.netMode != NetmodeID.MultiplayerClient || owner != Main.myPlayer || amount is <= 0 or > 120) return;
        Player player = Main.LocalPlayer;
        if (!player.active || player.dead || player.GetModPlayer<EvolutionAccessoryPlayer>().DyingTicks > 0) return;
        player.Heal(Math.Min(amount, Math.Max(0, player.statLifeMax2 - player.statLife)));
        player.GetModPlayer<DroneCoreMedicPlayer>().Cooldown = 900;
        NetMessage.SendData(MessageID.PlayerLifeMana, number: owner);
    }
}
