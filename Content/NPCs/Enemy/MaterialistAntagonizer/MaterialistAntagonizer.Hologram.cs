using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistAntagonizer
{
    // Client presentation follows the synchronized phase-wait flag. Never gates damage or phase changes.
    internal float LockShieldProgress { get; private set; }
    internal Vector2 LockShieldSize => new Vector2(500) * MathHelper.Lerp(.88f, 1,
        MaterialistFormation.Smooth(LockShieldProgress));

    private void UpdateLockShield()
    {
        if (Main.dedServ) return;
        bool active = FinalStand && Order is not (FlightOrder.Transition or FlightOrder.Death) && lostTargetTime == 0;
        float previous = LockShieldProgress;
        LockShieldProgress = MathHelper.Clamp(previous + (active ? 1 / 42f : -1 / 54f), 0, 1);
        if (Math.Abs(previous - LockShieldProgress) < .00001f ||
            Main.GameUpdateCount % (ulong)(MaterialistVisuals.Reduced ? 8 : 3) != 0) return;

        float front = LockShieldProgress * 2 - 1;
        Vector2 half = LockShieldSize * .5f;
        float span = half.X * MathF.Sqrt(Math.Max(0, 1 - front * front));
        for (int side = -1; side <= 1; side += 2)
        {
            Vector2 point = NPC.Center + new Vector2(side * span, front * half.Y).RotatedBy(NPC.rotation * .35f);
            MaterialistVisuals.Nano(point, new Vector2(side * .6f, active ? -.7f : -1.4f),
                new Color(100, 226, 255), .8f, 20);
        }
    }
}
