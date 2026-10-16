using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using ArknightsMod.Content.Projectiles.Caster.Pudding;

namespace ArknightsMod.Common.Particle.T;

public sealed class T_Electric_Pulse_Particle : global::ArknightsMod.Common.Particle.Particle
{
    private readonly float intensity;
    private readonly bool impact;
    private readonly Texture2D flash;
    public override string TexturePath => "ArknightsMod/Content/Textures/PassengerImpactRing";
    public override BlendState DrawBlendState => BlendState.Additive;

    internal T_Electric_Pulse_Particle(Vector2 position, float intensity, bool impact)
    {
        Position = position;
        this.intensity = MathHelper.Clamp(intensity, .2f, 1.5f);
        this.impact = impact;
        Lifetime = impact ? 17 : 9;
        flash = ModContent.Request<Texture2D>("ArknightsMod/Content/Textures/flare_01").Value;
    }

    public override void Draw()
    {
        float progress = LifetimeRatio;
        float wave = impact ? MathHelper.SmoothStep(0f, 1f, progress)
            : 1f - MathHelper.SmoothStep(0f, 1f, progress);
        float diameter = (impact ? 12f + wave * 63f : 12f + wave * 13f) * intensity;
        float fade = MathF.Pow(1f - progress, impact ? 1.3f : 1.8f);
        Main.spriteBatch.Draw(Texture, Position - Main.screenPosition, null,
            PuddingVisuals.Cyan * (fade * .66f), progress * .45f,
            Texture.Size() * .5f, new Vector2(diameter, diameter * .72f) / Texture.Size(),
            SpriteEffects.None, 0f);
        if (impact)
            Main.spriteBatch.Draw(flash, Position - Main.screenPosition, null,
                PuddingVisuals.White * (MathF.Pow(1f - progress, 3f) * .75f), 0f,
                flash.Size() * .5f, new Vector2(36f, 20f) * intensity / flash.Size(),
                SpriteEffects.None, 0f);
    }
}
