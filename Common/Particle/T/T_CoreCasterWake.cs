using System;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using Microsoft.Xna.Framework.Graphics;
using Terraria;

namespace ArknightsMod.Common.Particle.T;

// Like PlasmaRifleShot / SylvBolt retirement: only the already travelled path remains.
// No new hitbox, flying head, child shot or extra damage is created by this local afterglow.
public sealed class T_CoreCasterWake : Particle
{
    public override string TexturePath => CoreCasterArt.Assets + "EnergyLance";
    public override BlendState DrawBlendState => BlendState.AlphaBlend;
    public override SamplerState DrawSamplerState => SamplerState.LinearClamp;
    private readonly Projectile snapshot;
    private readonly CoreCasterKind kind;
    private readonly int mode;
    public T_CoreCasterWake(Projectile source, CoreCasterKind kind, int mode)
    {
        this.kind = kind; this.mode = mode; Position = source.Center; Lifetime = 12;
        snapshot = new Projectile { position = source.position, width = source.width, height = source.height,
            velocity = source.velocity, identity = source.identity, oldPos = (Microsoft.Xna.Framework.Vector2[])source.oldPos.Clone() };
    }
    public override void Draw()
    {
        if (CoreCasterVisuals.Ready()) CoreCasterTrail.Draw(snapshot, kind, mode, MathF.Pow(1 - LifetimeRatio, 1.5f));
    }
}
