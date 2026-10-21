using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public enum TideParticle : byte { Lumen, Droplet, Foam, Bubble, Mist, Prism, Ripple, Note, Wake }

// Authored alpha/luminance atlas; all hue lives in the material. No third-party assets.
public static class TidalParticles
{
    public const int Capacity = 1400;
    private const int FlightCapacity = 1050;
    private struct Particle
    {
        public TideParticle Kind;
        public Vector2 Position, Velocity, Size;
        public Color Color;
        public float Rotation, Spin, Seed;
        public int Life, Maximum;
        public SpriteEffects Flip;
    }
    private static readonly List<Particle> particles = new(Capacity);
    public static int Count => particles.Count;
    public static void Clear() => particles.Clear();
    public static void DisposeTextures() { } // ModContent owns the atlas.
    private static int Cell(TideParticle kind) => kind switch
    {
        TideParticle.Droplet => 0, TideParticle.Foam => 1, TideParticle.Mist => 2,
        TideParticle.Prism => 3, TideParticle.Ripple => 4, TideParticle.Wake => 5,
        TideParticle.Lumen => 6, TideParticle.Bubble => 7, _ => 8
    };
    private static Rectangle Frame(Texture2D atlas, int cell) => new(cell % 3 * (atlas.Width / 3), cell / 3 * (atlas.Height / 3), atlas.Width / 3, atlas.Height / 3);
    public static void Emit(TideParticle kind, Vector2 position, Vector2 velocity, Vector2 size, Color color, int life,
        float rotation = 0, bool impact = false)
    {
        if (Main.dedServ || particles.Count >= (impact ? Capacity : FlightCapacity)) return;
        if (!float.IsFinite(position.X + position.Y + velocity.X + velocity.Y)) return;
        float speed = kind switch { TideParticle.Droplet => 1.85f, TideParticle.Prism => 2.65f, TideParticle.Foam => 1.65f,
            TideParticle.Wake => 2.1f, TideParticle.Lumen => 2.3f, TideParticle.Mist => 1.6f, _ => 1.25f };
        float expansion = kind switch { TideParticle.Mist => 1.8f, TideParticle.Foam => 1.7f, TideParticle.Wake => 1.5f, _ => 1.25f };
        if (color.R > 180 && color.G > 180 && color.B > 180) color = IsharmlaVisuals.Crest;
        int maximum = Math.Max(1, (int)(life * (kind is TideParticle.Droplet or TideParticle.Prism or TideParticle.Foam ? .70f : .9f)));
        particles.Add(new Particle { Kind = kind, Position = position, Velocity = velocity * speed, Size = size * expansion, Color = color,
            Life = maximum, Maximum = maximum, Rotation = rotation, Spin = Main.rand.NextFloat(-.055f, .055f),
            Seed = Main.rand.NextFloat(MathHelper.TwoPi), Flip = Main.rand.NextBool() && kind is TideParticle.Foam or TideParticle.Mist ? SpriteEffects.FlipHorizontally : SpriteEffects.None });
    }
    public static void Burst(Vector2 center, int count)
    {
        if (Main.dedServ) return;
        Emit(TideParticle.Ripple, center, Vector2.Zero, new Vector2(36), IsharmlaVisuals.Azure, 35, impact: true);
        Emit(TideParticle.Lumen, center, Vector2.Zero, new Vector2(62, 32), IsharmlaVisuals.Cyan, 14, impact: true);
        for (int i = 0; i < count; i++)
        {
            Vector2 velocity = Main.rand.NextVector2Circular(23, 19);
            Emit(i % 4 == 0 ? TideParticle.Prism : i % 3 == 0 ? TideParticle.Foam : TideParticle.Droplet,
                center, velocity, new Vector2(Main.rand.NextFloat(5, 11)), i % 4 == 0 ? IsharmlaVisuals.Gold : IsharmlaVisuals.Cyan,
                Main.rand.Next(24, 47), impact: true);
            if (i % 5 == 0) Emit(TideParticle.Mist, center, velocity * .32f, new Vector2(32, 25), IsharmlaVisuals.Azure, 48, impact: true);
        }
    }
    public static void Splash(Vector2 point, bool entering, float power = 1)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < 4; i++) Emit(TideParticle.Ripple, point, Vector2.Zero,
            new Vector2(65 + i * 28, 10 + i * 5) * power, i % 2 == 0 ? IsharmlaVisuals.Cyan : IsharmlaVisuals.Azure, 35 + i * 5, impact: true);
        for (int i = 0; i < 90; i++)
        {
            Vector2 velocity = new(Main.rand.NextFloat(-21, 21) * power, Main.rand.NextFloat(entering ? -22 : -32, -5) * power);
            Vector2 origin = point + new Vector2(Main.rand.NextFloat(-90, 90) * power, Main.rand.NextFloat(-12, 6));
            Emit(i % 3 == 0 ? TideParticle.Foam : TideParticle.Droplet, origin, velocity, new Vector2(Main.rand.NextFloat(5, 12)),
                i % 4 == 0 ? IsharmlaVisuals.Azure : IsharmlaVisuals.Cyan, Main.rand.Next(27, 58), impact: true);
            if (i % 7 == 0) Emit(TideParticle.Mist, origin, velocity * .2f, new Vector2(48, 29) * power,
                IsharmlaVisuals.Azure, Main.rand.Next(40, 67), impact: true);
        }
    }
    public static void Update()
    {
        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle p = particles[i];
            if (--p.Life <= 0) { particles.RemoveAt(i); continue; }
            float age = p.Maximum - p.Life;
            switch (p.Kind)
            {
                case TideParticle.Droplet:
                    p.Velocity.Y += .28f; p.Velocity.X *= .988f; p.Rotation = p.Velocity.ToRotation() + .65f; break;
                case TideParticle.Foam:
                    p.Velocity *= .93f; p.Velocity.Y += .07f; p.Rotation += p.Spin; break;
                case TideParticle.Bubble:
                    p.Velocity.Y -= .026f; p.Velocity.X += MathF.Sin(age * .17f + p.Seed) * .043f; p.Velocity *= .987f; break;
                case TideParticle.Mist:
                    p.Velocity = p.Velocity.RotatedBy(MathF.Sin(age * .055f + p.Seed) * .016f) * .955f;
                    p.Size = Vector2.Min(p.Size * 1.016f, new Vector2(80, 65)); p.Rotation += p.Spin * .35f; break;
                case TideParticle.Prism:
                    p.Velocity *= .95f; p.Rotation = p.Velocity.ToRotation() + MathHelper.PiOver2; break;
                case TideParticle.Ripple:
                    // A short expanding accent, never a screen-sized rotating sprite.
                    float ratio = p.Size.Y / Math.Max(1, p.Size.X);
                    p.Size.X = Math.Min(112, p.Size.X + 2.8f);
                    p.Size.Y = Math.Min(96, p.Size.X * ratio); break;
                case TideParticle.Note:
                    p.Velocity.X += MathF.Sin(age * .12f + p.Seed) * .035f; p.Velocity.Y -= .02f; p.Rotation = MathF.Sin(age * .09f) * .2f; break;
                case TideParticle.Wake:
                    p.Velocity *= .91f; p.Size.X *= 1.025f; p.Size.Y *= .983f; break;
                default:
                    p.Velocity = p.Velocity.RotatedBy(.025f) * .955f; break;
            }
            p.Position += p.Velocity; particles[i] = p;
        }
    }
    public static void Draw()
    {
        if (Main.dedServ || particles.Count == 0) return;
        var batch = Main.spriteBatch; Texture2D atlas = IsharmlaVisuals.Art("ParticleAtlasV2");
        for (int pass = 0; pass < 3; pass++)
        {
            Effect shader = IsharmlaVisuals.ParticleMaterial(pass, atlas);
            batch.Begin(SpriteSortMode.Deferred, pass == 2 ? BlendState.Additive : BlendState.AlphaBlend,
                SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, shader, Main.GameViewMatrix.TransformationMatrix);
            foreach (Particle p in particles)
            {
                bool spark = p.Kind is TideParticle.Lumen or TideParticle.Prism or TideParticle.Note;
                if (pass == 0 && spark || pass == 1 && spark || pass == 2 && !spark) continue;
                float progress = 1 - p.Life / (float)p.Maximum;
                float fade = Math.Min(1, progress * 28 + .35f) * (1 - MathHelper.SmoothStep(0, 1, MathHelper.Clamp((progress - .55f) / .45f, 0, 1)));
                Vector2 size = p.Size;
                if (p.Kind == TideParticle.Droplet) size *= new Vector2(1, 1 + p.Velocity.Length() * .025f);
                if (p.Kind == TideParticle.Prism) size *= new Vector2(.55f, 1.5f + p.Velocity.Length() * .07f);
                if (p.Kind == TideParticle.Prism) size = Vector2.Min(size, new Vector2(12, 28));
                if (p.Kind == TideParticle.Droplet) size = Vector2.Min(size, new Vector2(18, 36));
                if (p.Kind == TideParticle.Mist) fade *= .86f;
                if (pass == 1) fade *= p.Kind is TideParticle.Bubble or TideParticle.Ripple ? .95f : .8f;
                if (pass == 1 && p.Kind == TideParticle.Mist) fade *= .48f;
                Rectangle frame = Frame(atlas, Cell(p.Kind));
                batch.Draw(atlas, p.Position - Main.screenPosition, frame, p.Color * fade, p.Rotation,
                    frame.Size() * .5f, size * 2 / frame.Size(), p.Flip, 0);
                if (pass == 1 && p.Kind == TideParticle.Droplet && p.Velocity.LengthSquared() > 100)
                {
                    Rectangle streak = Frame(atlas, 6);
                    batch.Draw(atlas, p.Position - p.Velocity * .7f - Main.screenPosition, streak, p.Color * (fade * .55f), p.Velocity.ToRotation(),
                        streak.Size() * .5f, new Vector2(p.Velocity.Length() * 2, size.Y * .65f) / streak.Size(), SpriteEffects.None, 0);
                }
            }
            batch.End();
        }
    }
}
