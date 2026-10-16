using System;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Passenger;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Lightning_Fracture_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly PassengerFissionArc arc;
    private readonly float growth;
    // 该类自己绘制几何；使用已有纹理满足粒子注册入口，不需要额外资源。
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;

    public T_Lightning_Fracture_Particle(Vector2 start, Vector2 end, Color color, float width,
        int seed, int lifetime, float growthFrames)
    {
        Position = start;
        Color = color;
        Lifetime = lifetime;
        growth = growthFrames;
        arc = new PassengerFissionArc(end - start, seed, width);
    }

    public override void Draw() => arc.Draw(Main.spriteBatch, Position, Color, Time, Lifetime, growth);
}
