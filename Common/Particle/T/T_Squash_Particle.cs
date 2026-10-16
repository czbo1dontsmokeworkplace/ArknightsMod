using ArknightsMod.Content.Projectiles.Guard.Mousse;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;
using ArknightsMod.Content.Dusts;

namespace ArknightsMod.Common.Particle.T;

// SquashDust motion and velocity-based shape reference: Azafure, LLC.
// https://github.com/CalamityTeam/CalamityModPublic
public class T_Squash_Particle : ModDust
{
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    public override void OnSpawn(Dust dust) => dust.scale *= Main.rand.NextFloat(.8f, 1f);
    public override bool Update(Dust dust)
    {
        dust.rotation = dust.velocity.ToRotation() + MathHelper.PiOver2;
        dust.velocity *= .96f;
        float fadeSpeed = dust.fadeIn + 1f;
        if (dust.noGravity) dust.scale -= .045f * fadeSpeed;
        else
        {
            dust.scale -= .03f * fadeSpeed;
            dust.velocity.Y += Main.rand.NextFloat(.1f, .35f) * fadeSpeed;
        }
        if (dust.scale <= 0f) { dust.active = false; return false; }
        if (!dust.noLightEmittence)
            Lighting.AddLight(dust.position, dust.color.ToVector3() * MathHelper.Clamp(dust.scale * .8f, 0f, 1f));
        dust.position += dust.velocity;
        return false;
    }
    public override bool PreDraw(Dust dust)
    {
        float speed = dust.velocity.Length();
        Vector2 squash = new(Utils.Remap(speed, 2f, 7f, 1f, .5f),
            Utils.Remap(speed, 2f, 7f, 1f, 2.5f));
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
