using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.Particle.T;
using ArknightsMod.Content.Items.Weapons.Medic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

internal enum MedicalTrail { None, Silk, Lance, Mist, Fork, Ember }
internal sealed record MedicalMaterial(int ParticleFrame, int Dust, MedicalTrail Trail, float Width, float Length, int Cadence);

internal static class MedicMaterialVfx
{
    internal const string AtlasPath="ArknightsMod/Content/Textures/MedicVfx/MedicalMaterials";
    // Emissive masks: comet, pearl, sweep, spindle, ragged arc, burst, flame, glints, pulse, fan, ribbon, mist.
    internal static readonly MedicalMaterial[] Designs = [
        new(7,DustID.PinkTorch,MedicalTrail.Lance,5,94,3),
        new(1,DustID.Honey,MedicalTrail.None,0,0,4),
        new(3,DustID.GreenTorch,MedicalTrail.Fork,7,124,2),
        new(2,DustID.TintableDustLighted,MedicalTrail.None,0,0,3),
        new(1,DustID.GoldFlame,MedicalTrail.None,0,0,4),
        new(10,DustID.SilverFlame,MedicalTrail.Silk,12,132,3),
        new(9,DustID.Water,MedicalTrail.Mist,15,106,3),
        new(1,DustID.DungeonWater,MedicalTrail.Silk,20,92,4),
        new(2,DustID.Rain,MedicalTrail.None,0,0,2),
        new(7,DustID.GoldFlame,MedicalTrail.Lance,6,140,3),
        new(5,DustID.Grass,MedicalTrail.None,0,0,5),
        new(2,DustID.TintableDustLighted,MedicalTrail.None,0,0,4),
        new(3,DustID.Electric,MedicalTrail.Fork,5,78,3),
        new(2,DustID.SilverFlame,MedicalTrail.None,0,0,2),
        new(5,DustID.GoldFlame,MedicalTrail.Silk,8,78,3),
        new(6,DustID.PinkTorch,MedicalTrail.Fork,10,105,3),
        new(3,DustID.GreenTorch,MedicalTrail.Lance,12,142,2),
        new(5,DustID.Grass,MedicalTrail.None,0,0,3),
        new(8,DustID.PurpleTorch,MedicalTrail.None,0,0,3),
        new(10,DustID.BlueTorch,MedicalTrail.Silk,17,120,4),
        new(8,DustID.PinkTorch,MedicalTrail.None,0,0,2),
        new(4,DustID.CrimsonTorch,MedicalTrail.Ember,16,108,3),
        new(5,DustID.PinkTorch,MedicalTrail.None,0,0,4),
        new(1,DustID.TintableDustLighted,MedicalTrail.Mist,8,112,3),
        new(6,DustID.GoldFlame,MedicalTrail.Ember,24,154,2),
        new(3,DustID.PinkTorch,MedicalTrail.Lance,9,125,3)
    ];
    internal static bool UsesLineArt(int id) => id is 4 or 10 or 11;
    internal static MedicalMaterial Get(int id)=>Designs[Math.Clamp(id,0,25)];
    internal static void Sprite(int frame,Vector2 at,Vector2 size,Color tint,float opacity,float rotation=0)
    {
        if(Main.dedServ||opacity<=0)return;
        Texture2D atlas=MedicArtsVfx.Texture(AtlasPath);
        Rectangle source=new(frame%4*(atlas.Width/4),frame/4*(atlas.Height/3),atlas.Width/4,atlas.Height/3);
        Main.EntitySpriteDraw(atlas,at-Main.screenPosition,source,MedicArtsVfx.Light(tint,opacity),rotation,
            source.Size()*.5f,size/source.Size(),SpriteEffects.None);
    }
    internal static void Body(int id,Vector2 c,Vector2 size,float angle,float time,Color body,Color core,float opacity)
    {
        if(Main.dedServ)return;
        float pulse=1+MathF.Sin(time*.11f+id)*.07f;
        Vector2 axis=angle.ToRotationVector2(),side=axis.RotatedBy(MathHelper.PiOver2);
        float unit=size.X/64f;
        void P(int frame,Vector2 offset,Vector2 shape,float rotation=0,float strength=1,bool bright=false)
            =>Sprite(frame,c+offset*unit,shape*unit,bright?core:body,opacity*strength,rotation);
        // Composed luminance masks. Only the three explicitly retained operators use glyph bodies.
        switch(id)
        {
            case 0: // Ansel: short soft rescue comet with a pearly nucleus.
                P(0,Vector2.Zero,new(62,25),angle);P(1,axis*17,new(13,11),angle,.8f,true);break;
            case 1: // Myrrh: heavy suspended medicine, small liquid satellites.
                P(1,Vector2.Zero,new(38,42),time*.013f);P(9,new(-8,9),new(28,20),angle,.7f);
                P(8,new(13,-9),new(17,16),0,.65f,true);break;
            case 2: // Sussurro: three staggered compact jets, with independent sharp noses.
                for(int i=-1;i<=1;i++)P(0,side*i*9-axis*Math.Abs(i)*12,new(i==0?70:44,13),angle,.95f,i==0);break;
            case 3: // Silence: medical feather swarm; old quadrotor line icon is gone.
                for(int i=-1;i<=1;i++)P(2,side*i*10-axis*Math.Abs(i)*8,new(37,18),angle+i*.25f,.8f,i==0);
                P(7,-axis*14,new(40,25),angle,.4f);break;
            case 5: // Shining: solid silver lance emerging through dark painted cloth.
                P(10,-axis*12,new(82,43),angle,.5f);P(3,axis*6,new(74,17),angle,.95f,true);
                P(2,-side*6,new(44,19),angle+.15f,.4f);break;
            case 6: // Purestream: layered water impact and two caustic liquid beads.
                P(9,Vector2.Zero,new(52,34),angle-MathHelper.PiOver4);P(1,axis*13,new(23,19),0,.8f,true);
                P(1,-axis*13+side*7,new(14,12),0,.5f);break;
            case 7: // Ceylon: spreading double splash with a broad liquid sheet.
                P(9,side*8,new(56,32),angle-.45f,.8f);P(9,-side*8,new(49,29),angle+.35f,.75f);
                P(1,axis*16,new(18,16),0,.7f,true);break;
            case 8: // Whisperain: vertical drifting rain plume, falling luminous droplets.
                P(2,new(-5,-4),new(26,58),MathHelper.PiOver2,.7f);P(11,new(0,-12),new(42,22),0,.35f);
                for(int i=0;i<3;i++)P(1,new((i-1)*12,9+(time*1.2f+i*7)%23),new(5,14),0,.8f,true);break;
            case 9: // Lumen: golden fire in a soft liquid glow, not a lantern outline.
                P(6,new(0,-5),new(42,32),-MathHelper.PiOver2,.9f);P(1,Vector2.Zero,new(24,31),0,.7f,true);
                P(7,new(0,-10),new(27,46),-MathHelper.PiOver2,.45f);break;
            case 12: // Ptilopsis: discrete crystalline data packets, never a square grid.
                for(int i=-1;i<=1;i++)P(3,side*i*10-axis*(i+1)*6,new(33-i*3,10),angle,.85f,i==0);
                P(7,-axis*12,new(42,24),angle,.35f);break;
            case 13: // Nightingale: two substantial feather wings and a mist core.
                P(2,new(-15,0),new(46,28),-.4f,.9f);P(2,new(14,1),new(46,28),MathHelper.Pi+.4f,.85f);
                P(11,new(0,5),new(32,32),0,.4f,true);break;
            case 14: // Papyrus: golden flowing husks/petals.
                for(int i=0;i<3;i++)P(2,-axis*i*12+side*MathF.Sin(time*.1f+i)*8,new(40-i*5,13),angle+i*.18f,.8f,i==0);break;
            case 15: // Paprika: plump pepper fire pearls around a curved hot stream.
                P(6,Vector2.Zero,new(52,25),angle,.85f);
                for(int i=0;i<3;i++) {float a=time*.16f+i*2.1f;P(1,new(MathF.Cos(a)*16,MathF.Sin(a)*10),new(15,12),a,.8f,i==0);}break;
            case 16: // Mon3tr: substantial jagged crystal head and two trailing splinters.
                P(3,axis*6,new(68,27),angle,1,true);P(3,-axis*16+side*12,new(34,11),angle+.2f,.7f);
                P(3,-axis*21-side*9,new(26,10),angle-.2f,.55f);break;
            case 17: // Chestnut: three freely rotating painted leaves.
                for(int i=0;i<3;i++){float a=time*.07f+i*2.1f;P(5,new(MathF.Cos(a)*12,MathF.Sin(a)*8),new(24,12),a,.9f,i==0);}break;
            case 18: // Honeyberry: two soft berry bodies, contrasted warm highlights.
                P(1,new(-9,MathF.Sin(time*.13f)*5),new(27,29),-.3f,.9f);P(1,new(10,-5),new(21,24),.4f,.85f,true);
                P(7,new(0,8),new(32,23),0,.35f);break;
            case 19: // Mulberry: two wide waving silk folds and fine falling dust.
                P(10,-axis*7+side*5,new(67,26),angle+.1f,.9f);P(10,axis*4-side*4,new(45,17),angle-.2f,.6f,true);break;
            case 20: // Eyjafjalla: filled cloudy bubbles in a soft powder halo.
                P(11,new(-5,2),new(58,45),time*.007f,.35f);P(8,Vector2.Zero,new(57,51),-.15f,.95f);
                P(8,new(-15,-9),new(22,20),time*.01f,.7f,true);break;
            case 21: // Warfarin: a dense swirling blood-vapor crescent, no crescent stroke.
                P(4,Vector2.Zero,new(51,48),time*.035f,.95f);P(1,axis*12,new(20,15),angle,.8f,true);break;
            case 22: // Hibiscus: layered translucent petals, drifting pollen.
                P(5,Vector2.Zero,new(45,43),time*.023f,.95f);P(5,new(-7,4),new(30,24),-time*.035f,.6f,true);break;
            case 23: // Nowell: oblique water-glass droplets with bright separate afterglints.
                P(1,side*7,new(26,36),angle+.5f,.85f);P(9,-side*8,new(27,32),angle-.4f,.65f,true);
                P(7,-axis*14,new(32,17),angle,.35f);break;
            case 24: // Reed: multi-tongue fire, cloth-like trailing smoke and embers.
                P(10,-axis*25,new(100,45),angle,.3f);P(6,Vector2.Zero,new(93,44),angle,.95f);
                P(6,axis*13-side*5,new(45,18),angle-.12f,.8f,true);break;
            case 25: // Closure: dense amethyst needle packet with a white pointed center.
                P(3,Vector2.Zero,new(69,24),angle,.95f);P(0,axis*5,new(63,12),angle,.85f,true);
                P(3,-axis*22+side*8,new(24,9),angle,.5f);break;
        }
    }
    internal static void Emit(Vector2 at,Vector2 velocity,int id,bool burst=false,int amount=1)
    {
        if(Main.dedServ||ParticleManager.activeParticles.Count>=1200)return;
        var d=Get(id);
        int count=Math.Min(Math.Min(amount,18),1200-ParticleManager.activeParticles.Count);
        for(int i=0;i<count;i++)
        {
            Vector2 jitter=Main.rand.NextVector2Circular(1,1);
            var particle=new T_Medic_Material(at+jitter*4,velocity+jitter*(burst?2f:.3f),id,d.ParticleFrame,
                burst?Main.rand.NextFloat(12,24):Main.rand.NextFloat(7,13),burst?Main.rand.Next(22,38):Main.rand.Next(16,28));
            particle.Spawn();
        }
    }
    internal static void Flight(Projectile q,int id,float age)
    {
        if(Main.dedServ)return;
        var d=Get(id);
        if((int)age%d.Cadence!=0)return;
        Vector2 side=q.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
        Vector2 offset=id switch {8=>new Vector2(Main.rand.NextFloat(-16,16),6),13=>side*Main.rand.NextFloat(-20,20),
            18=>side*(MathF.Sin(age*.14f)>0?8:-8),20=>Main.rand.NextVector2Circular(14,10),_=>Vector2.Zero};
        Emit(q.Center+offset,-q.velocity*.07f+(id==8?Vector2.UnitY*.9f:Vector2.Zero),id);
        if((int)age%(d.Cadence*2)==0)
        {
            var color=id<24?MedicCatalog.Get(id).Body:id==24?Color.Orange:Color.HotPink;
            Dust dust=Dust.NewDustPerfect(q.Center+offset,d.Dust,-q.velocity*.09f+Main.rand.NextVector2Circular(.5f,.5f),130,color,.7f);
            dust.noGravity=id is not (1 or 8 or 17);
        }
    }
}
