using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

// Client-only graph of actors, emitters, particles and trails. Visual particles
// are never Terraria damage projectiles and never consume network projectile IDs.
internal static class ReaperSwarmVisuals
{
    internal static readonly List<ReaperSwarmVisualNode> Nodes=new();
    private static readonly List<ReaperSwarmVisualNode> pending=new();
    private const int MaximumNodes=65536;
    private const int ParticleBudgetPerTick = 32;
    private const int LiveParticleBudget = 512;
    // Dense trails and sparks leave room for the small groups carrying rings and impact cores.
    private const int ReservedPerTick = 8;
    private const int ReservedLiveParticles = 32;
    private static int particlesThisTick, liveParticles;
    private static Vector2 viewMin, viewMax;
    private static bool hasView;
    internal static long OverflowCount;
    internal static ReaperSwarmVisualNode Add(ReaperSwarmVisualNode n)
    {
        if(Nodes.Count+pending.Count>=MaximumNodes) { OverflowCount++;return null; }
        pending.Add(n);
        if (n.IsParticle) { particlesThisTick++; liveParticles++; }
        n.Initialize();
        return n;
    }
    internal static bool CanAddParticle(bool primary)
        => particlesThisTick < ParticleBudgetPerTick - (primary ? 0 : ReservedPerTick)
        && liveParticles < LiveParticleBudget - (primary ? 0 : ReservedLiveParticles)
        && Nodes.Count + pending.Count < MaximumNodes;

    private static void CaptureView()
    {
        Matrix inverse = Matrix.Invert(Main.GameViewMatrix.TransformationMatrix);
        Vector2 topLeft = Vector2.Transform(Vector2.Zero, inverse);
        Vector2 topRight = Vector2.Transform(new(Main.screenWidth, 0), inverse);
        Vector2 bottomLeft = Vector2.Transform(new(0, Main.screenHeight), inverse);
        Vector2 bottomRight = Vector2.Transform(new(Main.screenWidth, Main.screenHeight), inverse);
        viewMin = Main.screenPosition + Vector2.Min(Vector2.Min(topLeft, topRight), Vector2.Min(bottomLeft, bottomRight));
        viewMax = Main.screenPosition + Vector2.Max(Vector2.Max(topLeft, topRight), Vector2.Max(bottomLeft, bottomRight));
        hasView = true;
    }

    internal static bool NearView(Vector2 position, Vector2 scale)
    {
        if (!hasView) CaptureView();
        // Leave room for moving particles, broad explosions and a moving camera.
        float pad = 640f * Math.Max(1f, Math.Max(Math.Abs(scale.X), Math.Abs(scale.Y)));
        return position.X >= viewMin.X - pad && position.X <= viewMax.X + pad
            && position.Y >= viewMin.Y - pad && position.Y <= viewMax.Y + pad;
    }
    internal static ReaperSwarmVisualNode Spawn(string name,Func<Vector2> pos,Func<Vector2> dir,float life=100000,Func<bool> active=null,Vector2? scale=null,float z=0,int depth=0,Func<bool> visible=null,ReaperKind palette=default,PhoenixFxStyle style=null)
    {
        if(Main.dedServ || string.IsNullOrEmpty(name) || depth>16
            || name.StartsWith("emitter.player.weapon.66.muzzle.targeting",StringComparison.Ordinal)
            // Retire the three launch smoke puffs; preserve their imported source data.
            || name=="emitter.player.weapon.81.projectile.spawn.sporecloud")return null;
        var definition=ReaperSwarmData.Definition(name);
        if(definition.Data.ValueKind==JsonValueKind.Undefined)return null;
        if(style?.SuppressShake==true && definition.Kind=="shake")return null;
        return Add(new ReaperSwarmVisualNode(definition.Data,pos,dir,life,active,scale??Vector2.One,z,depth,palette,style,definition){Visible=visible});
    }
    internal static void Attach(JsonElement attachment,Func<Vector2> pos,Func<Vector2> dir,Func<bool> active,int depth=0,Vector2? scale=null,float z=0,Func<bool> visible=null,ReaperKind palette=default,PhoenixFxStyle style=null)
    {
        Vector2 offset=attachment.V("location",Vector2.Zero), orientation=attachment.V("orientation",Vector2.UnitY);
        Vector2 inherited=scale??Vector2.One;
        var child=Spawn(attachment.S("name"),()=>pos()+ReaperSwarmJson.Local(offset*inherited,dir()),()=>ReaperSwarmJson.Local(orientation,dir()).Unit(),100000,active,
            attachment.V("relative_scale",attachment.V("scale",Vector2.One))*inherited,z+attachment.F("relative_z",attachment.F("z_order")),depth,visible,palette,style);
        if(child!=null)child.AttachmentRotationSpeed=attachment.F("rotation_speed");
    }
    internal static void Destruction(JsonElement data,Vector2 pos,Vector2 dir,float? parentZ=null,ReaperKind palette=default,PhoenixFxStyle style=null)
    {
        var d=data.At("on_destruction");
        if(d.ValueKind!=JsonValueKind.Object)return;
        Vector2 point=pos+ReaperSwarmJson.Local(d.V("location",Vector2.Zero),dir), heading=ReaperSwarmJson.Local(d.V("orientation",Vector2.UnitY),dir).Unit();
        Spawn(d.S("name"),()=>point,()=>heading,BurstLife(d.S("name")),scale:new Vector2(style?.Scale??1),z:(parentZ??data.F("z_order"))+d.F("relative_z"),palette:palette,style:style);
    }
    internal static float BurstLife(string name,int depth=0)
    {
        if(depth>12)return .1f;
        var definition = ReaperSwarmData.Definition(name);
        float life=.05f;
        if(definition.Kind == "emitter") life = definition.BurstDuration;
        foreach(var a in definition.Data.At("attachments").Array())life=Math.Max(life,BurstLife(a.S("name"),depth+1));
        return life;
    }
    internal static void Update(float dt)
    {
        if (pending.Count > 0 || Nodes.Count > 0) CaptureView();
        else hasView = false;
        PhoenixBeamPulse.Update(dt);
        if(pending.Count>0) { Nodes.AddRange(pending);pending.Clear(); }
        foreach(var node in Nodes)node.Update(dt);
        Nodes.RemoveAll(static n =>
        {
            if (!n.Finished) return false;
            if (n.IsParticle) liveParticles--;
            return true;
        });
        // Newly emitted particles carry their sub-frame age into the next tick.
        if(pending.Count>0) { Nodes.AddRange(pending);pending.Clear(); }
        particlesThisTick = 0;
    }
    internal static void Clear()
    {
        PhoenixBeamPulse.Clear();
        foreach(var n in Nodes)n.Stop();Nodes.Clear();pending.Clear();OverflowCount=0;
        hasView = false;
        particlesThisTick = liveParticles = 0;
    }
    internal static float Range(JsonElement value,float fallback=0)
    {
        if(value.ValueKind==JsonValueKind.Number)return value.Number(fallback);
        if(value.ValueKind!=JsonValueKind.Array)return fallback;
        return value.At(0).Number(fallback)+Main.rand.NextFloat(-1,1)*value.At(1).Number();
    }
    internal static float Curve(JsonElement times,JsonElement values,float t,float fallback)
    {
        if(times.ValueKind!=JsonValueKind.Array || values.ValueKind!=JsonValueKind.Array)return fallback;
        int count=Math.Min(times.GetArrayLength(),values.GetArrayLength());
        if(count==0)return fallback;
        if(t<=times[0].Number())return values[0].Number(fallback);
        for(int i=1;i<count;i++)if(t<=times[i].Number())
        {
            float f=Math.Clamp((t-times[i-1].Number())/Math.Max(.00001f,times[i].Number()-times[i-1].Number()),0,1);
            return MathHelper.Lerp(values[i-1].Number(),values[i].Number(),f);
        }
        return values[count-1].Number(fallback);
    }
    internal static Vector2 ScaleCurve(JsonElement times,JsonElement values,float t)
    {
        if(times.ValueKind!=JsonValueKind.Array || values.ValueKind!=JsonValueKind.Array)return Vector2.One;
        int count=Math.Min(times.GetArrayLength(),values.GetArrayLength());
        if(count==0)return Vector2.One;
        if(t<=times[0].Number())return values[0].V(Vector2.One);
        for(int i=1;i<count;i++)if(t<=times[i].Number())
            return Vector2.Lerp(values[i-1].V(Vector2.One),values[i].V(Vector2.One),Math.Clamp((t-times[i-1].Number())/Math.Max(.00001f,times[i].Number()-times[i-1].Number()),0,1));
        return values[count-1].V(Vector2.One);
    }

    // Terraria adaptation: orientation belongs to the emitting beam/projectile,
    // independently of the particle's random travel direction. When stretched,
    // nextgen glow uses texture X, whereas Quad's forward is texture Y.
    private static bool DirectionalGlow(JsonElement particle)
        => particle.S("appearance") is "appearance.particle.glow.nextgen" or "appearance.particle.glow.nextgen.small";

    internal static Vector2 ParticleEmissionArea(JsonElement emitter,JsonElement particle)
    {
        Vector2 area=emitter.V("emission_area",Vector2.Zero);
        string name=emitter.S("name");
        // Path glows should form longitudinal clusters rather than transverse
        // bars. Keep impact bursts, explicit local orientations and rings intact.
        if(DirectionalGlow(particle) && emitter.S("particle_orientation")=="standard" && area.X>area.Y
            && (name.Contains(".beam",StringComparison.Ordinal) || name.Contains(".trail",StringComparison.Ordinal)))
            return new Vector2(area.Y*(name=="emitter.player.weapon.17.beam"?.4f:1),area.X);
        return area;
    }

    internal static float ParticleOrientation(JsonElement emitter,JsonElement particle)
    {
        if(emitter.S("name")=="emitter.player.weapon.17.beam")return 0;
        var declared=emitter.At("particle_orientation");
        if(declared.ValueKind==JsonValueKind.Array)
            return ReaperSwarmJson.Local(declared.V(Vector2.UnitY),Vector2.UnitX).ToRotation();
        bool streak=DirectionalGlow(particle);
        if(declared.Text()=="random")
        {
            float angle=Main.rand.NextFloat(MathHelper.TwoPi);
            // Directional glow flecks may cross or follow the flight axis; they
            // never acquire a separate screen-based horizontal/vertical frame.
            return streak ? (angle<MathF.PI ? -MathHelper.PiOver2 : 0) : angle;
        }
        return streak ? -MathHelper.PiOver2 : 0;
    }
}

internal sealed class ReaperSwarmVisualNode
{
    private const int BurstSamplesPerUpdate = 12;
    internal readonly JsonElement Data;
    internal readonly ReaperKind Palette;
    internal readonly PhoenixFxStyle Style;
    internal Func<float> BeamLength;
    internal float AttachmentRotationSpeed;
    private readonly Func<Vector2> sourcePosition,sourceDirection;
    private readonly Func<bool> sourceActive;
    private readonly Vector2 inheritedScale;
    private readonly float life;
    private readonly int depth;
    private readonly string kind;
    private readonly ReaperSwarmDefinition definition;
    private ReaperSwarmDefinition emitterDefinition;
    private ReaperSwarmDefinition Emission => emitterDefinition ??= ReaperSwarmDefinition.Get(Emitter);
    private readonly Vector4 paletteColor;
    private Func<Vector2> followPosition, followDirection;
    private Func<bool> followAlive, followVisible;
    private Func<Vector2> FollowPosition => followPosition ??= () => Position;
    private Func<Vector2> FollowDirection => followDirection ??= () => Direction;
    private Func<bool> FollowAlive => followAlive ??= () => Alive;
    private Func<bool> FollowVisible => followVisible ??= () => Visible?.Invoke() ?? true;
    private float age,emissionClock,rotation,motionSpeed,acceleration,particleRotation;
    private int emitted;
    private bool stopped,soundPlayed;
    private Vector2 localParticlePosition,localParticleVelocity,motion;
    private SlotId soundSlot;
    internal Vector2 Position,Direction,Scale;
    internal float Z;
    internal Func<bool> Visible;
    internal bool IsParticle;
    internal JsonElement Emitter;
    internal bool Attached;
    internal float ParticleAlpha=1;
    internal bool Finished=>stopped && (kind!="trail" || trail.Count==0);
    internal bool Alive=>!stopped;
    internal bool IsShake=>kind=="shake";
    internal float ShakeAge=>age;
    internal float ShakeDuration => definition.Duration;
    internal float ShakeAmplitude => definition.ShakeAmplitude;
    internal float ShakeFrequency => definition.ShakeFrequency;
    private readonly List<TrailPoint> trail;
    private readonly record struct TrailPoint(Vector2 Position,float Time);
    internal ReaperSwarmVisualNode(JsonElement data,Func<Vector2> pos,Func<Vector2> dir,float lifetime,Func<bool> active,Vector2 scale,float z,int depth,ReaperKind palette,PhoenixFxStyle style,ReaperSwarmDefinition compiled=null)
    {
        Style=style;Palette=palette;Data=data;sourcePosition=pos;sourceDirection=dir;life=lifetime;sourceActive=active;inheritedScale=scale;Z=z;this.depth=depth;
        definition=compiled??ReaperSwarmDefinition.Get(data);kind=definition.Kind;
        if(kind=="trail")trail=new();
        if (style?.Tint == null) paletteColor=ReaperSwarmPalette.Tint(definition.Color,Palette);
        Position=pos();Direction=dir().Unit();Scale=scale*definition.Scale;
        emissionClock=definition.FirstEmission;
        motionSpeed=0;
    }
    internal void InitializeParticle(Vector2 pos,Vector2 velocity,float speed,float acc,float orientation,float initialAge)
    {
        IsParticle=true;localParticlePosition=pos;localParticleVelocity=velocity;motionSpeed=speed;acceleration=acc;particleRotation=orientation;
        // Advance from the emission instant to the end of its parent's update.
        age=initialAge;
        localParticlePosition+=localParticleVelocity*(motionSpeed*initialAge+.5f*acceleration*initialAge*initialAge);
        motionSpeed+=acceleration*initialAge;
        // Child emitters initialize inside Add's first Update(0). Publish the
        // particle's local/world frame before those children can emit from it.
        Vector2 basis=Attached?sourceDirection().Unit():Vector2.UnitX;
        Position=(Attached?sourcePosition():Vector2.Zero)+(Attached?ReaperSwarmJson.Local(localParticlePosition,basis):localParticlePosition);
        Direction=basis.Rotate(particleRotation);
    }
    internal void Stop()
    {
        if(stopped)return;stopped=true;
        if(!Main.dedServ && definition.HasDestruction)ReaperSwarmVisuals.Destruction(Data,Position,Direction,Z,Palette,Style);
        if(definition.Looping && SoundEngine.TryGetActiveSound(soundSlot,out var sound))sound.Stop();
    }
    internal void Initialize()
    {
        if (definition.HasAttachments)
            foreach(var a in Data.At("attachments").Array())
                ReaperSwarmVisuals.Attach(a,FollowPosition,FollowDirection,FollowAlive,depth+1,Scale,Z,FollowVisible,Palette,Style);
        if (IsParticle && kind is not ("emitter" or "sound" or "trail" or "shake"))
            UpdateParticleAppearance();
        else
            Update(0);
    }
    internal void Update(float dt)
    {
        dt*=Style?.Playback?.Invoke()??1;
        if(stopped)
        {
            if(kind=="trail") {age+=dt;PruneTrail();}
            return;
        }
        age+=dt;
        bool active=sourceActive?.Invoke()??true;
        // A beam emitter can leave and re-enter the visible section as the hit
        // endpoint changes. Other attached actors stop when their host stops.
        if(!active || age>life || kind=="shake" && age>definition.Duration) {Stop();return;}
        rotation-=MathHelper.ToRadians(definition.RotationSpeed+AttachmentRotationSpeed)*dt;
        if(IsParticle)
        {
            motionSpeed+=acceleration*dt;
            motionSpeed=Math.Clamp(motionSpeed,Emission.MinimumSpeed,Emission.MaximumSpeed);
            localParticlePosition+=localParticleVelocity*motionSpeed*dt;
            Vector2 basis=Attached?sourceDirection().Unit():Vector2.UnitX;
            Position=Attached?sourcePosition()+ReaperSwarmJson.Local(localParticlePosition,basis):localParticlePosition;
            Direction=basis.Rotate(particleRotation+rotation);
            UpdateParticleAppearance();
        }
        else
        {
            Vector2 basis=sourceDirection().Unit();
            Direction=basis.Rotate(rotation);
            motion+=definition.MotionDirection*dt+definition.Acceleration*age*dt;
            Position=sourcePosition()+ReaperSwarmJson.Local(motion,basis);
        }
        if(kind=="emitter" && (Visible?.Invoke()??true))Emit(dt);
        if(kind=="trail")
        {
            if(trail.Count==0 || Vector2.DistanceSquared(trail[^1].Position,Position)>.001f) trail.Add(new(Position,age));
            PruneTrail();
        }
        if(kind=="sound")PlaySound();
        if(definition.Looping && SoundEngine.TryGetActiveSound(soundSlot,out var snd))snd.Position=Position;
    }
    private void UpdateParticleAppearance()
    {
        float t = Math.Clamp(age / life, 0, 1);
        ParticleAlpha = Emission.Alpha(t);
        Scale = inheritedScale * definition.Scale * Emission.ParticleScale(t);
        if (Style?.SkyfireRing == true && Emission.Name == "emitter.player.weapon.24.impact.shockwave")
        {
            float remaining = (1 - t) * (1 - t);
            ParticleAlpha = .16f * remaining;
            Scale = inheritedScale * new Vector2(MathHelper.Lerp(.5f, 4.2f, 1 - remaining));
        }
    }
    private void PruneTrail()
    {
        float oldest=age-(definition.History+definition.Fade)*(Style?.TrailLifetimeScale??1);
        int remove=0;while(remove<trail.Count && trail[remove].Time<oldest)remove++;
        if(remove>0)trail.RemoveRange(0,remove);
    }
    private void PlaySound()
    {
        if(soundPlayed)return;
        JsonElement effect=Data.At("sound_effect");
        if(effect.ValueKind==JsonValueKind.String)effect=ReaperSwarmData.Get(effect.Text());
        if(age<effect.F("max_jitter_milliseconds")/2000f)return;
        soundPlayed=true;
        var samples=effect.At("samples").Array().ToArray();
        if(samples.Length==0)return;
        string name=samples[Main.rand.Next(samples.Length)].Text();
        if(name.Length==0)return;
        var style=new SoundStyle("ArknightsMod/Content/Projectiles/Guard/Reaper/Phoenix/Assets/"+name[..^4])
        {
            Volume=Math.Clamp(effect.F("gain",1)*MathF.Pow(10,Main.rand.NextFloat(-1,1)*effect.F("gain_variation_decibels")/20)*(Style?.SoundVolume??1),0,1),
            PitchVariance=Math.Clamp(MathF.Log2(1+effect.F("pitch_variation_percentage")/100),0,1),
            MaxInstances=Math.Max(1,(int)effect.F("max_simultaneous_playback",4)),
            SoundLimitBehavior=effect.B("start_new_playback_if_maximum_reached")?SoundLimitBehavior.ReplaceOldest:SoundLimitBehavior.IgnoreNew,
            IsLooped=Data.B("looping")
        };
        soundSlot=SoundEngine.PlaySound(style,Position);
    }
    private void Emit(float dt)
    {
        int maximum = definition.MaximumParticles;
        if (maximum >= 0 && emitted >= maximum) return;
        emissionClock-=dt;
        if (emissionClock > 0f) return;
        bool primary = maximum is >= 0 and <= 4;
        Vector2 origin = Style?.ProjectPosition?.Invoke(Position) ?? Position;
        bool inView = ReaperSwarmVisuals.NearView(origin, Scale);
        int samples = 0;
        while (emissionClock <= 0f)
        {
            if (inView && depth < 16 && samples < BurstSamplesPerUpdate && ReaperSwarmVisuals.CanAddParticle(primary))
            {
                EmitParticle(Math.Min(dt, -emissionClock));
                samples++;
            }
            float interval = definition.EmissionInterval;
            if (definition.RandomEmission) interval *= -MathF.Log(Math.Max(.00001f, Main.rand.NextFloat()));
            if (maximum < 0)
            {
                // Advance past missed continuous samples so they cannot accumulate.
                emissionClock = Math.Max(0f, emissionClock + Math.Max(1f / 60f, interval));
                return;
            }
            emissionClock += interval;
            if (++emitted >= maximum) return;
        }
    }
    private void EmitParticle(float subAge)
    {
        var variants=definition.Particles;
        if(variants.Length==0)return;
        var particleDefinition=variants.Length==1?variants[0]:variants[Main.rand.Next(variants.Length)];
        JsonElement particle=particleDefinition.Data;
        if(particle.ValueKind!=JsonValueKind.Object)return;
        float ttl=Math.Max(.001f,ReaperSwarmVisuals.Range(Data.At("particle_time_to_live"),.2f)*(Style?.ParticleLifetimeScale??1));
        if(Style?.SkyfireRing==true && Data.S("name")=="emitter.player.weapon.24.impact.shockwave")ttl=.45f;
        Vector2 offset=Vector2.Zero;
        if(Data.At("emission_area").ValueKind==JsonValueKind.Array)
            offset=new Vector2(Main.rand.NextFloat(-.5f,.5f),Main.rand.NextFloat(-.5f,.5f))*ReaperSwarmVisuals.ParticleEmissionArea(Data,particle);
        else if(Data.At("emission_ellipse").ValueKind==JsonValueKind.Array)
            offset=Vector2.UnitX.Rotate(Main.rand.NextFloat(MathHelper.TwoPi))*MathF.Sqrt(Main.rand.NextFloat())*Data.V("emission_ellipse",Vector2.One)*.5f;
        else if(Data.At("emission_ring_radius").ValueKind!=JsonValueKind.Undefined || Data.At("emission_donut_radius").ValueKind!=JsonValueKind.Undefined)
        {
            JsonElement radius=Data.At("emission_ring_radius");if(radius.ValueKind==JsonValueKind.Undefined)radius=Data.At("emission_donut_radius");
            Vector2 r=radius.ValueKind==JsonValueKind.Array?radius.V(Vector2.One):new Vector2(radius.Number());
            offset=Vector2.UnitX.Rotate(Main.rand.NextFloat(MathHelper.TwoPi))*(r+new Vector2(Main.rand.NextFloat(-.5f,.5f)*Data.F("emission_donut_width")));
        }
        else if(Data.At("emission_line_interval").ValueKind!=JsonValueKind.Undefined)
        {
            float length=Data.F("emission_line_length",1),step=Data.F("emission_line_interval",1),phase=emitted*step;
            float x=phase%Math.Max(.001f,length);
            if(Data.S("emission_line_mode")=="reverse" && (int)(phase/Math.Max(.001f,length))%2==1)x=length-x;
            offset=new Vector2(x-length*.5f,0);
        }
        bool attached=Data.B("attached_particles",true);
        Vector2 v=Data.V("emission_direction",Vector2.UnitY).Unit().Rotate(MathHelper.ToRadians(Main.rand.NextFloat(-.5f,.5f)*Data.F("random_angle_spread")));
        // Compress lateral drift with the spawn area; longitudinal motion is unchanged.
        if(Data.S("name")=="emitter.player.weapon.17.beam")v.X*=.4f;
        float orientation=ReaperSwarmVisuals.ParticleOrientation(Data,particle);
        bool centeredCut=Style?.CenteredCut==true && Data.S("name")=="emitter.player.weapon.40.impact.streak";
        if(centeredCut){offset=Vector2.Zero;v=Vector2.Zero;}
        Vector2 particleScale=attached || Data.B("apply_emitter_scale_to_unattached_particles")?Scale:new Vector2(Style?.Scale??1);
        ReaperSwarmVisualNode n;
        if(attached)
        {
            n=new ReaperSwarmVisualNode(particle,FollowPosition,FollowDirection,ttl,null,particleScale,Z,depth+1,Palette,Style,particleDefinition){Emitter=Data,emitterDefinition=definition,Attached=true};
            // Particle coordinates are expressed with +X right and +Y forward.
            n.InitializeParticle(offset*Scale,v,ReaperSwarmVisuals.Range(Data.At("particle_speed")),ReaperSwarmVisuals.Range(Data.At("particle_acceleration")),orientation,subAge);
        }
        else
        {
            Vector2 origin=Position;
            n=new ReaperSwarmVisualNode(particle,()=>Vector2.Zero,()=>Vector2.UnitX,ttl,null,particleScale,Z,depth+1,Palette,Style,particleDefinition){Emitter=Data,emitterDefinition=definition,Attached=false};
            n.InitializeParticle(origin+ReaperSwarmJson.Local(offset*Scale,Direction),ReaperSwarmJson.Local(v,Direction),ReaperSwarmVisuals.Range(Data.At("particle_speed")),ReaperSwarmVisuals.Range(Data.At("particle_acceleration")),Direction.ToRotation()+orientation,subAge);
        }
        ReaperSwarmVisuals.Add(n);
    }
    internal void Collect(ReaperSwarmRenderer renderer)
    {
        if(stopped && kind!="trail")return;
        if(!(Visible?.Invoke()??true))return;
        if(kind=="trail") {DrawTrail(renderer);return;}
        ReaperSwarmDefinition app=definition.Appearance;
        if(app.Data.ValueKind==JsonValueKind.Undefined)return;
        if(Style?.HideScuuxunPointer==true && definition.AppearanceName=="appearance.projectile.player.67")return;
        Vector4 color=(Style?.Tint?.Invoke(definition.Color)??paletteColor);color.W*=ParticleAlpha*(Style?.Opacity?.Invoke()??1);
        if(Emission.Name is "emitter.player.weapon.69.projectile.trail.flashes" or "emitter.player.weapon.69.projectile.trail.sparks")
            color.W*=.5f;
        float bloom=definition.Bloom;
        Vector2 size=app.Size*Scale*(Style?.SpriteStretch??Vector2.One);
        if(Emission.Name=="emitter.player.weapon.17.beam")
        {
            // This source fleck is round; make its actual footprint axial.
            size*=new Vector2(.6f,6);color.W*=.5f;
        }
        Vector2 position=Position;
        if(BeamLength!=null){size.Y=BeamLength();position+=Direction*size.Y*.5f;}
        position=Style?.ProjectPosition?.Invoke(position)??position;
        Vector2 drawDirection=Style?.ProjectDirection?.Invoke(Direction)??Direction;
        if(Emission.Name is "emitter.player.weapon.17.beam" or "emitter.player.weapon.69.projectile" or "emitter.player.weapon.81.projectile" or "emitter.player.weapon.81.projectile.flutter")
        {
            // Screen-space blur made these tiny flecks look like vertical
            // pillars even after rotating the sprite. Their two soft glow
            // layers now live in the beam's frame too, including afterglow.
            Vector4 halo=color;halo.W*=.18f;
            string haloAsset=Style?.MonochromeHalo==true?app.MonochromeAsset:app.Asset;
            renderer.Quad(haloAsset,position,size+new Vector2(14,18),drawDirection,halo,0,true,Z);
            halo=color;halo.W*=.8f;
            renderer.Quad(haloAsset,position,size+new Vector2(6,10),drawDirection,halo,0,true,Z);
            bloom=0;
        }
        string asset=Style?.MonochromeTexture==true?app.MonochromeAsset:app.Asset;
        renderer.Quad(asset,position,size,drawDirection,color,bloom,app.Additive,Z,definition.Lensflare,app.Invisible);
    }
    private void DrawTrail(ReaperSwarmRenderer renderer)
    {
        var texture=definition.Texture;
        float history=definition.History*(Style?.TrailLifetimeScale??1),fade=definition.Fade*(Style?.TrailLifetimeScale??1),width=definition.Width*Scale.X;
        Vector4 tint=(Style?.Tint?.Invoke(definition.Color)??paletteColor);
        for(int i=1;i<trail.Count;i++)
        {
            var a=trail[i-1];var b=trail[i];
            float u0=Math.Clamp((age-a.Time)/Math.Max(.001f,history),0,1),u1=Math.Clamp((age-b.Time)/Math.Max(.001f,history),0,1);
            float opacity=fade>0?Math.Clamp((history+fade-age+a.Time)/fade,0,1):1;
            Vector4 color=tint;color.W*=opacity*(Style?.Opacity?.Invoke()??1);
            string asset=Style?.MonochromeTexture==true?texture.MonochromeAsset:texture.Asset;
            renderer.Ribbon(asset,Style?.ProjectPosition?.Invoke(a.Position)??a.Position,Style?.ProjectPosition?.Invoke(b.Position)??b.Position,width,color,definition.Bloom,definition.Additive,Z,u0,u1);
        }
    }
}

public sealed class ReaperSwarmVisualSystem : ModSystem
{
    public override void Load()=>ReaperSwarmData.Ensure();
    public override void PostUpdateEverything() {if(!Main.dedServ && !Main.gamePaused)ReaperSwarmVisuals.Update(1f/60);}
    public override void OnWorldUnload()=>ReaperSwarmVisuals.Clear();
    public override void Unload() {ReaperSwarmVisuals.Clear();ReaperSwarmData.Unload();}
}

public sealed class ReaperSwarmCamera : ModPlayer
{
    public override void ModifyScreenPosition()
    {
        if(Main.dedServ)return;
        Vector2 shake=Vector2.Zero;
        foreach(var node in ReaperSwarmVisuals.Nodes)
        {
            if(!node.IsShake || !node.Alive)continue;
            float distance=Vector2.Distance(node.Position,Player.Center);
            if(distance>1600)continue;
            float t=node.ShakeAge;
            float envelope=Math.Clamp(1-t/Math.Max(.001f,node.ShakeDuration),0,1)*(1-distance/1600);
            float amplitude=node.ShakeAmplitude*envelope;
            float phase=t*node.ShakeFrequency*MathHelper.TwoPi;
            shake+=new Vector2(MathF.Sin(phase),MathF.Cos(phase*1.37f))*amplitude;
        }
        Main.screenPosition+=shake;
    }
}
