using Microsoft.Xna.Framework;
using Terraria;
using ArknightsMod.Content.NPCs.Enemy.ThroughChapter4;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistWing
{
    private bool FireArtsProtocol(NPC npc, MaterialistAntagonizer boss, WingDuty duty, int beat)
    {
        if (npc.ModNPC is not ArtsMasterDrone) return false;
        if (beat < 0 || beat >= 150) return true;
        Vector2 muzzle = Muzzle(npc);
        if (npc.ModNPC is ArtsMasterA1)
        {
            // A1 alone owns the large purple homing lights. One orb per unit per joint round.
            if (beat == 48 + Slot % 2 * 26)
            {
                Vector2 aim = (Main.player[boss.NPC.target].Center - muzzle).SafeNormalize(Vector2.UnitY);
                boss.Shoot(DroneShot.ArtsOrb, muzzle, aim.RotatedBy(Slot % 2 == 0 ? -.28f : .28f) * MaterialistRules.ArtsOrbSpeed,
                    24, MaterialistRules.ArtsOrbLifetime, npc.whoAmI, style: FireStyle.Precision);
            }
            return true;
        }
        // A2 alone owns persistent range containment, and fires only large purple lasers.
        if (beat == 24 + Slot % 2 * 16)
        {
            if (boss.Phase == 3)
            {
                // Final-phase containment translates a vertical beam; it never rotates towards the player.
                Vector2 player = Main.player[boss.NPC.target].Center;
                Vector2 separation = muzzle - player;
                separation.Y = -System.Math.Max(80, System.Math.Abs(separation.Y));
                Vector2 projection = player + separation * 1.3f;
                projection = Vector2.Clamp(projection, new Vector2(32), new Vector2(Main.maxTilesX * 16 - 32, Main.maxTilesY * 16 - 32));
                boss.Shoot(DroneShot.Beam, projection, Vector2.UnitY, 54, 76,
                    npc.whoAmI, style: FireStyle.Containment);
                return true;
            }
            // Both A2 openings deliberately miss the initial player line by about 15 degrees.
            float miss = MathHelper.ToRadians(15 + Main.rand.NextFloat(-3, 3)) * (Slot % 2 == 0 ? -1 : 1);
            Vector2 direction = (Main.player[boss.NPC.target].Center - muzzle).SafeNormalize(Vector2.UnitY).RotatedBy(miss);
            bool tracking = Slot == 0 && boss.Pattern is WingPattern.ShieldRelay or WingPattern.PursuitNet or WingPattern.Scissor;
            boss.Shoot(tracking ? DroneShot.ChaseBeam : DroneShot.Beam, muzzle, direction, 54, 76,
                npc.whoAmI, style: FireStyle.Containment);
        }
        return true;
    }
}
