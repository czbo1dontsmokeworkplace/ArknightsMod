using System;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using ArknightsMod.Content.Items.Weapons.Sniper.Typhon;
using ArknightsMod.Content.Buffs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon;

public sealed class TyphonAstralShot : ModProjectile
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Sniper/Typhon/TyphonArrow";
    public bool Guided => Projectile.ai[0] == 1;
    public bool SkillTwo => Projectile.ai[0] == 2;
    public int Age => (int)Projectile.ai[2];
    public const int WanderFrames = 0;
    public const float LeftScale = 1.7f, RightScale = LeftScale * .7f, LaunchSpeed = 9f;
    public static float HomingStrength(float age) => 1f;
    public static Vector2 PursuitVelocity(Vector2 velocity, Vector2 toTarget, float age, int updates)
    {
        float speed = MathHelper.Lerp(MathF.Max(3, velocity.Length()), 18, 1 - MathF.Pow(.74f, 1f / updates));
        Vector2 targetDirection = toTarget.SafeNormalize(velocity.SafeNormalize(Vector2.UnitX));
        // Right-click arrows lock directly onto the target, without a turn limit or wandering phase.
        return targetDirection * speed;
    }
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 32; ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18; Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.timeLeft = 1200; Projectile.extraUpdates = 3;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        Projectile.penetrate = Guided ? 1 : -1;
        Projectile.scale = Guided ? RightScale : LeftScale;
        if (!Main.dedServ)
        {
            TyphonAstralVisuals.Fire(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), !Guided);
            if (Guided) SoundEngine.PlaySound(SoundID.Item9 with { Volume = .3f, Pitch = .25f, MaxInstances = 3 }, Projectile.Center);
            else if (SkillTwo) SoundEngine.PlaySound(TyphonBow.FireSound, Projectile.Center);
        }
    }
    public override bool? CanDamage() => null;
    public override void AI()
    {
        if (Projectile.numUpdates == 0) Projectile.ai[2]++;
        if (Guided || SkillTwo)
        {
            NPC target = null;
            int pinned = (int)Projectile.ai[1] - 1;
            if (SkillTwo && Main.npc.IndexInRange(pinned) && Main.npc[pinned].CanBeChasedBy(Projectile))
                target = Main.npc[pinned];
            target ??= Projectile.FindTargetWithinRange(1400, false);
            Vector2 toTarget = target != null ? target.Center - Projectile.Center : Projectile.velocity;
            Projectile.velocity = SkillTwo
                ? toTarget.SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * Projectile.velocity.Length()
                : PursuitVelocity(Projectile.velocity, toTarget, Age, Projectile.MaxUpdates);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.numUpdates == 0 && !Main.dedServ) TyphonAstralVisuals.Flight(Projectile);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - Projectile.velocity,
            Projectile.Center, Guided ? 12 : 28, ref point);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (SkillTwo) modifiers.DisableCrit();
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (SkillTwo)
        {
            TyphonSkillArrowStun.TryApply(target, Main.player[Projectile.owner], Projectile);
            if (Main.rand.NextFloat() < .4f) target.AddBuff(BuffID.Confused, 60);
        }
        ChargedSniperVisuals.HitBurst(target.Hitbox.ClosestPointInRect(Projectile.Center), Projectile.velocity.SafeNormalize(Vector2.UnitX),
            ChargedOperatorKind.Fartooth, true, hit.Crit);
    }
    public override bool PreDraw(ref Color lightColor) { TyphonAstralVisuals.DrawShot(Projectile); return false; }
}
