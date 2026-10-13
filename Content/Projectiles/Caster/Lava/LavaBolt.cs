using System;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Projectiles.Caster.SpreadArts;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Lava;

public sealed class LavaBolt : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 18;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 135;
        Projectile.extraUpdates = 2;
    }
    public override void OnSpawn(IEntitySource source) => Projectile.velocity *= .5f;
    public override void AI()
    {
        if (Projectile.numUpdates == 0) Projectile.velocity *= 1.07f;
        Projectile.localAI[1] += Projectile.velocity.Length();
        if (Projectile.localAI[1] > 1200f) { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, .45f, .12f, .03f);
        if (Projectile.numUpdates == 0)
            OperatorFlightVfx.Spark(Projectile.Center, -Projectile.velocity * .13f,
                new Color(255, 153, 72), 15f, 11, 1.5f);
    }
    public override bool OnTileCollide(Vector2 oldVelocity) { Projectile.Kill(); return false; }
    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<LavaBurst>(), Projectile.damage, Projectile.knockBack,
                Projectile.owner);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<SpreadAfterfield>(), Math.Max(1, (int)(Projectile.damage * .32f)),
                Projectile.knockBack, Projectile.owner, 0f);
        }
        if (!Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = .36f }, Projectile.Center);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        OperatorFlightVfx.Trail(Projectile, new Color(157, 55, 26), new Color(255, 180, 79), 5f, 65f);
        OperatorFlightVfx.Sprite("StarCore", Projectile.Center, new Vector2(23f),
            OperatorFlightVfx.Add(new Color(255, 169, 83), .9f), Projectile.rotation);
        return false;
    }
}
