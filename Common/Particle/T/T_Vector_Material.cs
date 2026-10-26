using ArknightsMod.Content.Projectiles.Sniper.Exusiai;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Vector_Material : Particle
{
    private readonly int variant;
    private readonly float size,gravity,spin;
    public override string TexturePath => "ArknightsMod/Content/Textures/ShpcEnergy/BloomCircle";
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    public T_Vector_Material(Vector2 at,Vector2 velocity,Color color,int variant,float size,int life,float rotation,float gravity,float opacity,float spin)
    {
        Position=at;Velocity=velocity;Color=color;this.variant=variant;this.size=size;Lifetime=life;
        Rotation=rotation;this.gravity=gravity;Opacity=opacity;this.spin=spin;
    }
    public override void Update()
    {
        Velocity*=variant==2?.98f:.94f;
        Velocity.Y+=gravity;
        if(variant==1)Rotation=Velocity.ToRotation();else Rotation+=spin;
        if(variant==2)Velocity.X+=System.MathF.Sin(Time*.18f+Rotation)*.015f;
        if(variant==4)Velocity.Y-=.035f;
    }
    public override void Draw() => VectorArt.Material(Main.spriteBatch,Position-Main.screenPosition,Velocity,
        LifetimeRatio,variant,size,Color,Rotation,Opacity);
}
