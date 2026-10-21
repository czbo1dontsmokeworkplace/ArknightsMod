using System;
using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Earthshaker_Shard : Particle
{
    public override string TexturePath => "Terraria/Images/MagicPixel";
    public override BlendState DrawBlendState => smoke ? BlendState.NonPremultiplied : BlendState.Additive;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly EarthshakerKind kind;
    private readonly Vector2 origin;
    private readonly float size, gravity, spin;
    private readonly bool smoke;
    public T_Earthshaker_Shard(Vector2 point, Vector2 velocity, EarthshakerKind kind, float size, bool smoke=false, float gravity=1)
    {
        Position=origin=point;Velocity=velocity;this.kind=kind;this.size=Math.Clamp(size,3,55);this.smoke=smoke;this.gravity=gravity;
        Lifetime=smoke?42:30;spin=Main.rand.NextFloat(-.15f,.15f);Rotation=Main.rand.NextFloat(MathHelper.TwoPi);
    }
    public override void Update()
    {
        Velocity.X*=smoke?.96f:.985f;Velocity.Y+=(smoke?-.025f:.18f)*gravity;Rotation+=spin;
        if(Vector2.DistanceSquared(origin,Position)>650*650)Kill();
    }
    public override void Draw()
    {
        float fade=Math.Min(1,Time/2f)*MathF.Pow(1-LifetimeRatio,1.5f);
        if(smoke) ChargedOperatorVfx.Sprite("ImpactPetals",Position,new Vector2(size*(1+LifetimeRatio*1.6f),size*.8f),new Color(116,88,61)*(fade*.48f),Rotation);
        else {
            ChargedOperatorVfx.Sprite("ImpactPetals",Position,new Vector2(size,size*.45f),EarthshakerRules.Tint(kind)*(fade*.9f),Rotation);
            ChargedOperatorVfx.Sprite("StarCore",Position,new Vector2(size*.35f),EarthshakerRules.Accent(kind)*(fade*.55f));
        }
    }
}
