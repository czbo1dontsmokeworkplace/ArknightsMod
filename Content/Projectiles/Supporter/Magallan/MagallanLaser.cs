using System;
using ArknightsMod.Content.Projectiles.Caster.MechAccord;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Supporter.Magallan;

public sealed class MagallanLaser : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Supporter/Magallan/MagallanDrone";
    private Vector2 start;
    private bool hitTarget;
    private const int Lifetime = 15;
    internal const float MaxLength = 19.5f * 16f;

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 2500;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 4;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.friendly = true;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = Lifetime;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanDamage() => hitTarget ? false : null;
    public override bool? CanHitNPC(NPC target) =>
        target.whoAmI == Math.Abs((int)Projectile.ai[2]) - 1 && target.type == (int)Projectile.ai[1]
            ? null : false;

    public override void AI()
    {
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = true;
        MagallanDrone drone = null;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.identity == (int)Projectile.ai[0] && p.ModProjectile is MagallanDrone found)
            { drone = found; break; }
        int targetId = Math.Abs((int)Projectile.ai[2]) - 1;
        if (drone == null || drone.Module != 1 || drone.Recalling ||
            targetId < 0 || targetId >= Main.maxNPCs || !Main.npc[targetId].CanBeChasedBy() ||
            Main.npc[targetId].type != (int)Projectile.ai[1])
        { Projectile.Kill(); return; }

        start = drone.Muzzle;
        Projectile.Center = Main.npc[targetId].Center;
        if (Projectile.localAI[0]++ == 0 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.4f, Pitch = 0.2f, MaxInstances = 3 }, start);
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, 0.2f, 0.55f, 0.3f);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitTarget = true;
        MagallanVisuals.Puff(target.Center, Vector2.Zero, new Color(151, 200, 174), 28f, 28, 0.4f);
        MagallanVisuals.SparkBurst(target.Center, MagallanVisuals.ModuleColor(1), 3, 1.5f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        float fade = MathF.Pow(Projectile.timeLeft / (float)Lifetime, 3f);
        ClickTrackerLaser.DrawBeam(start, Projectile.Center, fade, 1, new Color(83, 235, 145));
        return false;
    }
}
