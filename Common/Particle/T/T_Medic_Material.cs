using System;
using ArknightsMod.Content.Items.Weapons.Medic;
using ArknightsMod.Content.Projectiles.Medic.Arts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Medic_Material : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath=>MedicMaterialVfx.AtlasPath;
    public override SamplerState DrawSamplerState=>SamplerState.LinearClamp;
    private readonly int profile,materialFrame;
    private readonly float size,spin;
    internal T_Medic_Material(Vector2 position,Vector2 velocity,int profile,int frame,float size,int lifetime)
    {
        Position=position;Velocity=velocity;this.profile=profile;materialFrame=frame;this.size=size;Lifetime=lifetime;
        Rotation=Main.rand.NextFloat(MathHelper.TwoPi);spin=Main.rand.NextFloat(-.055f,.055f);
    }
    public override void Update()
    {
        Velocity*=materialFrame==7?.95f:.92f;
        Velocity.Y+=materialFrame is 2 or 5?.025f:materialFrame is 8 or 11?-.025f:0;
        if(materialFrame is 2 or 5 or 8)Velocity.X+=MathF.Sin(Time*.12f+Rotation)*.025f;
        Rotation+=spin;
    }
    public override void Draw()
    {
        float t=MathHelper.Clamp(LifetimeRatio,0,1),fade=MathF.Sin(MathHelper.Clamp(t*3,0,1)*MathHelper.PiOver2)*MathF.Pow(1-t,1.7f);
        var body=profile<24?MedicCatalog.Get(profile).Body:profile==24?new Color(248,127,54):new Color(243,83,162);
        var core=profile<24?MedicCatalog.Get(profile).Core:Color.White;
        Vector2 extent=materialFrame switch {0 or 3 or 6=>new(size*1.6f,size*.6f),2 or 10=>new(size*1.7f,size*.8f),_=>new(size)};
        if(materialFrame is 8 or 11)extent*=.7f+t*.9f;
        MedicMaterialVfx.Sprite(materialFrame,Position,extent,Color.Lerp(core,body,t),fade*.7f,Rotation);
    }
}
