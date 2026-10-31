using System;
using System.IO;
using ArknightsMod.Content.Projectiles.Sniper.Crossbows;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Pozemka;

// A single Influx-Waver-style return: vanish, relocate, then one short inward
// strike against the originally hit NPC. It cannot spawn another return shot.
public sealed class PozemkaBlinkStrike : ModProjectile
{
    private const int WarpAt = 9, StrikeAt = 12, StrikeUpdates = 6, Lifetime = 24;
    private int age;
    private bool warped;
    private Vector2 start, previous;
    private NPC Target => (int)Projectile.ai[0] >= 0 && (int)Projectile.ai[0] < Main.maxNPCs
        ? Main.npc[(int)Projectile.ai[0]] : null;
    public override string Texture => "ArknightsMod/Content/Projectiles/Sniper/Pozemka/PozemkaCrossbow_Projectile";
    internal static void Spawn(IEntitySource source, Projectile arrow, NPC target)
    {
        if (arrow.owner != Main.myPlayer) return;
        var shot = Projectile.NewProjectileDirect(source, target.Center, arrow.velocity,
            ModContent.ProjectileType<PozemkaBlinkStrike>(), arrow.damage, arrow.knockBack, arrow.owner,
            target.whoAmI, Main.rand.NextFloat(MathHelper.TwoPi), target.netID);
        shot.CritChance = arrow.CritChance;
        var data = shot.GetGlobalProjectile<CrossbowShotData>();
        data.Kind = (int)CrossbowKind.Pozemka;
        data.Skill = arrow.GetGlobalProjectile<CrossbowShotData>().Skill;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = 1; Projectile.timeLeft = Lifetime; Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true; Projectile.tileCollide = false;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => age >= StrikeAt && age < StrikeAt + StrikeUpdates ? null : false;
    public override bool? CanHitNPC(NPC npc) => npc.whoAmI == (int)Projectile.ai[0] && npc.netID == (int)Projectile.ai[2] ? null : false;
    public override void AI()
    {
        NPC target = Target;
        if (target == null || target.netID != (int)Projectile.ai[2] || !target.CanBeChasedBy(Projectile))
        { Projectile.Kill(); return; }
        age++; previous = Projectile.Center;
        if (age < WarpAt) Projectile.Opacity = 1f - age / (float)WarpAt;
        else
        {
            if (!warped)
            {
                warped = true;
                float radius = Math.Max(target.width, target.height) * .5f + 96;
                start = target.Center + Projectile.ai[1].ToRotationVector2() * radius;
                Projectile.Center = previous = start;
                if (Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            }
            Projectile.Opacity = Math.Clamp((age - WarpAt) / (float)(StrikeAt - WarpAt), 0, 1);
            if (age >= StrikeAt)
                Projectile.Center = Vector2.Lerp(start, target.Center, Math.Clamp((age - StrikeAt + 1) / (float)StrikeUpdates, 0, 1));
            Projectile.velocity = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (age >= StrikeAt + StrikeUpdates) Projectile.Kill();
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (projHitbox.Intersects(targetHitbox)) return true;
        float point = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), previous, Projectile.Center, 10, ref point);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => CrossbowSignatureVfx.Hit(Projectile, CrossbowKind.Pozemka);
    public override void SendExtraAI(BinaryWriter writer)
    { writer.Write(age); writer.Write(warped); writer.Write(start.X); writer.Write(start.Y); writer.Write(previous.X); writer.Write(previous.Y); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        age = Math.Clamp(reader.ReadInt32(), 0, Lifetime); warped = reader.ReadBoolean();
        start = new(reader.ReadSingle(), reader.ReadSingle()); previous = new(reader.ReadSingle(), reader.ReadSingle());
    }
    public override bool PreDraw(ref Color lightColor)
    { CrossbowSignatureVfx.DrawNativeCore(Projectile, CrossbowKind.Pozemka); return false; }
}
