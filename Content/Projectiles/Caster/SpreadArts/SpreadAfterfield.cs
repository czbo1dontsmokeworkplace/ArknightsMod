using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.SpreadArts;

// A one-second, 100x100 damage field. ai[0] selects the caster's palette.
public sealed class SpreadAfterfield : ModProjectile
{
    private const int Lifetime = 60;
    private int Kind => Math.Clamp((int)Projectile.ai[0], 0, 3);
    private int Age => Lifetime - Projectile.timeLeft;
    private const string Artwork = "ArknightsMod/Content/Textures/SpreadArts/CasterAfterglow";
    public override string Texture => Artwork;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 100;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind is 0 or 3)
            target.AddBuff(BuffID.OnFire3, 120);
    }

    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        if (Main.dedServ) return;
        Color color = Kind switch
        {
            0 => new Color(255, 127, 57),
            1 => new Color(173, 115, 227),
            2 => new Color(105, 181, 236),
            _ => new Color(255, 169, 76)
        };
        float fade = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
        if (Age % 8 == 0)
            OperatorFlightVfx.Ring(Projectile.Center, color * fade, 61f, 12);
        // Textured custom sparks supply the body; vanilla Dust supplies a sparse, independent spray.
        if (ParticleManager.activeParticles.Count < 1500)
            for (int i = 0; i < 5; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 point = Projectile.Center + direction * Main.rand.NextFloat(4f, 45f);
                new DefaultParticle(point, direction * Main.rand.NextFloat(1.2f, 3.8f),
                    Main.rand.Next(12, 22), Main.rand.NextFloat(.34f, .62f) * fade,
                    i == 0 ? Color.White : color, true)
                { Deformation = new Vector2(.45f, 1.7f) }.Spawn();
            }
        if (Age % 2 == 0)
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(45f, 45f),
                Kind is 0 or 3 ? DustID.Torch : DustID.MagicMirror,
                Main.rand.NextVector2Circular(2f, 2f) - Vector2.UnitY, 100, color, .85f * fade);
            dust.noGravity = true;
        }
        Lighting.AddLight(Projectile.Center, color.ToVector3() * (.18f * fade));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Color color = Kind switch
        {
            0 => new Color(255, 127, 57), 1 => new Color(173, 115, 227),
            2 => new Color(105, 181, 236), _ => new Color(255, 169, 76)
        };
        float appear = MathHelper.Clamp(Age / 6f, 0f, 1f);
        float vanish = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
        float pulse = .78f + .22f * MathF.Sin(Age * .29f);
        float opacity = appear * vanish * pulse;
        Texture2D texture = SpreadArtsMaterials.Get("CasterAfterglow");
        Vector2 center = Projectile.Center - Main.screenPosition;
        Vector2 origin = texture.Size() * .5f;
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        try
        {
            Main.spriteBatch.Draw(texture, center, null, color * (.65f * opacity), Age * .015f,
                origin, 148f / texture.Width, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(texture, center, null, Color.Lerp(color, Color.White, .42f) * (.38f * opacity),
                -Age * .021f, origin, 110f / texture.Width, SpriteEffects.None, 0f);
        }
        finally
        {
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }
        return false;
    }
}
