using System;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

internal static class MaterialistArsenalRules
{
    internal const int GatlingSpinUp = 30, GatlingInterval = 2, GatlingSpinDown = 16;
    internal const int OrbitalFade = 10, RotorLaserInterval = 8;
    internal const int OrbitalWarning = 10, OrbitalBurstSpacing = 8, OrbitalBurstCount = 3;
    internal static Vector2 ClampAim(Vector2 requested, Player owner, float range)
    {
        if (!float.IsFinite(requested.X) || !float.IsFinite(requested.Y)) requested = owner.Center;
        Vector2 delta = requested - owner.Center;
        if (delta.LengthSquared() > range * range) requested = owner.Center + delta.SafeNormalize(Vector2.UnitX) * range;
        return Vector2.Clamp(requested, new Vector2(32), new Vector2(Main.maxTilesX * 16 - 32, Main.maxTilesY * 16 - 32));
    }
    internal static NPC Target(Vector2 center, float range, Player owner, bool designated, int previous = -1,
        float ownerRange = 1800, bool ignoreTiles = false)
    {
        bool Valid(NPC npc) => npc != null && npc.CanBeChasedBy() &&
            Vector2.DistanceSquared(center, npc.Center) < range * range &&
            Vector2.DistanceSquared(owner.Center, npc.Center) < ownerRange * ownerRange &&
            (ignoreTiles || Collision.CanHitLine(center, 1, 1, npc.position, npc.width, npc.height));
        if (designated && owner.HasMinionAttackTargetNPC && owner.MinionAttackTargetNPC >= 0 && owner.MinionAttackTargetNPC < Main.maxNPCs)
        {
            NPC marked = Main.npc[owner.MinionAttackTargetNPC];
            if (Valid(marked)) return marked;
        }
        NPC selected = null;
        float distance = range * range;
        if (previous >= 0 && previous < Main.maxNPCs && Valid(Main.npc[previous]))
        {
            selected = Main.npc[previous]; distance = selected.DistanceSQ(center) * .85f;
        }
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float next = npc.DistanceSQ(center);
            if (next < distance && Valid(npc)) { selected = npc; distance = next; }
        }
        return selected;
    }
    internal static bool Capsule(Rectangle target, Vector2 start, Vector2 end, float width)
    {
        float distance = 0;
        return Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(), start, end, width, ref distance);
    }
    internal static Vector2 Limit(Vector2 vector, float maximum) => vector.LengthSquared() > maximum * maximum
        ? vector.SafeNormalize(Vector2.Zero) * maximum : vector;
}
