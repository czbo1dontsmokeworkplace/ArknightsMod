using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;

namespace ArknightsMod.Common.Particle.T;

// Small detached physical accents for sprite-bodied medical projectiles.
public sealed class T_Medic_Item_Spark : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath=>"ArknightsMod/Common/Particle/DefaultParticle";
    public override SamplerState DrawSamplerState=>SamplerState.PointClamp;
    internal readonly int Profile;
    private readonly bool spark;
    private readonly float size;
    internal T_Medic_Item_Spark(Vector2 position,Vector2 velocity,int profile,Color color,bool spark,int lifetime,float size)
    {
        Position=position;Velocity=velocity;Profile=profile;Color=color;this.spark=spark;Lifetime=lifetime;this.size=size;
        Rotation=velocity.ToRotation();
    }
    public override void Update()
    {
        Velocity*=.94f;
        if(spark)Velocity.Y+=.018f;
        if(Velocity.LengthSquared()>.005f)Rotation=Velocity.ToRotation();
    }
    public override void Draw()
    {
        float fade=MathF.Pow(Math.Max(0,1-LifetimeRatio),1.6f);
        Vector2 extent=spark?new Vector2(size*(.5f+.5f*fade),.8f):new Vector2(size*fade);
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,Position-Main.screenPosition,new Rectangle(0,0,1,1),
            Color*fade,Rotation,new Vector2(.5f),extent,SpriteEffects.None,0);
    }
}
