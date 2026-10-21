using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

internal static class EvolutionArsenalRules
{
    internal const int CoreCount = 8, MatrixLife = 78, GuardianCycle = 120;
    internal const int CoreHitCooldown = 15;
    internal const float CoreAngularSpeed = 1.7f;
    internal const float OriginScale = .7f;
    internal static int CoreDamage(int laserDamage) => Math.Max(1, laserDamage / 5);
    internal static bool CoreActive(int index, float tick) => index >= 0 && index < CoreCount && tick >= 1 && tick < CoreFireTick(index);
    internal const float TerminusSearchRange = 200 * 16;
    internal const int MistLife = 45;
    internal static int SwingTicks(int stage) => stage == 0 ? 16 : stage == 1 ? 18 : 22;
    internal static int CoreFireTick(int index) => 20 + index * 4;
    internal static bool SwordActive(float progress) => progress >= .03f && progress <= .9f;
    internal static bool GuardianSlash(int tick) => tick >= 18 && tick <= 30;
    internal static bool GuardianDash(int tick) => tick >= 42 && tick <= 61;
    internal static bool GuardianContact(int tick) => GuardianSlash(tick) || GuardianDash(tick);
    internal static bool GuardianSpits(int tick) => tick == 70 || tick == 88 || tick == 106;
    internal static Vector2 ClampAnchor(Vector2 requested, Player owner)
    {
        Vector2 delta = requested - owner.Center;
        if (delta.LengthSquared() > 960 * 960) requested = owner.Center + delta.SafeNormalize(Vector2.UnitX) * 960;
        return Vector2.Clamp(requested, new Vector2(32), new Vector2(Main.maxTilesX * 16 - 32, Main.maxTilesY * 16 - 32));
    }
    internal static NPC Target(Vector2 center, float range, Player owner, bool designated = false)
    {
        bool Valid(NPC npc) => npc != null && npc.CanBeChasedBy() && Vector2.DistanceSquared(center, npc.Center) < range * range &&
            (!designated || Vector2.DistanceSquared(owner.Center, npc.Center) < 1600 * 1600);
        if (designated && owner.HasMinionAttackTargetNPC && owner.MinionAttackTargetNPC >= 0 && owner.MinionAttackTargetNPC < Main.maxNPCs)
        {
            NPC marked = Main.npc[owner.MinionAttackTargetNPC];
            if (Valid(marked)) return marked;
        }
        NPC result = null;
        float closest = range * range;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (!Valid(npc)) continue;
            float distance = Vector2.DistanceSquared(center, npc.Center);
            if (distance >= closest || !Collision.CanHitLine(center, 1, 1, npc.position, npc.width, npc.height)) continue;
            closest = distance; result = npc;
        }
        return result;
    }
    internal static Vector2 CoreOffset(int index, float tick)
    {
        float angle = index * MathHelper.TwoPi / CoreCount + tick * CoreAngularSpeed * (index % 2 == 0 ? .043f : -.034f);
        float radius = (index % 2 == 0 ? 92 : 67) * MathHelper.SmoothStep(.22f, 1, Math.Clamp(tick / 18f, 0, 1));
        return new Vector2(MathF.Cos(angle) * radius, MathF.Sin(angle) * radius * .76f).RotatedBy(tick * CoreAngularSpeed * .009f);
    }
    internal static float SwingAngle(int stage, float progress)
    {
        float p = SwingEase(progress);
        return stage == 0 ? MathHelper.Lerp(-1.9f, 1.5f, p) : stage == 1 ? MathHelper.Lerp(1.8f, -1.9f, p) : MathF.Sin(progress * MathHelper.Pi) * .12f;
    }
    // Angular speed decreases linearly: an immediate cut followed by a slower follow-through.
    internal static float SwingEase(float progress)
    {
        float remaining = 1 - Math.Clamp(progress, 0, 1);
        return 1 - remaining * remaining;
    }
    internal static Vector2 SwordTip(Vector2 center, Vector2 aim, int facing, int stage, float progress)
    {
        float reach = stage == 2 ? 115 + 190 * MathF.Sin(MathHelper.Pi * SwingEase(progress)) : stage == 0 ? 205 : 235;
        return center + aim.RotatedBy(SwingAngle(stage, progress) * facing) * (reach * OriginScale);
    }
    internal static Vector2 HitBloodglowOrigin(NPC target, int index, float horizontal, float height)
        => target.Center + new Vector2(Math.Clamp(horizontal, -1, 1) * 180,
            (index % 2 == 0 ? -1 : 1) * (target.height * .5f + MathHelper.Lerp(150, 300, Math.Clamp(height, 0, 1))));
    internal static bool Capsule(Rectangle target, Vector2 start, Vector2 end, float width)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), start, end, width, ref point);
    }
    internal static bool RayEntry(Vector2 start, Vector2 direction, Rectangle box, float maximum, out float entry)
    {
        float near = 0, far = maximum;
        for (int axis = 0; axis < 2; axis++)
        {
            float origin = axis == 0 ? start.X : start.Y, velocity = axis == 0 ? direction.X : direction.Y;
            float low = axis == 0 ? box.Left : box.Top, high = axis == 0 ? box.Right : box.Bottom;
            if (Math.Abs(velocity) < .00001f) { if (origin < low || origin > high) { entry = 0; return false; } continue; }
            float first = (low - origin) / velocity, last = (high - origin) / velocity;
            if (first > last) (first, last) = (last, first);
            near = Math.Max(near, first); far = Math.Min(far, last);
            if (near > far) { entry = 0; return false; }
        }
        entry = near; return far >= 0 && near <= maximum;
    }
    internal static float ClipRay(Vector2 origin, Vector2 direction, NPC selected, out int hitNpc)
    {
        float length = Math.Min(TerminusSearchRange, Vector2.Distance(origin, selected.Center));
        hitNpc = selected.whoAmI;
        for (int i = 0; i < Main.maxNPCs; i++)
        {
            NPC npc = Main.npc[i];
            if (npc == null || !npc.CanBeChasedBy() || !RayEntry(origin, direction, npc.Hitbox, length, out float entry)) continue;
            if (entry < length) { length = entry; hitNpc = i; }
        }
        // Stop slightly inside the first body so collision and the visible impact agree.
        return Math.Min(TerminusSearchRange, length + 2);
    }
}
