using System;
using ArknightsMod.Common.Particle;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.SpreadArts;

// A one-second damage field. ai[0] selects the caster and its area.
public sealed class SpreadAfterfield : ModProjectile
{
    private const int Lifetime = 60;
    private int Kind => Math.Clamp((int)Projectile.ai[0], 0, 3);
    private int Age => Lifetime - Projectile.timeLeft;
    private int Diameter => Kind switch { 0 or 1 => 300, 2 => 400, _ => 500 };
    private float Radius => Diameter * .5f;
    private const string Artwork = "ArknightsMod/Content/Textures/SpreadArts/CasterAfterglow";
    public override string Texture => Artwork;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 300;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 10;
    }

    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
    {
        Vector2 center = Projectile.Center;
        Projectile.width = Projectile.height = Diameter;
        Projectile.Center = center;
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
        if (Kind is 0 or 3) { FireArtsVfx.FieldTick(Projectile.Center, Radius, Age, Projectile.timeLeft, Art); return; }
        if (!SpreadVfxBudget.Visible(Projectile.Center, 90f)) return;
        Color color = SpreadArtsVfx.Body(Art);
        float fade = MathHelper.Clamp(Projectile.timeLeft / 15f, 0, 1);
        // The outward-moving Dust supplies the impact's force. The Arknights
        // particles are separate, longer-lived shapes travelling on the same rays.
        if (Projectile.timeLeft > 8)
        {
            int dustCount = (Kind switch { 0 => 9, 1 => 10, 2 => 12, _ => 15 }) + (Age == 0 ? 14 : 0);
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 ray = Main.rand.NextVector2Unit();
                Vector2 origin = Projectile.Center + ray * Main.rand.NextFloat(5f, 20f);
                int dustType = Kind switch {
                    0 or 1 or 3 => SpreadArtsVfx.ElementDust(Art, i + Age),
                    _ => i % 3 == 0 ? DustID.GemSapphire : DustID.Electric
                };
                float speed = Main.rand.NextFloat(Radius * .055f, Radius * .11f);
                Vector2 dustVelocity = ray * speed + Main.rand.NextVector2Circular(1.2f, 1.2f);
                if (Kind == 3 && Age >= 10)
                {
                    origin = Projectile.Center + new Vector2(Main.rand.NextFloat(-Radius * .6f, Radius * .6f), Main.rand.NextFloat(-8f, 14f));
                    dustVelocity = new Vector2(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-5f, -2f));
                }
                Dust dust = Dust.NewDustPerfect(origin, dustType,
                    dustVelocity,
                    80, Kind == 2 ? color : SpreadArtsVfx.DustTint(Art, i + Age),
                    Main.rand.NextFloat(1.05f, 1.65f) * fade);
                dust.noGravity = true;
            }
        }
        if (Age % 2 == 0 && Projectile.timeLeft > 18)
        {
            int count = Kind switch { 0 => 3, 1 => 4, 2 => 4, _ => 2 };
            int life = Math.Min(24, Projectile.timeLeft);
            for (int i = 0; i < count; i++)
            {
                float angle = Age * .21f + i * MathHelper.TwoPi / count + Main.rand.NextFloat(-.18f, .18f);
                Vector2 ray = angle.ToRotationVector2();
                Vector2 point = Projectile.Center + ray * Main.rand.NextFloat(8f, 24f);
                float speed = Main.rand.NextFloat(Radius * .04f, Radius * .07f);
                if (Kind == 0)
                    SpreadVfxDetail.Fire(point, ray * speed, 24f, life, Radius * .86f);
                else if (Kind == 3)
                    SpreadVfxDetail.Fragment(point, new Vector2(ray.X * 3f, -Main.rand.NextFloat(2f, 5f)),
                        new Color(255, 150, 57), 6f, life, 80f, true);
                else
                    SpreadVfxDetail.Fragment(point, ray * speed,
                        i % 2 == 0 ? SpreadArtsVfx.Edge(Art) : color,
                        Kind == 1 ? 15f : 12f, life, Radius * .86f);
            }
        }
        if (Kind == 1 && Age % 3 == 0 && Projectile.timeLeft > 12)
        {
            // Vanilla PrettySparkles trace a few broken crystal-light rays.
            for (int i = 0; i < 3; i++)
            {
                Vector2 ray = (Age * .17f + i * MathHelper.TwoPi / 3f).ToRotationVector2();
                Vector2 point = Projectile.Center + ray * Main.rand.NextFloat(24f, Radius * .72f);
                CasterSparkles.Spawn(point, ray * Main.rand.NextFloat(2.5f, 5f),
                    i == 0 ? Color.White : SpreadArtsVfx.Edge(Art), .5f, 11, ray.ToRotation());
            }
        }
        Lighting.AddLight(Projectile.Center, color.ToVector3() * (.3f * fade));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Kind is 0 or 3) { FireArtsVfx.FieldDraw(Projectile.Center, Radius, Age, Projectile.timeLeft, Art); return false; }
        if (!SpreadVfxBudget.Visible(Projectile.Center, 90f)) return false;
        float fade = MathHelper.Clamp((Age + 1f) / 4f, 0, 1) * MathHelper.Clamp(Projectile.timeLeft / 15f, 0, 1);
        Color body = SpreadArtsVfx.Body(Art), edge = SpreadArtsVfx.Edge(Art);
        using (SpreadArtsMaterial.Begin())
        {
            if (Kind != 3)
                SpreadArtsMaterial.Sprite("ImpactPetals", Projectile.Center, new Vector2(Radius * .8f, Radius * .62f),
                    new Color(44, 24, 40) * (.5f * fade));
            if (Kind == 3)
            {
                // Skyfire's residual heat is a low soft glow, never repeated
                // claw-shaped flame masks. Torch/lava Dust provides the rising fire.
                SpreadArtsMaterial.Sprite("ChargeBloom", Projectile.Center + Vector2.UnitY * 10f,
                    new Vector2(Radius * 1.5f, 48f), OperatorFlightVfx.Add(body, .48f * fade));
                SpreadArtsMaterial.Sprite("ChargeBloom", Projectile.Center,
                    new Vector2(Radius * .8f, 25f), OperatorFlightVfx.Add(edge, .24f * fade));
            }
            else if (Kind == 0)
            {
                // Seven asynchronous tongues are rooted inside the 100x100 field.
                // Lower red skirt, taller orange body and small warm core read as fire.
                int tongues = Kind == 0 ? 7 : 11;
                for (int i = 0; i < tongues; i++)
                {
                    float phase = Age * .15f + i * 2.4f;
                    float x = (i / (float)(tongues - 1) - .5f) * Radius * 1.45f;
                    float height = (Kind == 0 ? 48f : 70f) + MathF.Sin(phase) * 12f;
                    Vector2 root = Projectile.Center + new Vector2(x, 18f - height * .28f);
                    SpreadArtsMaterial.Sprite("ForwardFlame", root, new Vector2(Kind == 0 ? 27f : 37f, height),
                        OperatorFlightVfx.Add(i % 3 == 0 ? edge : body, fade * .62f), MathF.Sin(phase * .7f) * .16f);
                }
                SpreadArtsMaterial.Sprite("OrbitSmear", Projectile.Center + Vector2.UnitY * 17f,
                    new Vector2(Radius * 1.55f, 31f), OperatorFlightVfx.Add(body, .42f * fade));
                SpreadArtsMaterial.Sprite("BlastBloom", Projectile.Center + Vector2.UnitY * 10f,
                    new Vector2(Radius * 1.2f, 29f), OperatorFlightVfx.Add(body, .34f * fade));
            }
            else SpreadArtsMaterial.Sprite("OrbitSmear", Projectile.Center, new Vector2(Radius * 1.35f, Radius * .45f),
                OperatorFlightVfx.Add(body, fade * .22f), Age * .012f);
        }
        if (Kind == 1)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * MathHelper.TwoPi / 6f;
                Vector2 p = Projectile.Center + a.ToRotationVector2() * Radius * .55f + Vector2.UnitY * MathF.Sin(Age * .12f + i) * 5f;
                SpreadVfxDetail.Facet(p, 8f, 22f, -MathHelper.PiOver2 + MathF.Sin(a) * .25f, i % 2 == 0 ? edge : body, fade);
            }
        }
        if (Kind == 2)
        {
            for (int i = 0; i < 5; i++)
            {
                float a = i * MathHelper.TwoPi / 5f;
                Vector2 ray = a.ToRotationVector2();
                Vector2 bend = Projectile.Center + ray.RotatedBy(.2f) * Radius * .37f;
                SpreadVfxDetail.Segment(Projectile.Center + ray * 12f, bend, OperatorFlightVfx.Add(body, fade * .7f), 2f);
                SpreadVfxDetail.Segment(bend, Projectile.Center + ray * Radius * .8f, OperatorFlightVfx.Add(edge, fade * .6f), 1f);
                SpreadVfxDetail.Arc(Projectile.Center, Radius * (.25f + (Age % 10) * .045f), a, .7f,
                    OperatorFlightVfx.Add(body, fade * (1f - Age % 10 / 10f)), 1.5f);
            }
        }
        return false;
    }
}
