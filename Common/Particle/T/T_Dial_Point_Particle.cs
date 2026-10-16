using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using ArknightsMod.Content.Projectiles.Caster.Mostima;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Dial_Point_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly Vector2 center;
    private readonly int track, index, count;
    // Use the local soft light mask, not TemporalOrb (which is a textured explosion).
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_Dial_Point_Particle(Vector2 center, int track, int index, int count)
    {
        this.center = center; this.track = track; this.index = index; this.count = count;
        Lifetime = MostimaClockwork.Lifetime;
        Position = center + MostimaClockwork.Point(track, index, count, 0);
    }
    public override void Update()
    {
        Position = center + MostimaClockwork.Point(track, index, count, Time);
        float glimmer = .72f + .28f * MathF.Sin(index * 1.7f + Time * .14f);
        Color = (track is 1 or 3 ? MostimaClockwork.Brass : track == 4 ? MostimaClockwork.Silver : MostimaVisuals.Blue)
            * (MostimaClockwork.FaceOpacity(Time) * glimmer * (track >= 3 ? .85f : .65f));
    }
    public override void Draw()
    {
        float size = track >= 3 ? 14f : track == 2 ? 11f : 13f;
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null, Color, 0,
            Texture.Size() * .5f, new Vector2(size) / Texture.Size(), SpriteEffects.None, 0f);
    }
}
