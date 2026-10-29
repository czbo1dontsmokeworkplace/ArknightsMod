using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class Isharmla
{
    internal void SpawnDrifter(Vector2 origin, Vector2 target, int curveSide)
    {
        if (!Authority || Cleaning || Transition > 0 || Stage == TideStage.Finale ||
            Vector2.DistanceSquared(origin, Main.player[NPC.target].Center) < 300 * 300) return;
        int count = 0;
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is IsharmlaDrifter drifter && drifter.OwnerIndex == NPC.whoAmI && drifter.Encounter == Encounter) count++;
        if (count >= IsharmlaDrifter.PopulationLimit) return;
        Vector2 velocity = (target - origin).SafeNormalize(Vector2.UnitY).RotatedBy(curveSide * .36f) * 7.5f;
        int slot = Terraria.NPC.NewNPC(NPC.GetSource_FromAI(), (int)origin.X, (int)origin.Y, ModContent.NPCType<IsharmlaDrifter>());
        if (slot >= Main.maxNPCs) return;
        NPC spawned = Main.npc[slot]; spawned.Center = origin;
        ((IsharmlaDrifter)spawned.ModNPC).Bind(this, velocity, curveSide);
    }
}
