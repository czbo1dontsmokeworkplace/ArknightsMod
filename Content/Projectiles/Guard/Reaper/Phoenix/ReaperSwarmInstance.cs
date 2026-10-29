using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;

internal sealed class ReaperSwarmInstance
{
    private readonly Projectile projectile;
    private readonly ReaperKind palette;
    private readonly ReaperSwarmVisualNode root;
    private ReaperSwarmVisualNode active;
    private bool activated;
    internal ReaperSwarmInstance(ReaperWisp wisp)
    {
        projectile=wisp.Projectile;palette=wisp.Kind;
        root=ReaperSwarmVisuals.Spawn("actor.arknights.reaper.root",()=>projectile.Center,
            ()=>projectile.velocity.Unit(),active:()=>projectile.active,palette:palette);
    }
    internal void Update(int age)
    {
        var source=ReaperSwarmData.Get("projectile.player.81.rank6");
        if(!activated && age/60f>=source.F("activation_delay"))
        {
            activated=true;
            active=ReaperSwarmVisuals.Spawn(source.S("actor_on_activation"),()=>projectile.Center,
                ()=>projectile.velocity.Unit(),active:()=>projectile.active,palette:palette);
        }
    }
    internal void Stop()
    {
        if(root!=null){root.Position=projectile.Center;root.Direction=projectile.velocity.Unit();root.Stop();}
        active?.Stop();
    }
}

internal static class ReaperSwarmPalette
{
    internal static Vector4 Tint(Vector4 source,ReaperKind kind)
    {
        float peak=Math.Max(source.X,Math.Max(source.Y,source.Z));
        if(peak<=0)return source;
        float saturation=(peak-Math.Min(source.X,Math.Min(source.Y,source.Z)))/peak;
        Vector3 color=Vector3.Lerp(ReaperRules.Color(kind).ToVector3(),ReaperRules.Edge(kind).ToVector3(),1-saturation);
        color*=peak/Math.Max(.001f,Math.Max(color.X,Math.Max(color.Y,color.Z)));
        // Preserve each source layer's brightness, HDR peaks and alpha curve.
        return new Vector4(color,source.W);
    }
}
