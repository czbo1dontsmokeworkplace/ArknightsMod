using System;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

// Only a held blade's first physical NPC contact may release a volley. These
// projectiles never spawn damaging children, including when they hit or expire.
public sealed class CrusherUpheaval : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    internal CrusherKind Kind => (CrusherKind)((int)Projectile.ai[0] % 4);
    internal bool Empowered => Projectile.ai[0] >= 4;
    internal bool Waiting => Projectile.ai[2] > 0;
    private readonly Vector2[] history = new Vector2[18];
    private readonly bool[] pulled = new bool[Main.maxNPCs];
    private int samples, pulls;
    private bool launched;
    internal static int VolleyCount(CrusherKind kind) => kind switch {
        CrusherKind.Quartz => 1, CrusherKind.WindChimes => 2, CrusherKind.Hoederer => 3, _ => 1 };
    internal static void Release(Projectile blade, NPC target, CrusherKind kind, int special, int skillLevel)
    {
        if (blade.owner != Main.myPlayer) return;
        int count = VolleyCount(kind);
        float speed = 36 + (int)kind * 8;
        float damage = kind switch { CrusherKind.Quartz => .85f, CrusherKind.WindChimes => .7f,
            CrusherKind.Hoederer => .75f, _ => 1.8f };
        bool empowered = kind == CrusherKind.Ulpianus && special == 2;
        if (empowered) damage = CrusherRules.Level(skillLevel, 1.7f, 2.7f);
        for (int i = 0; i < count; i++) {
            // World-down is intentional, including for players with inverted gravity.
            // Start below the struck NPC; ai[1] keeps that exact target for homing.
            Vector2 aim = target.Center + target.velocity * (10 + i * 3);
            float spread = (i - (count - 1) * .5f) * 100;
            Vector2 start = aim + new Vector2(spread, 400 + (int)kind * 40 + i * 36);
            start.X = Math.Clamp(start.X, 32, Main.maxTilesX * 16 - 32);
            start.Y = Math.Min(start.Y, Main.maxTilesY * 16 - 32);
            Vector2 velocity = kind == CrusherKind.Ulpianus ? -Vector2.UnitY : (aim - start).SafeNormalize(-Vector2.UnitY);
            int id = Projectile.NewProjectile(blade.GetSource_FromThis(), start, velocity * speed / 3,
                ModContent.ProjectileType<CrusherUpheaval>(), Math.Max(1, (int)(blade.damage * damage)),
                blade.knockBack * 1.25f, blade.owner, (int)kind + (empowered ? 4 : 0), target.whoAmI, i * 4);
            if (id >= 0 && id < Main.maxProjectiles) {
                Main.projectile[id].CritChance = blade.CritChance;
                Main.projectile[id].netUpdate = true;
            }
        }
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Melee;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.extraUpdates = 2; Projectile.timeLeft = 150;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => !Waiting;
    public override bool? CanDamage() => Waiting ? false : null;
    public override void AI()
    {
        if (Waiting) { Projectile.ai[2] = Math.Max(0, Projectile.ai[2] - 1f / 3); return; }
        if (!launched) { launched = true; CrusherUpheavalVisuals.Launch(Projectile, Kind); }
        TrackStruckEnemy();
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Opacity = Math.Min(1, Projectile.timeLeft / 24f);
        for (int i = Math.Min(samples, history.Length - 1); i > 0; i--) history[i] = history[i - 1];
        history[0] = Projectile.Center; samples = Math.Min(samples + 1, history.Length);
        if (Projectile.numUpdates == 0) CrusherUpheavalVisuals.Flight(Projectile, Kind);
    }
    private void TrackStruckEnemy()
    {
        int id = (int)Projectile.ai[1];
        if (id < 0) return;
        if (id >= Main.maxNPCs || !Main.npc[id].active || Main.npc[id].life <= 0
            || Main.npc[id].friendly || Main.npc[id].dontTakeDamage) {
            // Never acquire a different enemy if the original victim disappears.
            Projectile.ai[1] = -1; Projectile.netUpdate = true; return;
        }
        NPC target = Main.npc[id];
        float speed = (36 + (int)Kind * 8) / 3f;
        Vector2 offset = target.Center - Projectile.Center;
        float distance = offset.Length();
        // NPC velocity is per game tick, projectile velocity is per sub-update.
        // Close in on the actual center near contact instead of overshooting a lead point.
        float lead = distance < 100 ? 0 : Math.Min(6, distance / (speed * 3));
        Vector2 wanted = (offset + target.velocity * lead).SafeNormalize(-Vector2.UnitY);
        float turn = distance < 120 ? .5f : .18f;
        Vector2 direction = Vector2.Lerp(Projectile.velocity.SafeNormalize(-Vector2.UnitY), wanted, turn).SafeNormalize(wanted);
        Projectile.velocity = direction * speed;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (Waiting) return false;
        float collision = 0;
        float width = Kind == CrusherKind.Ulpianus ? 68 : 26 + (int)Kind * 8;
        // Swept contact stays reliable at high speed. Anchor crown, shaft and
        // visible leading edge occupy the same axis as this capsule.
        Vector2 axis = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            Projectile.Center - Projectile.velocity - axis * width * .5f,
            Projectile.Center + axis * width * .5f, width, ref collision);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (target.whoAmI == (int)Projectile.ai[1]) {
            // Having reached its victim, keep piercing forward without orbiting it.
            Projectile.ai[1] = -1; Projectile.netUpdate = true;
        }
        if (Empowered) CrusherCombatNet.Hit(Projectile, target);
        if (Projectile.owner == Main.myPlayer && Projectile.numHits < 5) {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Hitbox.ClosestPointInRect(Projectile.Center),
                Projectile.velocity.SafeNormalize(-Vector2.UnitY), ModContent.ProjectileType<CrusherImpact>(),
                0, 0, Projectile.owner, (int)Kind, Kind == CrusherKind.Ulpianus ? 1.9f : 1.15f);
        }
    }
    internal void ApplyPull(NPC target)
    {
        if (!Empowered || pulled[target.whoAmI] || pulls >= 2) return;
        pulled[target.whoAmI] = true; pulls++;
        if (!target.boss && target.realLife < 0 && target.knockBackResist > 0) {
            target.velocity += (Main.player[Projectile.owner].Center - target.Center).SafeNormalize(Vector2.Zero) * 10 * target.knockBackResist;
            target.netUpdate = true;
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!Waiting) CrusherUpheavalVisuals.Draw(Projectile, Kind, history, samples);
        return false;
    }
}
