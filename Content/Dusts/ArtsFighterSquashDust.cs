using System;
using ArknightsMod.Content.Projectiles.Guard.Mousse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Dusts;

// SquashDust behavior reference, credit Azafure, LLC.
// https://github.com/CalamityTeam/CalamityModPublic
// Registered as Terraria Dust, never as an ArknightsMod Particle.
public class ArtsFighterSquashDust : ModDust
{
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    public override void OnSpawn(Dust dust) => dust.scale *= Main.rand.NextFloat(.8f, 1f);
    public override bool Update(Dust dust)
    {
        dust.rotation += dust.customData is float spin ? spin : .045f;
        dust.velocity *= .96f;
        dust.scale -= .045f * (dust.fadeIn + 1f);
        if (dust.scale <= 0f) { dust.active = false; return false; }
        Lighting.AddLight(dust.position, dust.color.ToVector3() * MathHelper.Clamp(dust.scale * .8f, 0f, 1f));
        dust.position += dust.velocity;
        return false;
    }
    public override bool PreDraw(Dust dust)
    {
        float speed = dust.velocity.Length();
        Vector2 squash = dust.customData is float ? new Vector2(.42f, 1.5f)
            : new Vector2(Utils.Remap(speed, 2f, 7f, 1f, .5f), Utils.Remap(speed, 2f, 7f, 1f, 2.5f));
        float opacity = Utils.GetLerpValue(255f, 0f, dust.alpha, true);
        Texture2D glow = MousseMaterials.Get("GlowOrb");
        MousseVisuals.DrawPixels(glow, dust.position, squash * dust.scale * 20f,
            MousseVisuals.Light(dust.color, opacity), dust.rotation, new Vector2(.5f));
        MousseVisuals.DrawPixels(glow, dust.position, squash * dust.scale * 8f,
            MousseVisuals.Light(dust.color, opacity * .85f), dust.rotation, new Vector2(.5f));
        MousseVisuals.DrawPixels(MousseMaterials.Get("SolidOrb"), dust.position, squash * dust.scale * 4.8f,
            MousseVisuals.Light(Color.Lerp(dust.color, Color.White, .3f), opacity), dust.rotation, new Vector2(.5f));
        return false;
    }
}
