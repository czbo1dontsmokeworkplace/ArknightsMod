using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

public sealed class RockrockStarBolt : ModProjectile
{
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";
    private Vector2 origin, destination;
    private bool ready;
    private readonly Vector2[] history = new Vector2[22];
    private int historyCount;
    private int Age => 150 - Projectile.timeLeft;
    internal static void SpawnFan(IEntitySource source, Vector2 muzzle, NPC target, int owner, int damage,
        float knockback, bool burst = false, int volley = 0)
    {
        int type = ModContent.ProjectileType<RockrockStarBolt>();
        if (Main.player[owner].ownedProjectileCounts[type] >= 60) return;
        int count = burst ? 5 : 3;
        // Positive rotation is clockwise in Terraria's downward-positive screen coordinates.
        // The shared offset rotates the whole fan while preserving equal spacing inside each volley.
        float baseAngle = -MathHelper.PiOver2 + volley * MathHelper.Pi / 20f;
        for (int slot = 0; slot < count; slot++)
        {
            int id = Projectile.NewProjectile(source, muzzle, Vector2.Zero, type,
                damage / count + (slot < damage % count ? 1 : 0), knockback, owner, target?.whoAmI ?? -1, slot, burst ? 1 : 0);
            if (id >= Main.maxProjectiles) continue;
            var bolt = (RockrockStarBolt)Main.projectile[id].ModProjectile;
            bolt.origin = muzzle; bolt.destination = target?.Center ?? muzzle; bolt.ready = true;
            float angle = baseAngle + MathHelper.TwoPi * slot / count;
            bolt.Projectile.velocity = angle.ToRotationVector2() * (burst ? 8f : 6f);
            Main.projectile[id].netUpdate = true;
        }
        SoundEngine.PlaySound(SoundID.Item9 with { MaxInstances=1 },muzzle);
    }
    internal static Vector2 Steer(Vector2 velocity, Vector2 position, Vector2 target, Vector2 targetVelocity, int age, bool burst)
    {
        // First spread out, then turn continuously with increasing speed and angular authority.
        float progress=Math.Clamp((age-8f)/80f,0,1);
        float speed=MathHelper.Lerp(burst?8f:6f,burst?36f:30f,progress);
        if(age<8)return velocity.SafeNormalize(Vector2.UnitX)*speed;
        float distance=Vector2.Distance(position,target);
        Vector2 predicted=target+targetVelocity*Math.Min(8f,distance/Math.Max(1,speed)*.35f);
        float desired=(predicted-position).ToRotation();
        float turn=MathHelper.Lerp(.035f,.34f,progress);
        float angle=velocity.ToRotation();
        float error=MathHelper.WrapAngle(desired-angle);
        angle+=Math.Clamp(error,-turn,turn);
        // Brake gracefully through tight turns instead of orbiting a target forever at top speed.
        float alignment=MathHelper.Lerp(.12f,1f,MathF.Pow(Math.Max(0,MathF.Cos(error)),2));
        float actualSpeed=MathHelper.Lerp(velocity.Length(),speed*alignment,.16f+progress*.2f);
        return angle.ToRotationVector2()*actualSpeed;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Magic;
        Projectile.penetrate = 1; Projectile.timeLeft = 150;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Age < 9 ? false : null;
    public override bool? CanHitNPC(NPC target) => Projectile.ai[0] < 0 || target.whoAmI == (int)Projectile.ai[0] ? null : false;
    public override void SendExtraAI(BinaryWriter writer)
    { writer.Write(ready); writer.WriteVector2(origin); writer.WriteVector2(destination); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        bool initialized = reader.ReadBoolean(); Vector2 a = reader.ReadVector2(), b = reader.ReadVector2();
        if (float.IsFinite(a.X) && float.IsFinite(a.Y) && float.IsFinite(b.X) && float.IsFinite(b.Y))
        { ready = initialized; origin = a; destination = b; }
    }
    public override void AI()
    {
        if (!ready) return;
        int index = (int)Projectile.ai[0];
        NPC target = index >= 0 && index < Main.maxNPCs && Main.npc[index].CanBeChasedBy()
            ? Main.npc[index] : null;
        if (target != null && target.DistanceSQ(Main.player[Projectile.owner].Center)>2200f*2200f)
            target = null;
        Vector2 previous=Projectile.Center;
        bool burst=Projectile.ai[2]>0;
        if (target != null)
        {
            destination=target.Center;
            Projectile.velocity=Steer(Projectile.velocity,Projectile.Center,destination,target.velocity,Age,burst);
        }
        else
        {
            Projectile.ai[0] = -1f;
            Projectile.velocity *= 1.02f;
        }
        Projectile.Center+=Projectile.velocity;
        Projectile.rotation=Projectile.velocity.ToRotation();
        for (int i = history.Length - 1; i > 0; i--) history[i] = history[i - 1];
        history[0] = Projectile.Center; historyCount = Math.Min(history.Length, historyCount + 1);
        if (Projectile.owner == Main.myPlayer && Age % 6 == 0) Projectile.netUpdate = true;
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, .22f, .08f, .3f);
            if (Age % 3 == 0) MechAccordVisuals.Wake(Projectile.Center, Projectile.Center - previous, Age);
        }
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float point = 0f;
        return historyCount > 1 && Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
            history[1], Projectile.Center, 9f, ref point);
    }
    public override void OnKill(int timeLeft) => MechAccordHitFlash.Spawn(Projectile, Projectile.Center, true, 48f);
    public override bool PreDraw(ref Color lightColor)
    {
        if (!ready) return false;
        Vector2[] points = new Vector2[historyCount]; Array.Copy(history, points, historyCount);
        float fade = Math.Min(1f, (Age + 1) / 5f);
        MechAccordVfx.DrawTrail(points, fade);
        MechAccordVfx.DrawStar(Projectile.Center, Projectile.rotation, fade);
        return false;
    }
}

