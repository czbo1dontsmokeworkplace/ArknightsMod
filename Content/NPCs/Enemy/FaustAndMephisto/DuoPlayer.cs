using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.FaustAndMephisto;

internal static class DuoNet
{
    internal enum Action : byte { Hit, Dust, Execute }
    internal static void Apply(int player, Faust boss, Action action, int amount = 0, int direction = 0)
    {
        if (!DuoEncounter.Authority || !DuoEncounter.Alive(Main.player[player])) return;
        if (Main.netMode == NetmodeID.Server)
        {
            ModPacket packet = boss.Mod.GetPacket();
            packet.Write((short)global::ArknightsMod.ArknightsMod.ArkMessageID.FaustAndMephisto);
            packet.Write((byte)action); packet.Write((short)boss.NPC.whoAmI); packet.Write((int)boss.NPC.ai[3]);
            packet.Write((short)amount); packet.Write((sbyte)direction); packet.Send(player);
        }
        else Local(Main.player[player], boss.NPC.whoAmI, action, amount, direction);
    }
    internal static void Receive(BinaryReader reader)
    {
        // This message is server -> one owning client only. Client requests never mutate combat.
        if (Main.netMode != NetmodeID.MultiplayerClient) return;
        Action action = (Action)reader.ReadByte(); int slot = reader.ReadInt16(), token = reader.ReadInt32();
        int amount = reader.ReadInt16(), direction = reader.ReadSByte();
        if (DuoEncounter.Owner(slot + 1, token) == null || !DuoEncounter.Alive(Main.LocalPlayer)) return;
        Local(Main.LocalPlayer, slot, action, amount, direction);
    }
    private static void Local(Player player, int boss, Action action, int amount, int direction)
    {
        if (action == Action.Dust)
        {
            player.AddBuff(ModContent.BuffType<OriginiumDust>(), DuoRules.DustDuration);
            player.GetModPlayer<DuoPlayer>().DustTicks = DuoRules.DustDuration;
            DuoVisuals.Puff(player.Center, Color.LightGray);
        }
        else if (action == Action.Execute)
        {
            Main.NewText(Language.GetTextValue("Mods.ArknightsMod.FaustAndMephisto.Impatient"), Color.IndianRed);
            player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.ArknightsMod.FaustAndMephisto.ExecutionDeath", player.name)), 999999, 0);
        }
        else if (action == Action.Hit && amount > 0)
            player.Hurt(PlayerDeathReason.ByNPC(boss), amount, direction, cooldownCounter: ImmunityCooldownID.Bosses);
    }
}
public sealed class OriginiumDust : ModBuff
{
    public override string LocalizationCategory => "FaustAndMephisto.Buffs";
    public override string Texture => "Terraria/Images/Buff_20";
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.pvpBuff[Type] = false; BuffID.Sets.LongerExpertDebuff[Type] = false; }
}
public sealed class DuoPlayer : ModPlayer
{
    internal int DustTicks, OutsideTicks;
    internal float Darkness;
    internal bool Watching;
    private int encounterToken, lifeStealCooldown;
    public override void OnEnterWorld() => ResetEncounter();
    private void ResetEncounter() { DustTicks = OutsideTicks = encounterToken = 0; Darkness = 0; Watching = false; }
    public override void UpdateDead() => ResetEncounter();
    public override void PostUpdate()
    {
        if (lifeStealCooldown > 0) lifeStealCooldown--;
        if (Player.whoAmI == Main.myPlayer && DustTicks > 0)
        {
            // A separate duration guarantees twelve 5-HP ticks even when the UI buff expires
            // earlier in the last frame. The originating medicine refreshes this duration.
            if (--DustTicks % DuoRules.DustInterval == 0)
            {
                Player.statLife -= DuoRules.DustDamage;
                CombatText.NewText(Player.Hitbox, Color.LightGray, DuoRules.DustDamage, dot: true);
                if (Player.statLife <= 0)
                    Player.KillMe(PlayerDeathReason.ByCustomReason(NetworkText.FromKey("Mods.ArknightsMod.FaustAndMephisto.DustDeath", Player.name)), DuoRules.DustDamage, 0);
                else if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
            }
        }
        if (!DuoEncounter.Alive(Player)) { ResetEncounter(); return; }
        Faust boss = null;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is Faust f && (npc.ai[3] == encounterToken || Player.Distance(DuoEncounter.Partner(f)?.NPC.Center ?? f.Arena) < DuoRules.EscapeRadius))
            { boss = f; break; }
        if (boss == null) { OutsideTicks = encounterToken = 0; Watching = false; Darkness = Math.Max(0, Darkness - .03f); return; }
        if (encounterToken != (int)boss.NPC.ai[3]) { encounterToken = (int)boss.NPC.ai[3]; OutsideTicks = 0; }
        Vector2 center = DuoEncounter.Partner(boss)?.NPC.Center ?? boss.NPC.Center;
        float distance = Player.Distance(center);
        OutsideTicks = boss.Retreating ? 0 : DuoRules.EscapeTimer(OutsideTicks, distance);
        Watching = distance > DuoRules.WarningRadius || OutsideTicks > 0;
        float desired = Watching ? MathHelper.Clamp((distance - DuoRules.WarningRadius) / (DuoRules.EscapeRadius - DuoRules.WarningRadius), 0, 1) * .6f : 0;
        Darkness = MathHelper.Lerp(Darkness, desired, .04f);
        if (DuoEncounter.Authority && OutsideTicks >= DuoRules.EscapeGrace + DuoRules.ExecutionLock)
        {
            Projectile.NewProjectile(boss.NPC.GetSource_FromAI(), boss.Muzzle, Player.Center - boss.Muzzle,
                ModContent.ProjectileType<FaustExecution>(), 0, 0, Main.myPlayer, boss.NPC.whoAmI + 1, boss.NPC.ai[3]);
            DuoNet.Apply(Player.whoAmI, boss, DuoNet.Action.Execute);
            OutsideTicks = 0;
        }
    }
    internal void StealLife(NPC target, int damage)
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead || lifeStealCooldown > 0 || Player.moonLeech || Player.lifeSteal < 1 ||
            target.friendly || target.type == NPCID.TargetDummy || target.lifeMax <= 5 || target.SpawnedFromStatue || damage <= 0) return;
        int heal = Math.Min(2, Math.Min((int)Player.lifeSteal, Player.statLifeMax2 - Player.statLife));
        if (heal <= 0) return;
        lifeStealCooldown = 30; Player.lifeSteal -= heal; Player.Heal(heal);
        if (Main.netMode == NetmodeID.MultiplayerClient) NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
    }
}
public sealed class DuoScreenSystem : ModSystem
{
    public override void PostDrawInterface(SpriteBatch spriteBatch)
    {
        if (Main.gameMenu || Main.LocalPlayer.dead) return;
        DuoPlayer state = Main.LocalPlayer.GetModPlayer<DuoPlayer>();
        if (state.Darkness > .005f)
            spriteBatch.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, 0, (int)(Main.screenWidth / Main.UIScale) + 1, (int)(Main.screenHeight / Main.UIScale) + 1), Color.Black * state.Darkness);
        if (!state.Watching) return;
        Vector2 position = Vector2.Transform(Main.LocalPlayer.Top - Main.screenPosition - new Vector2(0, 32), Main.GameViewMatrix.TransformationMatrix) / Main.UIScale;
        Color red = new(229, 60, 66);
        // Angular snake eye followed by the unmistakable locked crosshair.
        DuoVisuals.Line(spriteBatch, position + new Vector2(-18, 0), position + new Vector2(0, -8), red, 2);
        DuoVisuals.Line(spriteBatch, position + new Vector2(18, 0), position + new Vector2(0, -8), red, 2);
        DuoVisuals.Line(spriteBatch, position + new Vector2(-18, 0), position + new Vector2(0, 8), red, 2);
        DuoVisuals.Line(spriteBatch, position + new Vector2(18, 0), position + new Vector2(0, 8), red, 2);
        DuoVisuals.Line(spriteBatch, position - new Vector2(0, 6), position + new Vector2(0, 6), red, 3);
        if (state.OutsideTicks < DuoRules.EscapeGrace) return;
        Vector2 body = position + new Vector2(0, 57);
        DuoVisuals.Ring(spriteBatch, body, 31, red, 32);
        DuoVisuals.Line(spriteBatch, body - new Vector2(41, 0), body + new Vector2(41, 0), red, 1.5f);
        DuoVisuals.Line(spriteBatch, body - new Vector2(0, 41), body + new Vector2(0, 41), red, 1.5f);
    }
}
