using System;
using System.Collections.Generic;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.MechAccord;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Mech_Radial_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float diameter;
    public override string TexturePath => "ArknightsMod/Common/Particle/DefaultParticle";
    public override BlendState DrawBlendState => BlendState.Additive;
    internal T_Mech_Radial_Particle(Vector2 point, Vector2 velocity, Color color, float diameter, int lifetime)
    { Position = point; Velocity = velocity; Color = color; this.diameter = diameter; Lifetime = lifetime; }
    public override void Update() { Velocity *= .88f; }
    public override void Draw() => MechAccordVfx.Mask("Bloom", Position, new Vector2(diameter),
        MechAccordVfx.Fade(Color, MathF.Pow(1f - LifetimeRatio, 1.4f)));
}
