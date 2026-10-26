using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace ArknightsMod.Content.Projectiles.Sniper.Exusiai;

// Same renderer is used in game and in the standalone GPU review; no batch state changes.
public static class VectorArt
{
    private static Texture2D muzzle, bullet, impact, streak;
    public static void Assign(Func<string, Texture2D> load)
    { muzzle = load("MuzzleFlash"); bullet = load("PrecisionRound"); impact = load("ImpactFlare"); streak = load("SparkStreak"); }
    public static void Clear() { muzzle = bullet = impact = streak = null; }
    public static Color Theme(int mode) => mode == 3 ? new Color(255, 126, 64) : mode == 2 ? new Color(255, 192, 86) : new Color(255, 211, 123);
    private static Color Add(Color color, float opacity) => new(new Vector4(color.ToVector3() * Math.Clamp(opacity, 0, 1), 0));
    public static void Round(SpriteBatch batch, Vector2 head, Vector2 direction, float travelled, int mode, float opacity = 1)
    {
        if (direction.LengthSquared() < .0001f) direction = Vector2.UnitX;
        else direction.Normalize();
        float length = Math.Clamp(travelled, 0, 150), angle = MathF.Atan2(direction.Y, direction.X) + MathF.PI / 2;
        if (length < 1) return;
        Vector2 center = head - direction * length * .5f;
        Color color = Theme(mode);
        var source = new Rectangle(14, 0, 20, bullet.Height);
        Vector2 origin = new(10, bullet.Height / 2f);
        // Dark silhouette survives daylight. Filled gold ribbon and a narrow white core survive night.
        batch.Draw(bullet, center, source, new Color(47, 29, 24) * (.65f * opacity), angle, origin,
            new Vector2(24f / source.Width, length / bullet.Height), SpriteEffects.None, 0);
        batch.Draw(bullet, center, source, Add(color, 1f * opacity), angle, origin,
            new Vector2(14f / source.Width, length / bullet.Height), SpriteEffects.None, 0);
        batch.Draw(bullet, center + direction * length * .18f, source, Add(new Color(255, 245, 217), 1f * opacity), angle, origin,
            new Vector2(5f / source.Width, length * .55f / bullet.Height), SpriteEffects.None, 0);
    }
    public static void Muzzle(SpriteBatch batch, Vector2 point, float angle, float flash, int mode)
    {
        if (flash <= .01f) return;
        batch.Draw(muzzle, point, null, Add(Theme(mode), .65f * flash), angle, new Vector2(0, muzzle.Height / 2f),
            new Vector2(43f / muzzle.Width, 32f / muzzle.Height), SpriteEffects.None, 0);
        batch.Draw(muzzle, point, null, Add(Color.White, .72f * flash), angle, new Vector2(0, muzzle.Height / 2f),
            new Vector2(23f / muzzle.Width, 14f / muzzle.Height), SpriteEffects.None, 0);
    }
    public static void Aura(SpriteBatch batch, Vector2 center, int facing, int mode, float time, float opacity)
    {
        if (mode < 1 || opacity <= .01f) return;
        Color color = Theme(mode);
        float pulse = .85f + .15f * MathF.Sin(time * 5);
        // Exusiai's warm halo and feather-like firing fins; S3 opens a second feather layer.
        int feathers = mode == 3 ? 6 : mode == 2 ? 4 : 2;
        for (int side = -1; side <= 1; side += 2)
            for (int i = 0; i < feathers; i++)
            {
                float angle = side * (.25f + i * .16f) - MathF.PI / 2;
                Vector2 position = center + new Vector2(side * (14 + i * 3), 3 + i * 4);
                batch.Draw(streak, position, null, Add(color, opacity * pulse * .65f), angle,
                    new Vector2(streak.Width, streak.Height) / 2, new Vector2((32 + i * 4f) / streak.Width, 14f / streak.Height), SpriteEffects.None, 0);
            }
        for (int i = 0; i < 12; i++)
        {
            float angle = i * MathF.Tau / 12 + time * .18f;
            Vector2 position = center + new Vector2(MathF.Cos(angle) * 12, -37 + MathF.Sin(angle) * 3);
            batch.Draw(streak, position, null, Add(color, opacity * .65f), angle + MathF.PI / 2,
                new Vector2(streak.Width, streak.Height) / 2, new Vector2(7f / streak.Width, 9f / streak.Height), SpriteEffects.None, 0);
        }
    }
    public static void Impact(SpriteBatch batch, Vector2 point, Vector2 reverse, float age, int seed, int mode)
    {
        age = Math.Clamp(age, 0, 1);
        float alpha = MathF.Pow(1 - age, 1.5f);
        float rotation = MathF.Atan2(reverse.Y, reverse.X);
        Color color = Theme(mode);
        batch.Draw(impact, point, null, Add(color, alpha * .42f), rotation, new Vector2(impact.Width, impact.Height) / 2,
            new Vector2(22, 15) / new Vector2(impact.Width, impact.Height) * (1 + age), SpriteEffects.None, 0);
        for (int i = 0; i < 8; i++)
        {
            float hash = MathF.Sin(seed * 19.7f + i * 43.3f) * .5f + .5f;
            float angle = rotation - 1.1f + 2.2f * hash;
            Vector2 velocity = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * (15 + hash * 55);
            Vector2 position = point + velocity * age + new Vector2(0, 20 * age * age);
            batch.Draw(streak, position, null, Add(i % 3 == 0 ? Color.White : color, alpha * .8f), angle,
                new Vector2(streak.Width, streak.Height) / 2, new Vector2((8 + hash * 14) / streak.Width, 8f / streak.Height) * (1 - age * .6f), SpriteEffects.None, 0);
        }
    }
}
