using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    internal Vector2 FormationVelocity, TrackingVelocity;
    private Vector2 frameCorrection;
    private bool frameReady;

    private void UpdateCombatFrame(Player player)
    {
        Vector2 targetVelocity = MaterialistFlight.Limit(player.velocity, 110);
        TrackingVelocity = Vector2.Lerp(TrackingVelocity, targetVelocity, .18f);
        Vector2 lead = new(MathHelper.Clamp(TrackingVelocity.X * 6, -150, 150), MathHelper.Clamp(TrackingVelocity.Y * 4, -90, 90));
        Vector2 target = Vector2.Clamp(player.Center + lead, new Vector2(480), new Vector2(Main.maxTilesX * 16 - 480, Main.maxTilesY * 16 - 480));
        if (!frameReady)
        {
            FormationCenter = target;
            FormationVelocity = TrackingVelocity;
            frameReady = true;
        }
        else
        {
            Vector2 before = FormationCenter;
            float speed = TrackingVelocity.Length();
            Vector2 desired = TrackingVelocity + MaterialistFlight.Limit((target - FormationCenter) * .14f, 19);
            float cap = Math.Min(138, Math.Max(19, speed + 18));
            FormationVelocity = MaterialistFlight.Approach(FormationVelocity, MaterialistFlight.Limit(desired, cap), 1.1f + Math.Min(speed, 96) * .055f);
            Vector2 correction = MaterialistFlight.Limit(frameCorrection * .08f, 3);
            frameCorrection -= correction;
            FormationCenter = Vector2.Clamp(FormationCenter + FormationVelocity + correction,
                new Vector2(480), new Vector2(Main.maxTilesX * 16 - 480, Main.maxTilesY * 16 - 480));
            FormationVelocity = FormationCenter - before;
        }
        float exit = MathHelper.PiOver2 + (Cycle % 2 == 0 ? .25f : -.25f);
        ExitAngle += MathHelper.Clamp(MathHelper.WrapAngle(exit - ExitAngle), -.009f, .009f);
    }
    private void MoveTo(Vector2 destination, float speed = 12)
    {
        if (NPC.HasValidTarget)
        {
            Vector2 player = Main.player[NPC.target].Center;
            destination = player + (destination - player) * 1.10f;
        }
        destination = Vector2.Clamp(destination, new Vector2(180, 160), new Vector2(Main.maxTilesX * 16 - 180, Main.maxTilesY * 16 - 200));
        float targetSpeed = TrackingVelocity.Length();
        float maximumSpeed = MathHelper.Clamp(targetSpeed * 1.16f + 32, 34, 150);
        NPC.velocity = MaterialistFlight.Follow(NPC.Center, NPC.velocity, destination, FormationVelocity,
            Math.Max(9, speed + 3), targetSpeed, .85f + Math.Min(targetSpeed, 96) * .055f, maximumSpeed);
    }
}
