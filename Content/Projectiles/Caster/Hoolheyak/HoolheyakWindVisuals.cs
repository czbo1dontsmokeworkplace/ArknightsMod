using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Caster.Hoolheyak;

// The wind fields share the energy spell's textured clouds, palette and particle budget.
internal static class HoolheyakWindVisuals
{
    internal static void Bolt(Vector2 center, float rotation, float age, float scale)
    {
        if (CoreCasterVisuals.Ready()) CoreCasterArt.Head(Main.spriteBatch, center - Main.screenPosition,
            rotation.ToRotationVector2(), CoreCasterKind.Hoolheyak, scale * 17, age / 60, 0);
    }
    internal static void Vortex(Vector2 center, float age, float scale, float opacity, int seed)
    {
        if (opacity > 0 && CoreCasterVisuals.Ready()) CoreCasterArt.WindField(Main.spriteBatch,
            center - Main.screenPosition, age, scale, opacity, seed);
    }
    internal static void Mote(Vector2 position, Vector2 velocity, float scale) => CoreCasterVisuals.WindMote(position, velocity, scale, false);
    internal static void WindSpark(Vector2 position, Vector2 velocity, float scale) => CoreCasterVisuals.WindMote(position, velocity, scale, true);
    internal static void Burst(Vector2 center, Vector2 velocity, int count)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < count; i++) {
            Vector2 outward = Main.rand.NextVector2CircularEdge(1, 1);
            Mote(center + outward * 10, outward * Main.rand.NextFloat(1, 3) + velocity * .1f - Vector2.UnitY, .75f);
            WindSpark(center + outward * 8, outward.RotatedBy(.7f) * Main.rand.NextFloat(2, 4), .9f);
        }
    }
}
