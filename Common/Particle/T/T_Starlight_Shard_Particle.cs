using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Guard.Mousse;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Starlight_Shard_Particle : global::ArknightsMod.Common.Particle.Particle
{
    public override string TexturePath => MousseMaterials.Root + "MousseLightSplinter";
    private readonly Color tint;
    private readonly float spin;
    public T_Starlight_Shard_Particle(Vector2 position, Vector2 velocity, Color tint, int life)
    {
        Position = position; Velocity = velocity; this.tint = tint; Lifetime = life;
        Rotation = velocity.ToRotation();
        spin = Main.rand.NextFloat(-.085f, .085f);
    }
    public override void Update() { Velocity *= .94f; Rotation += spin; }
    public override void Draw()
    {
        float fade = MathF.Pow(1f - LifetimeRatio, 1.5f);
        MousseVisuals.DrawPixels(MousseMaterials.Get("MousseLightSplinter"), Position,
            new Vector2(14f, 5f) * (1f - LifetimeRatio * .35f), MousseVisuals.Light(tint, fade),
            Rotation, new Vector2(.5f));
    }
}
