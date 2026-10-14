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

    private SpreadArt Art => Kind switch { 0 => SpreadArt.Ember, 1 => SpreadArt.Crystal, 2 => SpreadArt.Rupture, _ => SpreadArt.Meteor };
    public override void AI()
    {
        Projectile.velocity = Vector2.Zero;
        if (!SpreadVfxBudget.Visible(Projectile.Center, 90f)) return;
        Color color = SpreadArtsVfx.Body(Art);
        float fade = MathHelper.Clamp(Projectile.timeLeft / 15f, 0, 1);
        // Stop births before the field ends. Every child expires by the end of its parent.
        if (Age % 3 == 0 && Projectile.timeLeft > 10)
        {
            int life = Math.Min(21, Projectile.timeLeft);
            for (int i = 0; i < (Kind is 0 or 3 ? 3 : 2); i++)
            {
                float angle = Age * .24f + i * 2.4f;
                Vector2 ray = angle.ToRotationVector2();
                Vector2 p = Projectile.Center + ray * 24f;
                if (Kind is 0 or 3)
                {
                    SpreadVfxDetail.Fire(p, new Vector2(ray.X * .4f, -1.6f), Kind == 0 ? 24f : 33f, life, 19f);
                    if (i == 0 && Age % 6 == 0) SpreadVfxDetail.Fire(p, -Vector2.UnitY * .6f, 28f, life, 15f, true);
                }
                else SpreadVfxDetail.Fragment(p, Kind == 1 ? new Vector2(ray.X * .4f, -.7f) : -ray * 1.1f,
                    i == 0 ? SpreadArtsVfx.Edge(Art) : color, Kind == 1 ? 13f : 9f, life, 19f);
            }
        }
        if (Age % 10 == 0 && Projectile.timeLeft > 10)
        {
            Vector2 p = Projectile.Center + (Age * .7f).ToRotationVector2() * 27f;
            CasterSparkles.Spawn(p, Vector2.Zero, SpreadArtsVfx.Edge(Art), .4f, 8);
            Dust dust = Dust.NewDustPerfect(p, Kind is 0 or 3 ? DustID.Torch : DustID.TintableDustLighted,
                -Vector2.UnitY * .7f, 130, color, .7f * fade);
            dust.noGravity = true;
        }
        Lighting.AddLight(Projectile.Center, color.ToVector3() * (.3f * fade));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (!SpreadVfxBudget.Visible(Projectile.Center, 90f)) return false;
        float fade = MathHelper.Clamp((Age + 1f) / 4f, 0, 1) * MathHelper.Clamp(Projectile.timeLeft / 15f, 0, 1);
        Color body = SpreadArtsVfx.Body(Art), edge = SpreadArtsVfx.Edge(Art);
        using (SpreadArtsMaterial.Begin())
        {
            SpreadArtsMaterial.Sprite("ImpactPetals", Projectile.Center, new Vector2(94f, 78f),
                new Color(44, 24, 40) * (.5f * fade));
            if (Kind is 0 or 3)
            {
                // Seven asynchronous tongues are rooted inside the 100x100 field.
                // Lower red skirt, taller orange body and small warm core read as fire.
                int tongues = Kind == 0 ? 5 : 7;
                for (int i = 0; i < tongues; i++)
                {
                    float phase = Age * .15f + i * 2.4f;
                    float x = (i / (float)(tongues - 1) - .5f) * 58f;
                    float height = (Kind == 0 ? 34f : 43f) + MathF.Sin(phase) * 8f;
                    Vector2 root = Projectile.Center + new Vector2(x, 18f - height * .28f);
                    SpreadArtsMaterial.Sprite("ForwardFlame", root, new Vector2(22f, height),
                        OperatorFlightVfx.Add(i % 3 == 0 ? edge : body, fade * .62f), MathF.Sin(phase * .7f) * .16f);
                }
                SpreadArtsMaterial.Sprite("OrbitSmear", Projectile.Center + Vector2.UnitY * 17f,
                    new Vector2(82f, 24f), OperatorFlightVfx.Add(body, .42f * fade));
                SpreadArtsMaterial.Sprite("BlastBloom", Projectile.Center + Vector2.UnitY * 10f,
                    new Vector2(65f, 25f), OperatorFlightVfx.Add(body, .34f * fade));
            }
            else SpreadArtsMaterial.Sprite("OrbitSmear", Projectile.Center, new Vector2(86f, 45f),
                OperatorFlightVfx.Add(body, fade * .22f), Age * .012f);
        }
        if (Kind == 1)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathHelper.TwoPi / 6f;
                Vector2 p = Projectile.Center + a.ToRotationVector2() * 28f + Vector2.UnitY * MathF.Sin(Age * .12f + i) * 3f;
                SpreadVfxDetail.Facet(p, 8f, 22f, -MathHelper.PiOver2 + MathF.Sin(a) * .25f, i % 2 == 0 ? edge : body, fade);
            }
        }
        if (Kind == 2)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathHelper.TwoPi / 5f;
                Vector2 ray = a.ToRotationVector2();
                Vector2 bend = Projectile.Center + ray.RotatedBy(.2f) * 23f;
                SpreadVfxDetail.Segment(Projectile.Center + ray * 7f, bend, OperatorFlightVfx.Add(body, fade * .7f), 2f);
                SpreadVfxDetail.Segment(bend, Projectile.Center + ray * 44f, OperatorFlightVfx.Add(edge, fade * .6f), 1f);
                SpreadVfxDetail.Arc(Projectile.Center, 25f + (Age % 10) * 1.8f, a, .7f,
                    OperatorFlightVfx.Add(body, fade * (1f - Age % 10 / 10f)), 1.5f);
            }
        }
        return false;
    }
}
