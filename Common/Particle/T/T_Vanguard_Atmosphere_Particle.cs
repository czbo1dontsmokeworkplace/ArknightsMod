using System;
using ArknightsMod.Content.Items.Weapons.Vanguard;
using ArknightsMod.Content.Projectiles.Vanguard.Spears;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Three complementary life cycles: dark wake vapor, expanding pressure arcs, branching afterglow.
// Explicit one-pixel sources keep the world-space geometry independent of MagicPixel dimensions.
public sealed class T_Vanguard_Atmosphere_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => mode == 0 ? BlendState.NonPremultiplied : BlendState.Additive;
    private readonly int mode;
    private readonly float radius, turn;
    private readonly Vector2[] branches = new Vector2[5];
    public T_Vanguard_Atmosphere_Particle(Vector2 point, Vector2 velocity, VanguardSpearKind kind, int mode, float radius, int life)
    {
        Position = point; Velocity = velocity; this.mode = Math.Clamp(mode, 0, 2);
        this.radius = Math.Clamp(radius, 6, 38); Lifetime = Math.Clamp(life, 12, 32);
        Color = this.mode == 0 ? Color.Lerp(new Color(26, 22, 32), VanguardSpearRules.Tint(kind), .22f)
            : Color.Lerp(VanguardSpearRules.Tint(kind), kind == VanguardSpearKind.Grani ? new Color(152, 230, 255) : new Color(255, 164, 92), .22f);
        Rotation = velocity.LengthSquared() > .01f ? velocity.ToRotation() : 0;
        turn = (kind == VanguardSpearKind.WildMane ? .07f : .025f) * (Main.rand.NextBool() ? -1 : 1);
        for (int i = 0; i < branches.Length; i++)
            branches[i] = new Vector2(i / 4f * this.radius, i == 0 ? 0 : Main.rand.NextFloat(-.18f, .18f) * this.radius);
    }
    public override void Update()
    {
        Velocity *= mode == 0 ? .96f : .91f;
        if (mode == 0) Velocity.Y -= .025f;
        Rotation += turn;
    }
    private void Segment(Vector2 a, Vector2 b, float width, Color tint)
    {
        Vector2 d = b - a;
        if (d.LengthSquared() < .01f) return;
        Main.spriteBatch.Draw(Texture, a - Main.screenPosition, new Rectangle(0, 0, 1, 1), tint,
            d.ToRotation(), new Vector2(0, .5f), new Vector2(d.Length(), width), SpriteEffects.None, 0);
    }
    public override void Draw()
    {
        float p = LifetimeRatio, fade = (1 - p) * Math.Min(1, Time / 3f);
        if (mode == 0)
        {
            float r = radius * (.55f + .45f * p);
            Texture2D smoke = VanguardSpearVfx.SmokeTexture;
            // Normalized soft-alpha smoke, without visible hard bands on a light background.
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = new Vector2((i - 1) * r * .28f, MathF.Sin(i * 2 + p) * r * .15f).RotatedBy(Rotation);
                Vector2 size = new(r * (1.8f - i * .15f), r * 1.3f);
                Main.spriteBatch.Draw(smoke, Position + offset - Main.screenPosition, null,
                    Color * (fade * .28f), Rotation + i * .4f, smoke.Size() * .5f,
                    size / smoke.Size(), SpriteEffects.None, 0);
            }
            return;
        }
        if (mode == 1)
        {
            float r = radius * (.6f + p * .55f);
            Vector2 previous = Position + (Rotation - .85f).ToRotationVector2() * r;
            for (int i = 1; i <= 12; i++)
            {
                float t = i / 12f;
                Vector2 next = Position + (Rotation - .85f + t * 1.7f).ToRotationVector2() * r;
                Segment(previous, next, 1.4f * MathF.Sin(t * MathHelper.Pi), Color * (fade * .7f));
                previous = next;
            }
            return;
        }
        for (int i = 1; i < branches.Length; i++)
        {
            Vector2 a = Position + branches[i - 1].RotatedBy(Rotation), b = Position + branches[i].RotatedBy(Rotation);
            Segment(a, b, 1.2f * (1 - p), Color * fade);
            if (i == 2)
                Segment(b, b + new Vector2(radius * .25f, -radius * .3f).RotatedBy(Rotation), .8f, Color * (fade * .55f));
        }
    }
}
