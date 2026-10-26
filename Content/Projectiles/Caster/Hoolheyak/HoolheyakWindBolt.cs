using System;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using ArknightsMod.Content.Items.Weapons.Caster.Hoolheyak;
using Microsoft.Xna.Framework;
using Terraria;
using System.IO;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Hoolheyak;

public sealed class HoolheyakWindBolt : ModProjectile, ICoreCasterGuidance
{
    internal const int SeekingSolo = 1, SeekingPair = 2, Barrage = 3, BarrageFinisher = 4;
    private int Mode => (int)Projectile.ai[0];
    private int Rank => Math.Clamp((int)Projectile.ai[2], 0, 9);
    private CoreCasterFlight flight;
    public bool GuidanceReady => flight?.CanGuide(CoreCasterKind.Hoolheyak) == true;
    public override void SendExtraAI(BinaryWriter writer) => CoreCasterGuidance.Write(writer, flight);
    public override void ReceiveExtraAI(BinaryReader reader) => flight = CoreCasterGuidance.Read(reader);
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 60;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = true;
        Projectile.penetrate = 1;
        Projectile.timeLeft = 270;
        Projectile.extraUpdates = 2;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }

    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanHitNPC(NPC target) => target.CanBeChasedBy(Projectile) ? null : false;

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead)
        {
            Projectile.Kill();
            return;
        }
        int targetIndex = (int)Projectile.ai[1] - 1;
        Vector2? destination = null;
        if (flight == null)
        {
            flight = new CoreCasterFlight(Projectile.Center, Projectile.velocity, Projectile.MaxUpdates, Projectile.identity);
        }
        if (Main.npc.IndexInRange(targetIndex))
        {
            NPC target = Main.npc[targetIndex];
            if (target.CanBeChasedBy(Projectile) && target.DistanceSQ(Projectile.Center) < 1100f * 1100f
                && HoolheyakWindCombat.ClearPath(Projectile.Center, target.Center))
                destination = target.Center;
        }
        if (!destination.HasValue && GuidanceReady)
            destination = CoreCasterGuidance.Target(Projectile, 1100);
        flight.Step(CoreCasterKind.Hoolheyak, Mode, Projectile.Center, ref Projectile.velocity, Projectile.MaxUpdates, destination);
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (Projectile.numUpdates != 0) return;
        Projectile.localAI[0]++;
        // Fine particles follow the shared energy core and the mint wind palette.
        if (!Main.dedServ && Projectile.localAI[0] % 2 == 0)
            HoolheyakWindVisuals.Mote(Projectile.Center, -Projectile.velocity * .08f, .65f);
        if (!Main.dedServ && Projectile.localAI[0] % (Mode >= Barrage ? 4 : 2) == 0)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float phase = Projectile.localAI[0] * .55f + Projectile.identity;
            Vector2 offset = side * MathF.Sin(phase) * 7f - forward * 12f;
            HoolheyakWindVisuals.WindSpark(Projectile.Center + offset,
                Projectile.velocity * .12f + side * MathF.Cos(phase) * .7f, .65f);
        }
    }

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (target.GetGlobalNPC<HoolheyakWindNPC>().IsLifted)
            modifiers.SourceDamage *= 1.2f;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Projectile.owner != Main.myPlayer)
            return;
        int liftTicks = Mode == SeekingSolo ? HoolheyakWindArts.SeekingLift[Rank]
            : Mode >= Barrage ? (Main.rand.NextFloat() < HoolheyakWindArts.BarrageLiftChance[Rank] ? 60 : 0)
            : Mode == 0 ? 24 : 0;
        if (liftTicks > 0 && HoolheyakWindCombat.CanLift(target))
            target.AddBuff(ModContent.BuffType<HoolheyakUpdraft>(), liftTicks);

        if (Mode == Barrage)
            return;
        float multiplier = Mode is SeekingSolo or SeekingPair ? HoolheyakWindArts.SeekingDamage[Rank]
            : Mode == BarrageFinisher ? HoolheyakWindArts.BarrageDamage[Rank] : 1f;
        // 风場伤害以普通攻击基数计算，强化弹不会把多倍伤害永久灌进持续风场。
        HoolheyakSmallVortex.CreateOrFeed(Projectile, (int)MathF.Round(Projectile.damage / multiplier), target);
    }

    public override void OnKill(int timeLeft) => CoreCasterVisuals.Impact(Projectile.Center, Projectile.velocity,
        CoreCasterKind.Hoolheyak, Mode is SeekingSolo or SeekingPair or BarrageFinisher);

    public override bool PreDraw(ref Color lightColor)
    {
        CoreCasterVisuals.Bolt(Projectile, CoreCasterKind.Hoolheyak, Mode >= Barrage ? 0 : Mode);
        return false;
    }
}
