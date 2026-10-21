using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Short, soft spear impact / moving ember, using the mod's existing tapered mask.
public sealed class T_Vanguard_HitFlare : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => "ArknightsMod/Content/Textures/OperatorVfx/LinearSpark";
    public override BlendState DrawBlendState => BlendState.Additive;
    private readonly float extent;
    private readonly bool impact;
    public T_Vanguard_HitFlare(Vector2 point, Vector2 velocity, Color tint, float extent, int life, bool impact = false)
    {
        Position = point; Velocity = velocity; Color = tint;
        this.extent = Math.Clamp(extent, 4, 48); this.impact = impact;
        Lifetime = Math.Clamp(life, 6, 22); Rotation = velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity *= impact ? .8f : .94f;
        if (!impact) { Velocity.Y += .07f; Rotation = Velocity.ToRotation(); }
    }
    public override void Draw()
    {
        float fade = MathF.Pow(1 - LifetimeRatio, 1.6f);
        float stretch = extent * (impact ? .65f + .55f * MathF.Sin(LifetimeRatio * MathHelper.Pi) : 1);
        Vector2 size = new Vector2(impact ? stretch * .5f : 3.5f, stretch);
        Vector2 origin = Texture.Size() * .5f, position = Position - Main.screenPosition;
        Main.spriteBatch.Draw(Texture, position, null, Color * fade, Rotation + MathHelper.PiOver2,
            origin, size / Texture.Size(), SpriteEffects.None, 0);
        Main.spriteBatch.Draw(Texture, position, null, Microsoft.Xna.Framework.Color.Lerp(Color, Microsoft.Xna.Framework.Color.White, .65f) * fade,
            Rotation + MathHelper.PiOver2, origin, size * new Vector2(.3f, .65f) / Texture.Size(), SpriteEffects.None, 0);
        if (impact)
            Main.spriteBatch.Draw(Texture, position, null, Color * (fade * .65f), Rotation,
                origin, size * new Vector2(.35f, .65f) / Texture.Size(), SpriteEffects.None, 0);
    }
}
