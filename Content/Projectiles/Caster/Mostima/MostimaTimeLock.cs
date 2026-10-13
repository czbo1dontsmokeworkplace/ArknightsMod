using System;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Common.Particle;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Mostima;

public sealed class MostimaTimeLock : ModProjectile
{
    public override string Texture => MostimaVisuals.Assets + "ChronoDial";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 600;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 420;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 60;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => (int)Projectile.ai[1] % 60 == 1 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => Vector2.DistanceSquared(targetHitbox.ClosestPointInRect(Projectile.Center), Projectile.Center) <= 300 * 300;
    public override void AI()
    {
        Projectile.ai[1]++;
        if (Projectile.ai[1] >= Projectile.ai[0] || !Main.player[Projectile.owner].active || Main.player[Projectile.owner].dead)
        { Projectile.Kill(); return; }
        if (Main.netMode != NetmodeID.MultiplayerClient && (int)Projectile.ai[1] % 30 == 1)
            foreach (NPC npc in Main.ActiveNPCs)
                if (npc.CanBeChasedBy() && !npc.boss && npc.realLife < 0
                    && Vector2.DistanceSquared(npc.Center, Projectile.Center) <= 300 * 300)
                    OperatorStunNPC.TryApply(npc, 31);
        if (!Main.dedServ && (int)Projectile.ai[1] % 60 == 1)
        {
            MostimaVisuals.Particles(Projectile.Center, 18, 8);
            if (ParticleManager.activeParticles.Count < 1500)
                for (int i = 0; i < 12; i++)
                    new MostimaOrbitGlyph(Projectile.Center, 280f, i * MathHelper.TwoPi / 12f,
                        .002f, MostimaVisuals.Blue, 54).Spawn();
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Math.Min(MathHelper.Clamp(Projectile.ai[1] / 15, 0, 1),
            MathHelper.Clamp((Projectile.ai[0] - Projectile.ai[1]) / 25, 0, 1));
        using var pass = MostimaVisuals.Begin();
        MostimaVisuals.Dial(Projectile.Center, new Vector2(600), 0, MostimaVisuals.Blue * fade * .45f);
        MostimaVisuals.Dial(Projectile.Center, new Vector2(510), 0, MostimaVisuals.Violet * fade * .2f);
        // A stepped second hand, rather than smooth rotation, communicates arrested time.
        float angle = (int)(Projectile.ai[1] / 60) * MathHelper.PiOver4;
        MostimaVisuals.Sprite("ChronoHand", Projectile.Center + angle.ToRotationVector2() * 100f,
            new Vector2(210, 24), MostimaVisuals.Blue * fade * .75f, angle);
        MostimaVisuals.Sprite("TemporalSmear", Projectile.Center, new Vector2(430),
            MostimaVisuals.Blue * fade * .14f, -Projectile.ai[1] * .004f);
        MostimaVisuals.Sprite("TemporalStar", Projectile.Center, new Vector2(52f),
            new Color(216, 240, 255) * fade * .7f);
        return false;
    }
}
