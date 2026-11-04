using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.BossArsenal;

internal static class NoxPreludeVisuals
{
    internal static PhoenixFxStyle Style(bool white)
    {
        var style = PhoenixFx.Colored(white ? new Color(198, 211, 246) : new Color(125, 215, 255), Color.White, .75f);
        style.SuppressShake = true;
        if (white) { style.MonochromeTexture = true; style.MonochromeHalo = true; style.Opacity = static () => .8f; }
        return style;
    }
    internal static PhoenixMotionFx Create(Projectile host, bool white)
        => new(95, host, Style(white));
    internal static void Impact(Projectile host, bool white)
        => PhoenixFx.Burst("emitter.player.weapon.95.projectile.impact", host.Center,
            -host.velocity.SafeNormalize(Vector2.UnitX), Style(white));
}
