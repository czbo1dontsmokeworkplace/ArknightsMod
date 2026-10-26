using ArknightsMod.Content.Projectiles.Guard.Reaper;
using Microsoft.Xna.Framework;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Reaper_WavePulse : Particle
{
    public override string TexturePath => ReaperWaveNovaVisuals.BloomPath;
    private readonly Color tint;
    private readonly float startSize,endSize;
    public T_Reaper_WavePulse(Vector2 at,Vector2 velocity,Color color,float start,float end,int life)
    {Position=at;Velocity=velocity;tint=color;startSize=start;endSize=end;Lifetime=life;}
    public override void Draw()
    {
        float t=LifetimeRatio;
        ReaperWaveNovaVisuals.Bloom(Position,new Vector2(MathHelper.Lerp(startSize,endSize,t)),tint*(1-t)*.5f);
    }
}
