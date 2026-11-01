using System.IO;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Manticore;

internal class ManticoreSetPlayer : ArknightsArmorPlayer, global::ArknightsMod.Players.IProbabilityDodgeSource
{
    private readonly ManticoreStealthState state = new();
    public string DodgeName => "狮蝎头盔";
    public double DodgeChance => ManticoreHelmetActive ? .10 : 0;
    public bool ManticoreHelmetActive;
    public bool ManticoreSetActive;
    public bool Stealthed => state.Stealthed;
    public int Transparency => state.Transparency;
    private bool Owner => Player.whoAmI == Main.myPlayer;

    internal static bool HasSet(Player player) => player.active && !player.dead
        && player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<ManticoreHead>()
        && player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<ManticoreBody>()
        && player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<ManticoreLegs>();

    public override void ResetEffects() => ManticoreHelmetActive = ManticoreSetActive = false;
    public override void PostUpdateEquips() {
        ManticoreHelmetActive = Player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<ManticoreHead>();
        ManticoreSetActive = HasSet(Player);
        if (!ManticoreSetActive) ClearState();
    }

    public override void PostUpdate() {
        if (!HasSet(Player)) { ClearState(); return; }
        int before = state.FadeTicks;
        state.Tick(Player.itemAnimation > 0 || Player.itemTime > 0, Main.GameUpdateCount);
        if (state.Stealthed) Player.aggro -= 1200;
        if (Owner && before != state.FadeTicks
            && (before == 0 || state.FadeTicks == ManticoreStealthState.FadeDuration || state.FadeTicks % 30 == 0))
            SendState();
    }

    private static bool IsEnemy(NPC npc) => npc.type == NPCID.TargetDummy
        || !npc.friendly && !NPCID.Sets.CountsAsCritter[npc.type];

    // These hooks run for a concrete hit, not for weapon-panel/tooltip damage queries.
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) {
        if (HasSet(Player) && state.Stealthed && IsEnemy(target)) modifiers.FinalDamage *= 1.5f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => BreakOnHit(target, damageDone);
    private void BreakOnHit(NPC target, int damageDone) {
        if (!HasSet(Player) || damageDone <= 0 || !IsEnemy(target)) return;
        // Also block regrowth during this hit's update: another simultaneous hit gets no bonus.
        if (state.BreakOnHit(Main.GameUpdateCount) && Owner) SendState();
    }

    public override void TransformDrawData(ref PlayerDrawSet drawInfo) {
        if (!HasSet(Player) || state.Transparency <= 0) return;
        float opacity = (255f - state.Transparency) / 255f;
        // Fade the completed premultiplied draw colors once, including armor and glow layers.
        // Do not use immuneAlpha: stealth must not overwrite hit-invulnerability flashing.
        for (int i = 0; i < drawInfo.DrawDataCache.Count; i++) {
            DrawData data = drawInfo.DrawDataCache[i];
            data.color *= opacity;
            drawInfo.DrawDataCache[i] = data;
        }
    }

    private void ClearState() {
        bool changed = state.Stealthed;
        state.Clear();
        if (changed && Owner) SendState();
    }
    public override void UpdateDead() => ClearState();
    public override void OnRespawn() => state.Clear();
    public override void OnEnterWorld() => state.Clear();
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => SendState(toWho);

    private void SendState(int toWho = -1) {
        if (Main.netMode == NetmodeID.SinglePlayer || Main.netMode == NetmodeID.MultiplayerClient && !Owner) return;
        var packet = ModContent.GetInstance<ArknightsMod>().GetPacket();
        packet.Write((short)ArknightsMod.ArkMessageID.ManticoreStealth);
        packet.Write((byte)Player.whoAmI);
        packet.Write((byte)state.FadeTicks);
        packet.Send(toWho);
    }
    internal static void Receive(BinaryReader reader, int sender) {
        int index = reader.ReadByte(), fadeTicks = reader.ReadByte();
        if ((uint)index >= Main.maxPlayers || fadeTicks > ManticoreStealthState.FadeDuration) return;
        Player player = Main.player[index];
        var stealth = player.GetModPlayer<ManticoreSetPlayer>();
        if (Main.netMode == NetmodeID.Server) {
            if (index != sender || fadeTicks > 0 && !HasSet(player)) return;
            stealth.state.Set(fadeTicks, Main.GameUpdateCount);
            stealth.SendState();
        }
        else if (Main.netMode == NetmodeID.MultiplayerClient && index != Main.myPlayer)
            stealth.state.Set(fadeTicks, Main.GameUpdateCount);
    }
}
