using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.StandardBearers;

// Some existing minions call StrikeNPC directly inside AI and bypass CanDamage.
// Keep the projectile owner in a scoped context so those calls obey phasing too.
public sealed class StandardBearerDamageSystem : ModSystem
{
    internal static int ActingOwner { get; private set; } = -1;
    public override void Load()
    {
        On_Projectile.AI += ScopeAI;
        On_Projectile.Damage += ScopeDamage;
        On_NPC.StrikeNPC_HitInfo_bool_bool += GateStrike;
        On_Player.ApplyDamageToNPC += GatePlayerStrike;
        On_NPC.AddBuff += TrackBuff;
        On_NPC.UpdateNPC_BuffSetFlags += SuspendOwnedBuffs;
    }
    private static void ScopeAI(On_Projectile.orig_AI orig, Projectile self)
    {
        int previous = ActingOwner; ActingOwner = self.owner;
        try { orig(self); } finally { ActingOwner = previous; }
    }
    private static void ScopeDamage(On_Projectile.orig_Damage orig, Projectile self)
    {
        int previous = ActingOwner; ActingOwner = self.owner;
        try { orig(self); } finally { ActingOwner = previous; }
    }
    private static int GateStrike(On_NPC.orig_StrikeNPC_HitInfo_bool_bool orig, NPC self, NPC.HitInfo hit, bool fromNet, bool noPlayerInteraction)
    {
        if ((uint)ActingOwner < Main.maxPlayers && StandardBearerPlayer.IsPhased(Main.player[ActingOwner])) return 0;
        return orig(self, hit, fromNet, noPlayerInteraction);
    }
    private static void GatePlayerStrike(On_Player.orig_ApplyDamageToNPC orig, Player self, NPC target, int damage,
        float knockback, int direction, bool crit, DamageClass damageType, bool damageVariation)
    {
        if (StandardBearerPlayer.IsPhased(self)) return;
        int previous = ActingOwner; ActingOwner = self.whoAmI;
        try { orig(self, target, damage, knockback, direction, crit, damageType, damageVariation); }
        finally { ActingOwner = previous; }
    }
    private static void TrackBuff(On_NPC.orig_AddBuff orig, NPC self, int type, int time, bool quiet)
    {
        bool existing = self.HasBuff(type);
        orig(self, type, time, quiet);
        if (self.HasBuff(type)) self.GetGlobalNPC<StandardBearerDotOwnership>().Track(type, ActingOwner, existing);
    }
    private static void SuspendOwnedBuffs(On_NPC.orig_UpdateNPC_BuffSetFlags orig, NPC self, bool lowerBuffTime)
    {
        StandardBearerDotOwnership state = self.GetGlobalNPC<StandardBearerDotOwnership>();
        // Temporarily omit owned effects from this frame's flag calculation, preserving
        // their remaining lifetime. Other players' debuffs keep working in multiplayer.
        var suspended = state.Suspend(self, lowerBuffTime);
        try { orig(self, lowerBuffTime); }
        finally { state.Restore(self, suspended); }
    }
}

public sealed class StandardBearerDotOwnership : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private readonly System.Collections.Generic.Dictionary<int, System.Collections.Generic.HashSet<int>> owners = new();
    internal void Track(int type, int owner, bool existing)
    {
        if (!existing || !owners.TryGetValue(type, out var sources)) owners[type] = sources = new();
        sources.Add(owner);
    }
    internal System.Collections.Generic.List<(int type, int time)> Suspend(NPC npc, bool lowerTime)
    {
        System.Collections.Generic.List<(int type, int time)> held = null;
        for (int i = 0; i < npc.buffType.Length; i++)
        {
            int type = npc.buffType[i];
            if (type == 0 || !owners.TryGetValue(type, out var sources)) continue;
            bool allPhased = sources.Count > 0;
            foreach (int source in sources)
                if ((uint)source >= Main.maxPlayers || !StandardBearerPlayer.IsPhased(Main.player[source])) { allPhased = false; break; }
            if (!allPhased) continue;
            held ??= new(); held.Add((type, npc.buffTime[i] - (lowerTime ? 1 : 0)));
            // DelBuff compacts the remaining slots. Restore into empty slots afterwards.
            npc.DelBuff(i--);
        }
        return held;
    }
    internal void Restore(NPC npc, System.Collections.Generic.List<(int type, int time)> held)
    {
        if (held == null) return;
        foreach (var buff in held)
        {
            if (buff.time <= 0) { owners.Remove(buff.type); continue; }
            for (int i = 0; i < npc.buffType.Length; i++)
                if (npc.buffType[i] == 0) { npc.buffType[i] = buff.type; npc.buffTime[i] = buff.time; break; }
        }
    }
    public override void UpdateLifeRegen(NPC npc, ref int damage)
    {
        if (npc.lifeRegen >= 0) return;
        // Covers bespoke mod DoTs which store timers outside the normal buff array.
        // Only suppress the aggregate when every contributing player is phased.
        bool any = false;
        for (int i = 0; i < Main.maxPlayers; i++)
            if (npc.playerInteraction[i])
            {
                any = true;
                if (!StandardBearerPlayer.IsPhased(Main.player[i])) return;
            }
        if (any) { npc.lifeRegen = 0; damage = 0; }
    }
}
