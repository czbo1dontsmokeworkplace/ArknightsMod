using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

// The ship-81 graph is now the shared client-only Phoenix renderer. Existing
// Reaper calls keep their palette path; these adapters never create damage.
internal sealed class PhoenixFxStyle
{
    internal Func<Vector4,Vector4> Tint;
    internal Func<float> Playback;
    internal Func<float> Opacity;
    internal float TrailLifetimeScale=1;
    internal float Scale=1;
    internal float ParticleLifetimeScale=1;
    internal float SoundVolume=1;
    internal bool SkyfireRing,CenteredCut;
    internal bool MonochromeTexture;
    internal bool MonochromeHalo;
    internal bool HideScuuxunPointer;
    internal bool SuppressShake;
    internal Func<Vector2,Vector2> ProjectPosition,ProjectDirection;
    internal Vector2 SpriteStretch=Vector2.One;
}

internal static class PhoenixFx
{
    internal static PhoenixFxStyle Original(float scale=1)=>new(){Tint=static c=>c,Scale=scale};
    internal static PhoenixFxStyle Colored(Color primary,Color edge,float scale=1)=>new(){Scale=scale,Tint=c=>
    {
        float peak=Math.Max(c.X,Math.Max(c.Y,c.Z));if(peak<=0)return c;
        float saturation=(peak-Math.Min(c.X,Math.Min(c.Y,c.Z)))/peak;
        // Source beams are often pale/desaturated. Keep the operator's main
        // hue dominant instead of mapping every pale source layer to the edge.
        float highlight=Math.Clamp((.4f-saturation)*1.5f,0,.6f);
        Vector3 color=Vector3.Lerp(primary.ToVector3(),edge.ToVector3(),highlight);
        color*=peak/Math.Max(.001f,Math.Max(color.X,Math.Max(color.Y,color.Z)));
        return new Vector4(color,c.W);
    }};
    internal static PhoenixFxStyle BlastCaster(Color primary,Color edge,float scale=1)
    {
        var style=Colored(primary,edge,scale);
        // Shorten only the optical particles, not emitter cadence or gameplay.
        style.ParticleLifetimeScale=.3f;
        return style;
    }
    internal static PhoenixFxStyle NecrassGamma(bool muzzle=false)
    {
        var style=Original(muzzle?1.5f:1);
        style.SuppressShake=true;
        style.MonochromeTexture=true;
        // Keep this dark palette's actual intensity instead of normalizing it
        // back to the imported pink/white HDR peak.
        style.Tint=c=>new Vector4(new Vector3(83,35,132)/255f*Math.Max(c.X,Math.Max(c.Y,c.Z)),c.W);
        if(muzzle)style.Opacity=static()=>.5f;
        return style;
    }
    internal static void Burst(string name,Vector2 at,Vector2 direction,PhoenixFxStyle style=null)
    {
        if(Main.dedServ)return;style??=Original();
        ReaperSwarmVisuals.Spawn(name,()=>at,()=>direction.Unit(),ReaperSwarmVisuals.BurstLife(name),
            scale:new Vector2(style.Scale),style:style);
    }
    internal static void Sakura(Vector2 at,Vector2 direction,Color primary,Color edge,float scale=1)
    {
        var style=Colored(primary,edge,scale);style.CenteredCut=true;style.SuppressShake=true;
        Burst("emitter.player.weapon.40.impact",at,direction,style);
    }
    internal static void Skyfire(Vector2 at,float scale,Vector2 direction=default)
    {
        var style=Original(scale);style.SkyfireRing=true;
        Burst("emitter.player.weapon.24.impact.shockwave",at,direction.LengthSquared()>.000001f?direction.Unit():-Vector2.UnitY,style);
    }
}

internal sealed class PhoenixMotionFx
{
    private readonly Projectile host;
    private readonly PhoenixFxStyle style;
    private readonly ReaperSwarmVisualNode root;
    private ReaperSwarmVisualNode activation;
    internal PhoenixMotionFx(int ship,Projectile host,PhoenixFxStyle style=null,Func<Vector2> direction=null,Func<Vector2> position=null)
    {
        this.host=host;this.style=style??PhoenixFx.Original();
        root=ReaperSwarmVisuals.Spawn("actor.arknights.projectile."+ship,position??(()=>host.Center),direction??(()=>host.velocity.Unit()),
            active:()=>host.active,scale:new Vector2(this.style.Scale),style:this.style);
    }
    internal void Activate(string name)
    {
        if(activation!=null)return;
        activation=ReaperSwarmVisuals.Spawn(name,()=>host.Center,()=>host.velocity.Unit(),active:()=>host.active,
            scale:new Vector2(style.Scale),style:style);
    }
    internal void Stop(){root?.Stop();activation?.Stop();}
}

// The source rank-6 NC-271 shot is 7 rows x 3 cells, all travelling
// together on parallel paths. A cell is not the whole plasma cluster.
internal sealed class PhoenixPlasmaClusterFx
{
    private readonly List<PhoenixMotionFx> cells=new();
    internal PhoenixPlasmaClusterFx(Projectile host,PhoenixFxStyle style,int cellCount=21)
    {
        for(int row=0;row<7;row++)for(int barrel=0;barrel<3;barrel++)
        {
            // Four corner cells removed for Lappland: round(21 * .8) = 17.
            if(cellCount == 17 && (row == 0 || row == 6) && barrel != 1) continue;
            Vector2 offset=new((barrel-1)*12,30-row*(60f/7)-((barrel+1)%3)*5);
            cells.Add(new PhoenixMotionFx(69,host,style,
                ()=>host.velocity.Unit(),
                ()=>host.Center+ReaperSwarmJson.Local(offset*style.Scale,host.velocity.Unit())));
        }
    }
    internal void Stop(){foreach(var cell in cells)cell.Stop();}
}

internal sealed class PhoenixBeamFx
{
    private const int MaximumAlongBeamEmitters = 32;
    private readonly Func<Vector2> directionFallback;
    private readonly Func<Vector2> from,to;
    private readonly Func<bool> active;
    private readonly string source;
    private readonly PhoenixFxStyle style;
    private readonly List<ReaperSwarmVisualNode> roots=new();
    private readonly Func<bool> emitting;
    private readonly float maxLength,spacing;
    private Vector2 origin, direction;
    internal float Length { get; private set; }
    private int nextEmitter;
    private readonly string along;
    private bool stopped;
    private bool Alive=>!stopped && active();
    internal PhoenixBeamFx(string source,Func<Vector2> from,Func<Vector2> to,Func<bool> active,float maxLength,PhoenixFxStyle style=null,Func<Vector2> directionFallback=null,Func<bool> emitting=null)
    {
        this.source=source;this.from=from;this.to=to;this.active=active;this.style=style??PhoenixFx.Original();this.directionFallback=directionFallback;
        this.maxLength=maxLength;this.emitting=emitting;
        if(source=="projectile.player.17.1")this.style.Playback=()=>stopped?2:1;
        var data=ReaperSwarmData.Get(source);
        RefreshGeometry();
        var body=ReaperSwarmVisuals.Spawn(source,()=>origin,()=>direction,active:()=>Alive,scale:new Vector2(this.style.Scale),visible:()=>Emitting,style:this.style);
        if(body!=null){body.BeamLength=()=>Length;roots.Add(body);}
        float minimumSpacing = source == "projectile.player.17.1" ? 40f : 1f;
        spacing=Math.Max(Math.Max(minimumSpacing,data.F("distance_between_emitters_along_beam",30)),maxLength/MaximumAlongBeamEmitters);
        along=data.S("emitter_along_beam");
        Extend();
    }
    private bool Emitting=>Alive && (emitting?.Invoke()??true);
    private void RefreshGeometry()
    {
        origin=from();
        Vector2 delta=to()-origin;
        float distance=delta.Length();
        Length=float.IsFinite(distance)?Math.Min(maxLength,distance):0f;
        direction=distance>.001f && float.IsFinite(distance) ? delta/distance : (directionFallback?.Invoke()??Vector2.UnitX).Unit();
    }
    // Growing packets allocate only the portion actually reached by their head.
    // Existing emitters pause without accumulating a catch-up emission burst.
    internal void Extend()
    {
        if(stopped)return;
        RefreshGeometry();
        if(string.IsNullOrEmpty(along))return;
        for(;nextEmitter<MaximumAlongBeamEmitters && nextEmitter*spacing<Length;nextEmitter++)
        {
            float distance=nextEmitter*spacing;
            var emitter=ReaperSwarmVisuals.Spawn(along,()=>origin+direction*distance,()=>direction,
                active:()=>Alive,scale:new Vector2(style.Scale),visible:()=>Emitting && distance<Length,style:style);
            if(emitter!=null)roots.Add(emitter);
        }
    }
    internal void Hit(Vector2 at)
    {
        var data=ReaperSwarmData.Get(source);
        PhoenixFx.Burst(data.S("emitter_on_hit"),at,-direction,style);
        PhoenixFx.Burst(data.S("actor_on_hit"),at,-direction,style);
    }
    internal void HitAlong(float distance)
    {
        var data=ReaperSwarmData.Get(source);
        foreach(string name in new[]{data.S("emitter_on_hit"),data.S("actor_on_hit")})
            if(!string.IsNullOrEmpty(name))
                ReaperSwarmVisuals.Spawn(name,()=>origin+direction*Math.Clamp(distance,0,Length),
                    ()=>-direction,ReaperSwarmVisuals.BurstLife(name),active:()=>Alive,
                    scale:new Vector2(style.Scale),style:style);
    }
    internal void Stop(){if(stopped)return;stopped=true;foreach(var node in roots)node.Stop();}
}

// Aligned damage packets share one optical lane. Distinct owners, skills,
// origins, turns and relay legs stay separate; collision remains per packet.
internal sealed class PhoenixBeamPulse
{
    private sealed class Lane
    {
        internal int Owner,Variant,Users;
        internal Vector2 Origin,Axis;
        internal float Length,Range,RetireAt;
        internal bool Stopped;
        internal PhoenixBeamFx Beam;
    }
    private static readonly List<Lane> lanes=new();
    private static float clock;
    private readonly Lane lane;
    private bool released;
    private PhoenixBeamPulse(Lane lane)=>this.lane=lane;
    internal static PhoenixBeamPulse Acquire(int owner,int variant,Vector2 origin,Vector2 direction,float range,PhoenixFxStyle style)
    {
        direction=direction.Unit();
        Lane found=lanes.Find(l=>!l.Stopped && l.Owner==owner && l.Variant==variant
            && Math.Abs(l.Range-range)<.1f && Vector2.DistanceSquared(l.Origin,origin)<1
            && Vector2.DistanceSquared(l.Axis,direction)<.000001f);
        if(found==null)
        {
            found=new Lane{Owner=owner,Variant=variant,Origin=origin,Axis=direction,Range=range};
            var capture=found;
            found.Beam=new PhoenixBeamFx("projectile.player.95.beam",()=>capture.Origin,()=>capture.Origin+capture.Axis*capture.Length,
                ()=>!capture.Stopped,range,style,()=>capture.Axis,
                ()=>capture.Users>0 || clock<capture.RetireAt);
            lanes.Add(found);
        }
        // Keep the reached beam visible across gaps between damage packets.
        // Resetting its length on every volley caused full-beam brightness flashes.
        found.Users++;
        return new(found);
    }
    internal void Advance(Vector2 head)
    {
        if(released)return;
        lane.Length=Math.Max(lane.Length,Math.Clamp(Vector2.Dot(head-lane.Origin,lane.Axis),0,lane.Range));
    }
    internal void Stop()
    {
        if(released)return;released=true;
        if(--lane.Users==0)lane.RetireAt=clock+.1f;
    }
    internal static void Update(float dt)
    {
        clock+=dt;
        for(int i=lanes.Count-1;i>=0;i--)
        {
            var l=lanes[i];
            if(l.Users==0 && clock>=l.RetireAt){l.Stopped=true;l.Beam.Stop();lanes.RemoveAt(i);}
            else l.Beam.Extend();
        }
    }
    internal static void Clear()
    {
        foreach(var l in lanes){l.Stopped=true;l.Beam.Stop();}
        lanes.Clear();clock=0;
    }
}
