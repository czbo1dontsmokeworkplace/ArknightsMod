using System;
using ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

public sealed class OrbitalPurpleLaser : ModProjectile
{
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => "Terraria/Images/MagicPixel";
    private int Age => (int)Projectile.ai[0];
    private float Reach => Projectile.ai[1];
    private float Strength => Age < 1 ? 0 : MathHelper.Clamp(1 - (Age - 1) / (float)MaterialistArsenalRules.OrbitalFade, 0, 1);
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 150000;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4; Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic; Projectile.penetrate = -1;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.timeLeft = MaterialistArsenalRules.OrbitalFade + 1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Projectile.ai[0]++;
        // Owner chooses the terrain endpoint; the synchronized length is shared by all clients.
        if (Projectile.ai[1] <= 0)
        {
            Projectile.ai[1] = MaterialistHazard.TraceBeam(Projectile.Center, Vector2.UnitY, 32);
            if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        }
        if (Main.dedServ) return;
        if (Age == 1)
        {
            SoundEngine.PlaySound(SoundID.Item158 with { Volume = .65f, Pitch = -.12f, MaxInstances = 4 },
                Projectile.Center + Vector2.UnitY * Math.Min(Reach, 700));
            for (int i = 0; i < 10; i++)
            {
                MaterialistVisuals.Nano(Projectile.Center, new Vector2(Main.rand.NextFloat(-3, 3), Main.rand.NextFloat(1, 5)),
                    i % 3 == 0 ? Color.White : MaterialistVisuals.Violet, .9f, 18);
                MaterialistVisuals.Nano(Projectile.Center + Vector2.UnitY * Reach,
                    Main.rand.NextVector2Circular(4, 2), MaterialistVisuals.Violet, .9f, 16);
            }
        }
        Lighting.AddLight(Projectile.Center, .35f * Strength, .12f * Strength, .5f * Strength);
    }
    public override bool? CanDamage() => Strength >= .15f && Reach > 1 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        Strength >= .15f && MaterialistArsenalRules.Capsule(targetHitbox, Projectile.Center,
            Projectile.Center + Vector2.UnitY * Reach, 32 * Strength);
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!Main.dedServ) MaterialistVisuals.Burst(target.Center, MaterialistVisuals.Violet, .55f);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        MaterialistVisuals.DrawFriendlyLaser(Projectile.Center, Vector2.UnitY, Reach, 32, Strength, Age);
        return false;
    }
}
