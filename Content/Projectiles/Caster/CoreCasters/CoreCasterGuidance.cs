using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster.CoreCasters;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

// Armor homing respects the same launch interval as the projectile itself.
public interface ICoreCasterGuidance { bool GuidanceReady { get; } }

internal static class CoreCasterGuidance
{
    internal static Vector2? Target(Projectile p, float range = 650)
    {
        NPC best = null; float nearest = range * range;
        foreach (NPC npc in Main.ActiveNPCs) {
            float distance = npc.DistanceSQ(p.Center);
            if (distance < nearest && npc.CanBeChasedBy(p) && CoreCasterRules.Clear(p.Center, npc.Center))
            { best = npc; nearest = distance; }
        }
        return best?.Center;
    }
    internal static void Write(BinaryWriter w, CoreCasterFlight flight)
    { w.Write(flight != null); flight?.Write(w); }
    internal static CoreCasterFlight Read(BinaryReader r) => r.ReadBoolean() ? CoreCasterFlight.Read(r) : null;
}
