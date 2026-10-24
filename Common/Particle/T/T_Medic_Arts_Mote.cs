using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Medic;
using ArknightsMod.Content.Projectiles.Medic.Arts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.Particle.T;

// Independent lifetimes, drift, stretch and dissipation give the stream physical volume.
public sealed class T_Medic_Arts_Mote : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => OperatorTextureEffects.GlowPath;
    public override SamplerState DrawSamplerState => MedicMaterialVfx.UsesLineArt(profile) ? Main.DefaultSamplerState : SamplerState.LinearClamp;
    private readonly int profile;
    private readonly MedicalMoteKind kind;
    private readonly float size, phase;
    internal T_Medic_Arts_Mote(Vector2 position, Vector2 velocity, int profile, MedicalMoteKind kind, float size, int lifetime, float phase)
    {
        Position = position; Velocity = velocity; this.profile = profile; this.kind = kind;
        this.size = size; this.phase = phase; Lifetime = lifetime;
        Rotation = Main.rand.NextFloat(MathHelper.TwoPi);
    }
    public override void Update()
    {
        Velocity *= kind == MedicalMoteKind.Drop ? .985f : .94f;
        if (kind == MedicalMoteKind.Drop) Velocity.Y += .07f;
        else if (kind is MedicalMoteKind.Petal or MedicalMoteKind.Feather)
        {
            Velocity.X += MathF.Sin(Time * .12f + phase) * .018f;
            Velocity.Y -= .012f;
        }
        else if (kind == MedicalMoteKind.Thread) Velocity.Y -= .014f;
        Rotation += kind == MedicalMoteKind.Mist ? .012f : .025f;
    }
    public override void Draw()
    {
        var p = MedicCatalog.Get(profile);
        float t = MathHelper.Clamp(LifetimeRatio, 0, 1), fade = (1 - t) * (1 - t);
        float s = size * (1 - .25f * t);
        if(!MedicMaterialVfx.UsesLineArt(profile))
        {
            if(kind==MedicalMoteKind.Signature)
                MedicMaterialVfx.Body(profile,Position,MedicIdentityVisuals.Size(profile)*(size/50f*(.7f+t*.7f)),0,Time,p.Body,p.Core,fade*.75f);
            else
            {
                int frame=kind switch {MedicalMoteKind.Mist=>11,MedicalMoteKind.Feather=>2,MedicalMoteKind.Petal=>5,
                    MedicalMoteKind.Purity=>7,MedicalMoteKind.Pulse=>9,MedicalMoteKind.Drop=>1,
                    _=>MedicMaterialVfx.Get(profile).ParticleFrame};
                Vector2 extent=frame is 0 or 3 or 6 or 10?new Vector2(s*1.7f,s*.7f):new Vector2(s);
                if(kind==MedicalMoteKind.Mist)extent*=1+t*.8f;
                MedicMaterialVfx.Sprite(frame,Position,extent,Color.Lerp(p.Core,p.Body,t),fade*(kind==MedicalMoteKind.Mist?.22f:.75f),Rotation);
            }
            return;
        }
        switch (kind)
        {
            case MedicalMoteKind.Signature:
                MedicIdentityVisuals.Feedback(Position, profile, size / 50f * (.7f + t * .7f), Time, fade * .75f);
                break;
            case MedicalMoteKind.Mist:
                Texture2D mist = MedicArtsVfx.Texture(MedicArtsVfx.MistPath);
                Main.EntitySpriteDraw(mist, Position - Main.screenPosition, null,
                    MedicArtsVfx.Light(Color.Lerp(p.Body, p.Core, t * .3f), MathF.Sin(MathHelper.Pi * t) * .13f),
                    Rotation, mist.Size() * .5f, new Vector2(size * (1 + t * .8f)) / mist.Size(), SpriteEffects.None);
                break;
            case MedicalMoteKind.Pulse:
                float radius = size * (1 - MathF.Pow(1 - t, 3));
                MedicArtsVfx.Arc(Position, new Vector2(radius, radius * .8f), phase, 0, MathHelper.TwoPi, p.Body, 1, fade * .55f);
                MedicArtsVfx.Glow(Position, new Vector2(size * 1.2f), p.Body, fade * .22f);
                break;
            case MedicalMoteKind.Petal:
            case MedicalMoteKind.Feather:
                MedicArtsVfx.Material("OrbitSmear", Position, new Vector2(s * 1.8f, s * .65f), p.Body, fade * .65f, Rotation);
                MedicArtsVfx.Glow(Position, new Vector2(s * .5f), p.Core, fade * .6f);
                if (kind == MedicalMoteKind.Feather)
                    MedicArtsVfx.Material("LinearSpark", Position, new Vector2(s * 2, s * .22f), p.Core, fade, Rotation);
                break;
            case MedicalMoteKind.Purity:
                MedicArtsVfx.Material("StarCore", Position, new Vector2(s * .65f, s * 1.25f), p.Core, fade, Rotation);
                MedicArtsVfx.Glow(Position, new Vector2(s * 2), p.Body, fade * .25f);
                break;
            case MedicalMoteKind.Thread:
                MedicArtsVfx.Thread(Position, Position + new Vector2(8, 0).RotatedBy(Rotation + phase), p.Body, 1.2f, fade);
                MedicArtsVfx.Glow(Position, new Vector2(s), p.Core, fade);
                break;
            case MedicalMoteKind.Drop:
                float angle = Velocity.ToRotation();
                MedicArtsVfx.Glow(Position, new Vector2(s * (1.2f + Velocity.Length() * .3f), s * .65f), p.Body, fade * .7f, angle);
                MedicArtsVfx.Glow(Position, new Vector2(s * .5f), p.Core, fade);
                break;
            default:
                MedicArtsVfx.Glow(Position, new Vector2(s * 2.5f), p.Body, fade * .25f);
                MedicArtsVfx.Glow(Position, new Vector2(s), p.Core, fade * .7f);
                break;
        }
    }
}
