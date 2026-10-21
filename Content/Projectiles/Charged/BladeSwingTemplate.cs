using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Charged;

// Shared motion, pose and swept collision. Target limits and branch bonuses belong
// to the calling weapon, so sharing a swing never shares its profession traits.
internal static class BladeSwingTemplate
{
    internal const int BaseTicks = 28;
    internal static float Duration(float attackSpeed, bool swift = false) =>
        Math.Max(8, (swift ? 21f : BaseTicks) / Math.Max(.1f, attackSpeed));
    internal static float Advance(float age, float duration, int extraUpdates) =>
        Math.Min(duration, age + 1f / (extraUpdates + 1));
    internal static float Turn(float progress)
    {
        float t = Math.Clamp(progress, 0, 1);
        return t - .6f * MathF.Sin(2 * MathF.PI * t) / MathF.PI
            + .075f * MathF.Sin(4 * MathF.PI * t) / MathF.PI;
    }
    internal static float Angle(float aim, int facing, float progress) =>
        aim + MathHelper.Lerp(-MathHelper.Pi, MathHelper.Pi, Turn(progress)) * facing;
    internal static void Pose(Player owner, float angle, bool twoHands = false)
    {
        owner.itemRotation = angle;
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
        if (twoHands) owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, angle - MathHelper.PiOver2);
    }
    internal static bool Contact(Rectangle box, float startAngle, float endAngle,
        Vector2 startCenter, Vector2 endCenter, float reach, float thickness, int facing,
        out Vector2 point, out Vector2 tangent)
    {
        int steps = Math.Max(1, (int)MathF.Ceiling(Math.Abs(endAngle - startAngle) / .04f));
        for (int i = 0; i <= steps; i++) {
            float t = i / (float)steps, distance = 0;
            Vector2 start = Vector2.Lerp(startCenter, endCenter, t);
            Vector2 axis = MathHelper.Lerp(startAngle, endAngle, t).ToRotationVector2();
            if (!Collision.CheckAABBvLineCollision(box.TopLeft(), box.Size(), start, start + axis * reach, thickness, ref distance)
                || !Collision.CanHitLine(start, 1, 1, box.TopLeft(), box.Width, box.Height)) continue;
            point = box.ClosestPointInRect(start + axis * Math.Clamp(distance, 0, reach));
            tangent = axis.RotatedBy(facing * MathHelper.PiOver2);
            return true;
        }
        point = tangent = default;
        return false;
    }
}
