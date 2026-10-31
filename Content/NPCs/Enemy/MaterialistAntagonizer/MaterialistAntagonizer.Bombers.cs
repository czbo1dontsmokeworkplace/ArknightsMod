using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal bool BomberOperationActive
    {
        get {
            if (BomberSortieInProgress) return true;
            foreach (Projectile shot in Main.ActiveProjectiles)
                if (shot.ModProjectile is BomberDronePayload payload && payload.BelongsTo(this)) return true;
            return false;
        }
    }

    internal static bool BomberReleaseDue(int phase, int beat, int launched) =>
        phase == 3 && launched < 6 && beat >= 12 + launched * 24;

    private void RunBomberSortie(int beat)
    {
        if (!Authority || !BomberReleaseDue(Phase, beat, BombersLaunched)) return;
        Vector2 origin = NPC.Center + new Vector2(BombersLaunched % 2 == 0 ? -120 : 120, 38);
        int index = NPC.NewNPC(NPC.GetSource_FromAI(), (int)origin.X, (int)origin.Y, ModContent.NPCType<BomberDrone>());
        if (index < 0 || index >= Main.maxNPCs) return; // A full pool returns the sentinel slot, not a live NPC.
        NPC escort = Main.npc[index];
        escort.Center = origin;
        var wing = escort.GetGlobalNPC<MaterialistWing>();
        wing.Bind(escort, this, 20 + BombersLaunched);
        wing.Age = MaterialistRules.EscortGrace; // This sortie is already part of an active combat order.
        escort.dontTakeDamage = false;
        BombersLaunched++;
        escort.netUpdate = NPC.netUpdate = true;
    }
}
