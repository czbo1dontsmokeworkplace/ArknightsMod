using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using ArknightsMod.Common.VisualEffects;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_FireSmoke_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 origin, launch;
    private readonly float size, travel, curl;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_FireSmoke_Particle(Vector2 center, Vector2 velocity, float size, int life, float travel)
    {
        Position = origin = center; launch = velocity; this.size = MathHelper.Clamp(size, 8, 70);
        Lifetime = Math.Clamp(life, 8, 40); this.travel = MathHelper.Clamp(travel, 8, 220);
        curl = Main.rand.NextFloat(-.55f, .55f);
        Rotation = velocity.ToRotation() + MathHelper.PiOver2;
    }
    public override void Update()
    {
        float t = LifetimeRatio;
        Vector2 offset = launch.RotatedBy(curl * t) * ((1f - MathF.Pow(.94f, Time)) / .06f) - Vector2.UnitY * Time * t * .35f;
        if (offset.LengthSquared() > travel * travel) offset = offset.SafeNormalize(-Vector2.UnitY) * travel;
        Position = origin + offset;
        Rotation += curl * .025f;
        float envelope = MathHelper.Clamp(Time / 3f, 0, 1) * MathF.Pow(1 - t, 1.2f);
        Color = Color.Lerp(new Color(255, 218, 113), new Color(235, 49, 17), t) * envelope;
    }
    public override void Draw()
    {
        float t = LifetimeRatio;
        Vector2 dimensions = new(size * (.65f + .5f * t), size * (1f + t * .35f));
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, Rotation,
            Texture.Size() * .5f, dimensions / Texture.Size(), SpriteEffects.None, 0f);
    }
}
