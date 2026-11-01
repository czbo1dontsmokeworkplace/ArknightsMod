using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mizuki;

internal class MizukiSetPlayer : ArknightsArmorPlayer
{
    public bool MizukiHelmetActive;
    public bool MizukiSetActive;
    private bool applyingBonus;

    private static bool HasHelmet(Player player) => player.active && !player.dead
        && player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<MizukiHead>();
    internal static bool HasSet(Player player) => HasHelmet(player)
        && player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<MizukiBody>()
        && player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<MizukiLegs>();

    public override void ResetEffects() => MizukiHelmetActive = MizukiSetActive = false;
    public override void PostUpdateEquips() {
        MizukiHelmetActive = HasHelmet(Player);
        MizukiSetActive = HasSet(Player);
    }
    public override void PostUpdateMiscEffects() {
        if (HasHelmet(Player) && HasLowHpEnemyNearby()) Player.GetDamage(DamageClass.Generic) *= 1.2f;
    }
    private bool HasLowHpEnemyNearby() {
        foreach (NPC npc in Main.ActiveNPCs) {
            if (npc.friendly || npc.lifeMax <= 5 || npc.dontTakeDamage) continue;
            if (npc.Distance(Player.Center) <= 900f && npc.life < npc.lifeMax * .5f) return true;
        }
        return false;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) {
        // tML invokes hit callbacks on the attacking client. Sync the finalized strike,
        // rather than rerunning the armor proc on the server.
        if (applyingBonus || Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer || !HasSet(Player) || damageDone < 2
            || !target.active || target.life <= 0 || target.dontTakeDamage
            || (target.type != NPCID.TargetDummy && (target.friendly || NPCID.Sets.CountsAsCritter[target.type]))) return;
        int bonus = damageDone / 2;
        NPC.HitInfo extraHit = new() {
            Damage = bonus,
            SourceDamage = bonus,
            DamageType = DamageClass.Generic,
            Crit = false,
            Knockback = 0f,
            HitDirection = hit.HitDirection
        };
        applyingBonus = true;
        try {
            target.PlayerInteraction(Player.whoAmI);
            // HitInfo is already finalized: no second defense/resistance/crit calculation.
            target.StrikeNPC(extraHit);
            if (Main.netMode != NetmodeID.SinglePlayer) NetMessage.SendStrikeNPC(target, ref extraHit);
        }
        finally { applyingBonus = false; }
    }
}
