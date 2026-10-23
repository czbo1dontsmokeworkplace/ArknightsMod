using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Common.VisualEffects;

// Body-sized fragments; each motif has its own motion and silhouette. Never a combat trail.
public sealed class T_SkillBodyMote : Particle.Particle
{
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    internal int Owner { get; }
    private readonly SkillBodyProfile profile;
    private readonly float size;
    private Vector2 lastCenter;
    public T_SkillBodyMote(int owner, Vector2 position, Vector2 velocity, SkillBodyProfile profile, int life, float size)
    {
        Owner = owner; Position = position; Velocity = velocity; this.profile = profile;
        Lifetime = life; this.size = size; lastCenter = Main.player[owner].Center;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
    }
    public override void Update()
    {
        Player player = Main.player[Owner];
        if (!player.active || player.dead) { Kill(); return; }
        Vector2 delta = player.Center - lastCenter; lastCenter = player.Center;
        if (delta.LengthSquared() > 160 * 160) { Kill(); return; }
        Position += delta * .85f;
        switch (profile.Motif)
        {
            case SkillBodyMotif.Ember: Velocity *= .96f; Velocity.Y -= .025f * player.gravDir; break;
            case SkillBodyMotif.Spark: Velocity = Velocity.RotatedBy(Time % 4 == 0 ? .7f : -.12f) * .92f; break;
            case SkillBodyMotif.Petal: Velocity.X += MathF.Sin(Time * .25f + Rotation) * .07f; Rotation += .04f; break;
            case SkillBodyMotif.Mist: Velocity *= .94f; Rotation += .02f; break;
            case SkillBodyMotif.Shadow: Velocity *= .9f; Opacity *= .98f; break;
            case SkillBodyMotif.Slag: Velocity.Y += .035f * player.gravDir; Rotation += .07f; break;
            case SkillBodyMotif.Orbit: Velocity = Velocity.RotatedBy(.12f); Rotation += .12f; break;
            default: Velocity *= .94f; Rotation += .025f; break;
        }
        if (Position.DistanceSQ(player.Center) > 90 * 90) Kill();
    }
    private void Dot(Texture2D texture, Vector2 position, Vector2 pixels, Color tint, float rotation)
        => Main.spriteBatch.Draw(texture, position - Main.screenPosition, new Rectangle(0, 0, 1, 1), tint,
            rotation, new Vector2(.5f), pixels, SpriteEffects.None, 0);
    public override void Draw()
    {
        float fade = Math.Min(1, Time / 4f) * MathF.Pow(1 - LifetimeRatio, 1.5f) * Opacity;
        Color tint = profile.Color * fade;
        // Tiny bounded facets use one source texel; alpha participates in real Additive.
        Texture2D pixel = TextureAssets.MagicPixel.Value;
        float haze = profile.Motif is SkillBodyMotif.Mist or SkillBodyMotif.Shadow ? 4f : 2.5f;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, tint * .65f,
            Rotation, Texture.Size() * .5f, new Vector2(size * haze) / Texture.Size(), SpriteEffects.None, 0);
        if (profile.Motif == SkillBodyMotif.Mist) return;
        Vector2 shape = profile.Motif switch {
            SkillBodyMotif.Ember => new(size * .45f, size),
            SkillBodyMotif.Spark => new(size * 1.3f, 1),
            SkillBodyMotif.Crystal => new(size * .5f, size),
            SkillBodyMotif.Petal => new(size, size * .4f),
            SkillBodyMotif.Shadow => new(size * .7f),
            SkillBodyMotif.Slag => new(size * .5f, size * .35f),
            _ => new(size * .45f)
        };
        Dot(pixel, Position, shape, tint * .85f, Rotation);
        Dot(pixel, Position, shape * .4f, Color.Lerp(tint, Color.White * fade, .3f), Rotation);
    }
}
