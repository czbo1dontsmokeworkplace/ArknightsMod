using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

internal static class CrossbowFlightVfx
{
    internal static void Draw(Projectile p, CrossbowKind kind, Vector2[] history = null, bool drawArrow = true)
        => CrossbowSignatureVfx.Draw(p, kind, history, drawArrow);
    internal static void Wake(Projectile p, CrossbowKind kind)
        => CrossbowSignatureVfx.Wake(p, kind);
}
