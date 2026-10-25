using System;
using ArknightsMod.Content.Items.Weapons.Medic;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

// One entry per saved profile, including the legacy Warfarin item. Units are real ticks.
internal sealed record MedicFlight(string Motif, float Speed, float Gravity, float Turn,
    int SeekAfter, float Wave, float Frequency, float Acceleration, float Drag, float Trail, float Width);
internal static class MedicIdentity
{
    internal static readonly MedicFlight[] Flights = [
        new("Rescue needle", 9.2f,.025f,.075f,24,0,0,0,1,60,2),
        new("Herbal ampoule",5.5f,.23f,0,0,0,0,0,.999f,34,5),
        new("Emergency triad",14f,.008f,.10f,16,0,0,.018f,1,112,1.4f),
        new("Feathered medical light",7.8f,.018f,.045f,18,.012f,.12f,.008f,1,45,3),
        new("Reservoir hexagon",6.8f,.04f,.07f,28,0,0,0,.996f,38,5),
        new("Black veil sword",15.5f,.004f,.05f,14,0,0,.025f,1,140,2),
        new("Dolphin ripple",7.4f,.085f,.025f,0,.012f,.13f,0,.999f,100,4),
        new("Water fan",6.6f,.065f,.035f,0,.026f,.09f,0,.998f,82,6),
        new("Umbrella rain",4.8f,.045f,.06f,0,.009f,.17f,0,.998f,105,2),
        new("Lighthouse ember",10.8f,.13f,.085f,0,0,0,0,1,135,2.4f),
        new("Perfume vessel",4.4f,.20f,0,0,.008f,.09f,0,.996f,40,7),
        new("Wind pinwheel",6.2f,.12f,0,0,.03f,.11f,0,.999f,75,4),
        new("Enkephalin glints",8.8f,.16f,0,0,0,0,0,.988f,58,2),
        new("Sanctuary wings",5.2f,.08f,0,0,.024f,.07f,0,.994f,80,6),
        new("Papyrus fold",8.1f,.045f,.035f,30,.024f,.1f,0,1,70,3),
        new("Pepper helix",10.2f,.022f,.04f,26,.035f,.18f,0,1,88,3),
        new("Emerald spine",13.2f,.006f,.12f,20,0,0,.012f,1,110,2),
        new("Chestnut leaf",7.1f,.09f,.035f,36,.04f,.12f,0,.998f,60,4),
        new("Berry twins",6.1f,.055f,.05f,24,.028f,.14f,0,.999f,80,4),
        new("Cleansing silk",10.5f,.012f,.09f,30,0,0,0,1,100,1.5f),
        new("Volcanic bubbles",5.8f,.018f,.065f,34,.016f,.085f,.006f,.999f,48,7),
        new("Crimson transfusion",9.4f,.018f,.055f,22,.03f,.15f,.008f,1,95,3),
        new("Hibiscus blossom",6.9f,.095f,.045f,35,.03f,.085f,0,.999f,50,5),
        new("Paired waterlight",8.6f,.075f,.048f,0,.018f,.11f,0,.999f,96,2.5f)
    ];
    internal static MedicFlight Get(int id) => Flights[Math.Clamp(id,0,Flights.Length-1)];

    internal static Player Patient(Projectile q, float range = 960)
    {
        if (!Main.player.IndexInRange(q.owner)) return null;
        Player owner=Main.player[q.owner], best=null;
        float score=range*range;
        foreach (Player p in Main.ActivePlayers)
        {
            if (!MedicHealingNet.Allied(owner,p) || p.statLife>=p.statLifeMax2 && !MedicTreatment.HasCurableDebuff(p)) continue;
            float distance=p.DistanceSQ(q.Center);
            if (distance<score && Collision.CanHitLine(q.Center,1,1,p.Center,1,1)) { best=p; score=distance; }
        }
        return best;
    }

    internal static void Move(MedicArt art,float step)
    {
        Projectile q=art.Projectile; MedicFlight f=Get(art.ProfileId);
        // Gravity remains present, but acquired guidance supports the art through a rising turn.
        q.velocity.Y=Math.Min(10,q.velocity.Y+f.Gravity*step*(art.PatientTarget>=0?.08f:1f));
        float wave=f.Wave*MathF.Sin(art.Age*f.Frequency+q.identity*.73f);
        if(art.PatientTarget>=0)wave*=.15f;
        q.velocity=q.velocity.RotatedBy(wave*step)*MathF.Pow(f.Drag,step);
        // Tuye stores pressure briefly; Ptilopsis decelerates into a precise field placement.
        if (art.ProfileId==4 && art.Age is > 12 and < 26) q.velocity*=MathF.Pow(.96f,step);
        bool therapist=art.Profile.Branch==MedicBranch.Therapist;
        if (therapist && art.HomingStartAge<0 && q.velocity.Y>0)
        { art.HomingStartAge=art.Age; if(q.owner==Main.myPlayer)q.netUpdate=true; }
        bool seek=therapist ? art.HomingStartAge>=0 : f.SeekAfter>0 && art.Age>=f.SeekAfter;
        if(seek && Main.netMode!=Terraria.ID.NetmodeID.MultiplayerClient && (q.numUpdates==0 && (int)art.Age%12==0 || art.PatientTarget<0))
        {
            int next=Patient(q)?.whoAmI??-1;
            // Keep an acquired wounded ally through the outside of a broad turn.
            if(next<0 && Main.player.IndexInRange(art.PatientTarget))
            {
                Player existing=Main.player[art.PatientTarget];
                if(MedicHealingNet.Allied(Main.player[q.owner],existing)
                    && (existing.statLife<existing.statLifeMax2||MedicTreatment.HasCurableDebuff(existing))
                    && existing.DistanceSQ(q.Center)<1600*1600)next=existing.whoAmI;
            }
            if(next!=art.PatientTarget){art.PatientTarget=next;q.netUpdate=true;}
        }
        if(seek && Main.player.IndexInRange(art.PatientTarget) && Main.player[art.PatientTarget] is {active:true,dead:false} target)
        {
            Vector2 delta=target.Center-q.Center;
            float angle=q.velocity.ToRotation();
            float turn=f.Turn*step*(therapist ? .45f+.55f*Utils.GetLerpValue(art.HomingStartAge,art.HomingStartAge+24,art.Age,true):1);
            if(art.ProfileId==19)turn*=((int)art.Age/8)%2==0?2f:.2f;
            float wanted=delta.ToRotation();
            float next=angle+MathHelper.Clamp(MathHelper.WrapAngle(wanted-angle),-turn,turn);
            float approach=MathHelper.Clamp(delta.Length()*f.Turn*.4f,.65f,f.Speed);
            // Brake into a reversal rather than orbiting outside the patient's contact box.
            approach*=MathHelper.Lerp(.24f,1f,Math.Max(0,Vector2.Dot(delta.SafeNormalize(Vector2.UnitY),q.velocity.SafeNormalize(Vector2.UnitY))));
            float speed=MathHelper.Lerp(q.velocity.Length(),approach,1-MathF.Pow(.90f,step));
            q.velocity=next.ToRotationVector2()*speed;
        }
        if(!seek && f.Acceleration>0 && q.velocity.Length()<f.Speed*1.35f)q.velocity*=1+f.Acceleration*step;
        float max=f.Speed*1.4f;
        if(q.velocity.LengthSquared()>max*max)q.velocity=q.velocity.SafeNormalize(Vector2.UnitY)*max;
    }
}
