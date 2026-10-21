using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Reaper_Petal : Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly ReaperKind kind;
    private readonly Vector2? destination;
    private readonly Vector2 origin;
    private readonly float size,spin;
    public T_Reaper_Petal(Vector2 point,Vector2 velocity,ReaperKind kind,int life,float size,Vector2? destination=null)
    {
        Position=origin=point;Velocity=velocity;this.kind=kind;this.destination=destination;
        Lifetime=Math.Clamp(life,12,36);this.size=Math.Clamp(size,3,20);spin=Main.rand.NextFloat(-.1f,.1f);Rotation=velocity.ToRotation();
    }
    public override void Update()
    {
        if(destination is Vector2 target) {
            Vector2 offset=target-Position;
            Velocity=Vector2.Lerp(Velocity,offset.SafeNormalize(Vector2.Zero)*Math.Min(9,offset.Length()*.2f),.22f);
            if(offset.LengthSquared()<7*7)Kill();
        }
        else {Velocity*=.94f;Velocity=Velocity.RotatedBy(spin*.5f);Velocity.Y-=.018f;}
        Rotation+=spin;if(Vector2.DistanceSquared(origin,Position)>300*300)Kill();
    }
    public override void Draw()
    {
        float fade=Math.Min(1,Time/3f)*MathF.Pow(1-LifetimeRatio,1.4f);
        ChargedOperatorVfx.Sprite("ImpactPetals",Position,new Vector2(size,size*.48f),ReaperRules.Color(kind)*fade,Rotation);
        ChargedOperatorVfx.Sprite("StarCore",Position,new Vector2(size*.3f),ReaperRules.Edge(kind)*(fade*.6f));
    }
}
