using ArknightsMod.Content.Items.Weapons.Charged;
using Microsoft.Xna.Framework;

namespace ArknightsMod.Content.Projectiles.Charged;

// Source pixels, not a universal 45-degree correction. All drawing is transformed
// so that this grip maps to the player's hand and this tip maps to the collision tip.
internal readonly record struct DreadnoughtBladeLayout(Vector2 Grip, Vector2 Tip)
{
    internal static DreadnoughtBladeLayout For(ChargedOperatorKind kind, Vector2 size) => kind switch
    {
        ChargedOperatorKind.Melantha => new(new Vector2(9, size.Y * .5f), new Vector2(size.X - 1, size.Y * .5f)),
        ChargedOperatorKind.Matoimaru => new(size * new Vector2(.11f, .89f), size * new Vector2(.97f, .04f)),
        ChargedOperatorKind.Franka => new(size * new Vector2(.14f, .87f), size * new Vector2(.96f, .04f)),
        ChargedOperatorKind.NearlRadiant => new(size * new Vector2(.10f, .90f), size * new Vector2(.97f, .02f)),
        _ => new(new Vector2(0, size.Y), new Vector2(size.X, 0))
    };

    internal Vector2 Axis(bool flip) => (Tip - Grip) * new Vector2(flip ? -1 : 1, 1);
    internal Vector2 Origin(float width, bool flip) => flip ? new Vector2(width - Grip.X, Grip.Y) : Grip;
}
