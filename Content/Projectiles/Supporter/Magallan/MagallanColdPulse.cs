using System;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Supporter.Magallan;

public sealed class MagallanColdPulse : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Supporter/Magallan/MagallanDrone";
    private const int Lifetime = 42;
    private float Progress => 1f - Projectile.timeLeft / (float)Lifetime;
    private float Radius => Projectile.ai[0] * MathHelper.Clamp(Progress * 2.5f, 0f, 1f);

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.MinionShot[Type] = true;
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 340;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
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

    public override void AI()
    {
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = true;
        if (Projectile.localAI[0]++ == 0 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item28 with { Volume = 0.35f, Pitch = -0.35f, MaxInstances = 3 }, Projectile.Center);
        if (Projectile.localAI[0] <= 21f && ((int)Projectile.localAI[0] - 1) % 4 == 0)
            MagallanVisuals.ColdMist(Projectile.Center, Math.Max(12f, Radius), Projectile.ai[2] > 0);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(Projectile.Center, nearest) <= Radius * Radius
            && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height);
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (!MagallanColdGlobalNPC.IsBoss(target))
        {
            target.AddBuff(ModContent.BuffType<MagallanChill>(), 75);
            if (Projectile.ai[2] > 0) target.AddBuff(ModContent.BuffType<MagallanBind>(), 45);
        }
        MagallanVisuals.Puff(target.Center, new Vector2(0f, 0.6f), new Color(186, 220, 235),
            MathHelper.Clamp(target.width * 0.6f, 18f, 40f), 24, 0.35f, 0.02f);
    }

    // Rendering is entirely handled by the independent smoke particles.
    public override bool PreDraw(ref Color lightColor) => false;
}

public sealed class MagallanChill : ModBuff
{
    public override string Texture => $"Terraria/Images/Buff_{BuffID.Chilled}";
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
}

public sealed class MagallanBind : ModBuff
{
    public override string Texture => $"Terraria/Images/Buff_{BuffID.Frozen}";
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; }
}

public sealed class MagallanColdGlobalNPC : GlobalNPC
{
    internal static bool IsBoss(NPC npc) => npc.boss || NPCID.Sets.ShouldBeCountedAsBoss[npc.type]
        || (npc.realLife >= 0 && Main.npc[npc.realLife].boss);

    public override bool PreAI(NPC npc)
    {
        if (!IsBoss(npc) && npc.HasBuff<MagallanBind>())
        {
            npc.velocity = Vector2.Zero;
            return false;
        }
        return true;
    }

    public override void PostAI(NPC npc)
    {
        if (IsBoss(npc)) return;
        if (npc.HasBuff<MagallanBind>()) npc.velocity = Vector2.Zero;
        else if (npc.HasBuff<MagallanChill>()) npc.velocity *= 0.5f;
    }

    public override void DrawEffects(NPC npc, ref Color drawColor)
    {
        if (!npc.HasBuff<MagallanChill>() && !npc.HasBuff<MagallanBind>()) return;
        drawColor = Color.Lerp(drawColor, new Color(160, 220, 255), 0.35f);
        // Residual frost stays subtle; ongoing mist is emitted from AI, never from draw calls.
    }
}
