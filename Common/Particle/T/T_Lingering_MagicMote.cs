using System;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// World-space visual residue, independent of the projectile that deposited it.
public sealed class T_Lingering_MagicMote : Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly Color pigment, highlight;
    private readonly float size, phase, curl;
    private readonly int variant;
    public T_Lingering_MagicMote(Vector2 point,Vector2 drift,Color pigment,Color highlight,int life,float size)
    {
        Position=point;Velocity=drift;this.pigment=pigment;this.highlight=highlight;
        Lifetime=Math.Clamp(life,24,90);this.size=size;
        phase=Main.rand.NextFloat(MathHelper.TwoPi);curl=Main.rand.NextFloat(-.045f,.045f);
        Rotation=phase;variant=Main.rand.Next(3);
    }
    public override void Update()
    {
        Velocity=(Velocity*.94f).RotatedBy(curl);
        Velocity.Y-=.002f;
        Rotation+=curl;
    }
    public override void Draw()
    {
        float t=Math.Clamp(LifetimeRatio,0,1);
        float fade=Math.Min(1,Time/4f)*MathF.Pow(1-t,.75f);
        float pulse=.8f+.2f*MathF.Sin(phase+Time*.15f);
        float radius=size*(.85f+.35f*t);
        // Fade the source alpha once; multiplying both RGB and alpha made the
        // additive residue disappear too early on normal game backgrounds.
        Color Tint(Color color,float opacity)=>new(color.R,color.G,color.B,(byte)(255*Math.Clamp(opacity,0,1)));
        ChargedOperatorVfx.Sprite("OrbitSmear",Position,new Vector2(radius*2.8f,radius*1.9f),Tint(pigment,fade*.09f),Rotation);
        if(variant==0)
            ChargedOperatorVfx.Sprite("ImpactPetals",Position,new Vector2(radius*1.6f,radius*.8f),Tint(pigment,fade*.75f),Rotation);
        else if(variant==1){
            ChargedOperatorVfx.Sprite("StarCore",Position,new Vector2(radius*2.4f,radius*.65f),Tint(highlight,fade*pulse),Rotation);
            ChargedOperatorVfx.Sprite("StarCore",Position,new Vector2(radius*1.25f,radius*.4f),Tint(highlight,fade*pulse*.65f),Rotation+MathHelper.PiOver2);
        }
        else ChargedOperatorVfx.Sprite("StarCore",Position,new Vector2(radius*1.1f),Tint(highlight,fade*pulse),Rotation);
    }
}
