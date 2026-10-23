using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    private void RunPayloadBattery(int beat, int forcedPayload = -1)
    {
        if (!Authority || Order == FlightOrder.Ram) return;
        int round = CoreSerial;
        int payload = forcedPayload >= 0 ? forcedPayload : Math.Abs(Cycle + round + Wave + Phase) % 3;
        Player player = Main.player[NPC.target];
        if (payload == 0 && beat is 30 or 60)
        {
            // Upward launch followed by a real parabola, with lateral velocity aimed at a broad landing zone.
            float drift = MathHelper.Clamp((player.Center.X - CoreMuzzle.X) / 98f, -6, 6);
            int half = Phase == 1 ? 1 : 2;
            for (int i = -half; i <= half; i++)
            {
                Vector2 velocity = new(drift + i * 1.45f, -7.5f - Math.Abs(i) * .4f);
                float axis = new Vector2(drift, -7.5f).ToRotation();
                velocity = velocity.RotatedBy(MathHelper.WrapAngle(velocity.ToRotation() - axis) * (MaterialistRules.SpreadSpacing - 1));
                Shoot(DroneShot.GravityOrb, CoreMuzzle, velocity, duration: 160, style: FireStyle.CoreSpread);
            }
        }
        if (payload == 2 && beat == 18)
        {
            int count = Phase == 3 ? 3 : 2;
            for (int i = 0; i < count; i++)
            {
                Vector2 point = player.Center + new Vector2((i - (count - 1) * .5f) * 190, -60 - i % 2 * 40);
                bool burst = (i + round + Cycle) % 2 == 0;
                Shoot(DroneShot.Beacon, CoreMuzzle, (point - CoreMuzzle) * .085f, duration: 164,
                    style: FireStyle.CoreFocus, bend: burst ? 1 : 0);
            }
        }
    }
    private void FireAreaScan(Player player)
    {
        Vector2 point = player.Center + Vector2.Clamp(player.velocity * 10, new Vector2(-70), new Vector2(70));
        Shoot(DroneShot.ScanBurst, point, Vector2.Zero, MaterialistRules.ScanWarning, 24, sweep: 200 + Phase * 20,
            length: 260 + Phase * 40, style: FireStyle.CoreFocus);
    }
}
