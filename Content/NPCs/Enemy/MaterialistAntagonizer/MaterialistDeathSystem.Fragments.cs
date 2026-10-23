using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;

public sealed partial class MaterialistDeathSystem
{
    private struct Fragment
    {
        internal Vector2 Position, Velocity;
        internal float Rotation, Spin, Size;
        internal Color Tint;
        internal Rectangle Frame;
        internal int Age, Life;
        internal bool Plate;
    }
    private readonly Fragment[] fragments = new Fragment[MaterialistDeathRules.ParticleCapacity];
    private int cursor;
    private bool HasFragments
    {
        get { foreach (Fragment fragment in fragments) if (fragment.Life > 0) return true; return false; }
    }
    private void ClearFragments() { Array.Clear(fragments); cursor = 0; }
    private void EmitFragments(Vector2 point, int count, float power, int seed, bool terminal)
    {
        int limit = MaterialistVisuals.Reduced ? 224 : fragments.Length;
        for (int i = 0; i < count; i++)
        {
            int key = seed * 97 + i * 31;
            float angle = MaterialistDeathRules.Hash(key) * MathHelper.TwoPi;
            Vector2 axis = angle.ToRotationVector2();
            bool plate = terminal && i % 7 == 0;
            float speed = MathHelper.Lerp(4, terminal ? 22 : 12, MaterialistDeathRules.Hash(key + 1)) * power;
            int life = (int)MathHelper.Lerp(32, terminal ? 96 : 54, MaterialistDeathRules.Hash(key + 2));
            fragments[cursor++ % limit] = new Fragment
            {
                Position = point + axis * MathHelper.Lerp(4, 32, MaterialistDeathRules.Hash(key + 3)),
                Velocity = axis * speed, Rotation = angle, Spin = (MaterialistDeathRules.Hash(key + 4) - .5f) * .17f,
                Size = MathHelper.Lerp(.8f, 1.9f, MaterialistDeathRules.Hash(key + 5)) * power,
                Tint = MaterialistDeathRules.Tint(i + seed), Plate = plate,
                Frame = new Rectangle((int)(MaterialistDeathRules.Hash(key + 6) * 210),
                    (int)(MaterialistDeathRules.Hash(key + 7) * 94), 12, 10),
                Life = Math.Min(life, Math.Max(1, MaterialistDeathRules.VisualEnd - age))
            };
        }
    }
    private void UpdateFragments()
    {
        for (int i = 0; i < fragments.Length; i++)
        {
            ref Fragment p = ref fragments[i];
            if (p.Life == 0) continue;
            if (++p.Age >= p.Life) { p.Life = 0; continue; }
            p.Velocity *= p.Plate ? .982f : .96f;
            p.Velocity.Y += p.Plate ? .18f : -.015f;
            p.Position += p.Velocity;
            if (Vector2.DistanceSquared(p.Position, center) > 520 * 520) { p.Life = 0; continue; }
            p.Rotation += p.Spin;
        }
    }
    private void DrawFragments()
    {
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        Texture2D body = MaterialistVisuals.Asset(MaterialistAntagonizer.AssetRoot + "MaterialistAntagonizer");
        foreach (Fragment p in fragments)
        {
            if (p.Life == 0) continue;
            float fade = MathF.Pow(1 - p.Age / (float)p.Life, 1.5f);
            Vector2 point = p.Position - Main.screenPosition;
            if (p.Plate)
            {
                Main.spriteBatch.Draw(body, point, p.Frame, Color.Lerp(Color.White, p.Tint, .25f) * fade,
                    p.Rotation, p.Frame.Size() * .5f, p.Size * .55f, SpriteEffects.None, 0);
                MaterialistVisuals.Glow(p.Position, p.Tint * (fade * .2f), new Vector2(14 * p.Size));
            }
            else
            {
                Vector2 size = new(2 + p.Size * 1.6f);
                if (!MaterialistVisuals.Reduced && p.Age % 3 == 0)
                    MaterialistVisuals.Glow(p.Position, p.Tint * (fade * .22f), size * 5);
                Main.spriteBatch.Draw(pixel, point, new Rectangle(0, 0, 1, 1), (p.Tint with { A = 0 }) * fade,
                    p.Rotation, new Vector2(.5f), size, SpriteEffects.None, 0);
                Main.spriteBatch.Draw(pixel, point, new Rectangle(0, 0, 1, 1), (Color.White with { A = 0 }) * (.8f * fade),
                    p.Rotation, new Vector2(.5f), size * .4f, SpriteEffects.None, 0);
                if (!MaterialistVisuals.Reduced && p.Velocity.LengthSquared() > 36)
                    MaterialistVisuals.Glow(p.Position - p.Velocity * .5f, p.Tint * (.25f * fade),
                        new Vector2(p.Velocity.Length() * 1.7f, 2 + p.Size), p.Velocity.ToRotation());
            }
        }
    }
}
