using System;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    private int broodDamageRemainder;

    internal void ReceiveBroodLifeLoss(int loss)
    {
        if (Main.netMode == NetmodeID.MultiplayerClient || !NPC.active || loss <= 0) return;
        if (Phase != 1 || Intangible) { broodDamageRemainder = 0; return; }
        long sixths = (long)loss + broodDamageRemainder;
        int damage = (int)Math.Min(int.MaxValue, sixths / 6);
        broodDamageRemainder = (int)(sixths % 6);
        if (damage == 0) return;
        // Share actual HP loss once. Do not run defense/mark multipliers on that damage a second time.
        // Preserve the normal 80% phase transition rather than allowing an overkill to skip phases.
        NPC.life = Math.Min(NPC.life, Math.Max(EvolutionRules.Threshold(NPC.lifeMax, 1), NPC.life - damage));
        NPC.netUpdate = true;
    }
}

public abstract partial class EvolutionBroodNPC
{
    private int observedLife = -1;
    public override void OnSpawn(IEntitySource source) => observedLife = NPC.life;

    internal void ShareLostLife()
    {
        int current = Math.Max(0, NPC.life);
        int lost = observedLife < 0 ? 0 : observedLife - current;
        observedLife = current;
        if (Main.netMode == NetmodeID.MultiplayerClient || SupportOnly || Retiring || lost <= 0) return;
        Parent?.ReceiveBroodLifeLoss(lost);
    }
}
