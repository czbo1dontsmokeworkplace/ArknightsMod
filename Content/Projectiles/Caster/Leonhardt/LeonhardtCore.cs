using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.DataStructures;

namespace ArknightsMod.Content.Projectiles.Caster.Leonhardt;

/// <summary>压缩的源石碎片；无论命中敌人、墙壁还是达到射程，只爆破一次。</summary>
public sealed class LeonhardtCore : ModProjectile
{
    private float Charge => MathHelper.Clamp(Projectile.ai[0], 0f, 1f);
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 28;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 210;
        Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override void OnSpawn(IEntitySource source) => Projectile.velocity *= .5f;

    public override void AI()
    {
        if (Projectile.numUpdates == 0) Projectile.velocity *= 1.07f;
        Projectile.localAI[1] += Projectile.velocity.Length();
        if (Projectile.localAI[1] > 2200f) { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Main.dedServ) return;
        Lighting.AddLight(Projectile.Center, new Vector3(.10f, .23f, .40f) * (.5f + Charge));
        if (Projectile.numUpdates == 0)
            global::ArknightsMod.Common.VisualEffects.SpreadArtsVfx.Wake(Projectile.Center, Projectile.velocity,
                global::ArknightsMod.Common.VisualEffects.SpreadArt.Rupture, (int)Projectile.localAI[0]++);
        if (Main.rand.NextBool(3))
        {
            Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 7f),
                DustID.GemSapphire, -Projectile.velocity * Main.rand.NextFloat(.07f, .18f),
                110, default, Main.rand.NextFloat(.55f, .9f));
            dust.noGravity = true;
        }
    }

    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        Projectile.Kill();
        return false;
    }

    public override void OnKill(int timeLeft)
    {
        if (Projectile.owner == Main.myPlayer)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<LeonhardtDetonation>(),
                Math.Max(1, (int)(Projectile.damage * MathHelper.Lerp(1.2f, 1.65f, Charge))),
                Projectile.knockBack, Projectile.owner, Charge);

        if (!Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = .48f, Pitch = .42f }, Projectile.Center);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        LeonhardtVisuals.DrawCore(Projectile, Charge);
        return false;
    }
}
