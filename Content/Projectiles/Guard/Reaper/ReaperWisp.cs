using System;
using ArknightsMod.Content.Items.Weapons.Guard.Reaper;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Reaper;

// Radial spray followed by fast, direction-aware seeking. Ship 81's visual graph
// supplies the fluttering body, trails and impact, using each scythe's palette.
// No ReaperPlayer hit callbacks: these ranged motes neither heal nor recover SP.
public sealed class ReaperWisp : ModProjectile
{
    private ReaperSwarmInstance swarm;
    internal const int Lifetime = 84;
    internal const int DriftTicks = 20;
    internal ReaperKind Kind => (ReaperKind)(int)Projectile.ai[0];
    internal int Age => Lifetime - Projectile.timeLeft;
    public override string Texture => "Terraria/Images/MagicPixel";

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 24;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = 1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = Lifetime;
    }
    public override bool? CanDamage() => Age >= DriftTicks ? null : false;
    public override void AI()
    {
        if (Age < DriftTicks) Projectile.velocity *= .995f;
        else {
            NPC target = null;
            int locked = (int)Projectile.ai[1] - 1;
            bool Valid(NPC npc) => npc.CanBeChasedBy(Projectile)
                && Vector2.DistanceSquared(npc.Center, Projectile.Center) <= 720 * 720
                && Collision.CanHitLine(Projectile.position, Projectile.width, Projectile.height,
                    npc.position, npc.width, npc.height);
            if (locked >= 0 && locked < Main.maxNPCs && Valid(Main.npc[locked])) target = Main.npc[locked];
            if (target == null && Projectile.owner == Main.myPlayer) {
                float best = float.MaxValue;
                Vector2 heading = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                foreach (NPC npc in Main.ActiveNPCs) {
                    if (!Valid(npc)) continue;
                    Vector2 offset = npc.Center - Projectile.Center;
                    // Prefer the outward sector this mote occupies instead of collapsing the spray
                    // onto whichever enemy is a few pixels closer to the player.
                    float score = offset.Length() * (1 + (1 - Vector2.Dot(heading, offset.SafeNormalize(heading))) * .85f);
                    if (score >= best) continue;
                    target = npc;
                    best = score;
                }
                int next = target == null ? 0 : target.whoAmI + 1;
                if ((int)Projectile.ai[1] != next) { Projectile.ai[1] = next; Projectile.netUpdate = true; }
            }
            if (target != null) {
                Vector2 offset = target.Center - Projectile.Center;
                float turnRate = MathHelper.Lerp(.22f, .48f, 1 - Math.Clamp(offset.Length() / 140, 0, 1));
                float heading = Projectile.velocity.ToRotation().AngleTowards(offset.ToRotation(), turnRate);
                float speed = Math.Min(ReaperRules.WispSeekSpeed, Projectile.velocity.Length() + .85f);
                Projectile.velocity = heading.ToRotationVector2() * speed;
            }
            else Projectile.velocity *= .985f;
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        Projectile.Opacity = Math.Min(1, (Age + 1) / 5f) * Math.Min(1, Projectile.timeLeft / 16f);
        if(!Main.dedServ) {
            swarm ??= new ReaperSwarmInstance(this);
            swarm.Update(Age);
            Lighting.AddLight(Projectile.Center,ReaperRules.Color(Kind).ToVector3()*(.38f*Projectile.Opacity));
        }
    }
    public override void OnKill(int timeLeft)
    {
        if(!Main.dedServ)swarm?.Stop();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (!ReaperVisuals.Visible(Projectile.Center)) return false;
        // The client visual system draws this projectile's imported graph once.
        return false;
    }
}
