using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster.CoreCasters;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.CoreCasters;

public sealed class CoreCasterBolt : ModProjectile, ICoreCasterGuidance
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private CoreCasterKind Kind => (CoreCasterKind)(int)Projectile.ai[0];
    private int Mode => (int)Projectile.ai[1];
    private bool Volcano => Kind == CoreCasterKind.Eyjafjalla && Mode == 3;
    private int age, primary = -1;
    private float particleDistance;
    private CoreCasterFlight flight;
    public bool GuidanceReady => flight?.CanGuide(Kind, Mode) == true;
    private Vector2? aimPoint;
    internal void SetFlightAim(Vector2 point) { aimPoint = point; Projectile.netUpdate = true; }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(aimPoint.HasValue);
        if (aimPoint.HasValue) { writer.Write(aimPoint.Value.X); writer.Write(aimPoint.Value.Y); }
        CoreCasterGuidance.Write(writer, flight);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        aimPoint = reader.ReadBoolean() ? new Vector2(reader.ReadSingle(), reader.ReadSingle()) : null;
        flight = CoreCasterGuidance.Read(reader);
    }
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 80;
        ProjectileID.Sets.TrailingMode[Type] = 0;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 14;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = Projectile.ignoreWater = true;
        Projectile.penetrate = 1; Projectile.timeLeft = 150; Projectile.extraUpdates = 1;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source)
    {
        if (Kind == CoreCasterKind.Absinthe)
        { Projectile.extraUpdates = 3; Projectile.timeLeft = 300; }
        if (Volcano) { Projectile.extraUpdates = 2; Projectile.timeLeft = 450; }
        CoreCasterFlight.Configure(Projectile, Kind, Volcano);
    }
    public override void AI()
    {
        age++;
        if (flight == null)
        {
            flight = new CoreCasterFlight(Projectile.Center, Projectile.velocity, Projectile.MaxUpdates, Projectile.identity, aimPoint);
        }
        NPC target = Projectile.ai[2] > 0 && Main.npc.IndexInRange((int)Projectile.ai[2] - 1)
            ? Main.npc[(int)Projectile.ai[2] - 1] : null;
        if (target != null && (!target.CanBeChasedBy() || Kind == CoreCasterKind.Absinthe && Mode == 2 && CoreCasterRules.LifeFraction(target) > .5f))
            target = null;
        bool seeking = GuidanceReady;
        if (seeking && target == null && (!Volcano || Projectile.owner == Main.myPlayer) && age % (3 * Projectile.MaxUpdates) == 1)
        {
            Vector2 focus = Volcano ? aimPoint ?? Projectile.Center : Projectile.Center;
            var choices = CoreCasterRules.Targets(focus, Volcano ? 360 : 650, focus, Kind == CoreCasterKind.Absinthe,
                Kind == CoreCasterKind.Absinthe && Mode == 2 ? .5f : 1);
            if (choices.Count > 0) { target = choices[0]; Projectile.ai[2] = target.whoAmI + 1; Projectile.netUpdate = true; }
        }
        Vector2? destination = seeking && target != null && (Volcano || CoreCasterRules.Clear(Projectile.Center, target.Center)) ? target.Center : null;
        bool falling = flight.Descending;
        flight.Step(Kind, Mode, Projectile.Center, ref Projectile.velocity, Projectile.MaxUpdates, destination);
        if (Volcano)
        {
            // Matches the artillery family: ascent hits ceilings, descent can reach the strike zone below terrain.
            Projectile.tileCollide = !flight.Descending;
            if (falling != flight.Descending && Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
            Vector2 strike = target?.Center ?? aimPoint ?? Projectile.Center + Vector2.UnitY * 600;
            Vector2 remaining = strike - Projectile.Center;
            if (flight.Descending && (target == null && remaining.LengthSquared() < 26 * 26
                || remaining.LengthSquared() < 60 * 60 && Vector2.Dot(remaining, Projectile.velocity) < 0))
            { Projectile.Kill(); return; }
        }
        Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ)
        {
            var color = CoreCasterArt.ColorFor(Kind);
            Lighting.AddLight(Projectile.Center, color.ToVector3() * .42f);
            particleDistance += Projectile.velocity.Length();
            if (particleDistance >= (Volcano ? 64 : 30))
            { particleDistance = 0; CoreCasterVisuals.Trail(Projectile, Kind, Mode); }
        }
    }
    public override bool? CanHitNPC(NPC target) => Kind == CoreCasterKind.Absinthe && Mode == 2
        && CoreCasterRules.LifeFraction(target) > .5f ? false : null;
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Kind == CoreCasterKind.Absinthe && CoreCasterRules.LifeFraction(target) <= .4f) modifiers.SourceDamage *= 1.25f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        primary = target.whoAmI;
        if (Kind == CoreCasterKind.Eyjafjalla)
        {
            target.AddBuff(BuffID.OnFire3, Mode == 0 ? 180 : 300);
            if (Mode == 2) target.AddBuff(ModContent.BuffType<EyjafjallaScorch>(), 360);
        }
        if (Kind == CoreCasterKind.Nightmare && Mode == 1 && Projectile.owner == Main.myPlayer)
            CoreCasterRules.Spawn(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.UnitY * -2,
                ModContent.ProjectileType<CoreCasterMend>(), 0, 0, Projectile.owner);
    }
    public override void OnKill(int timeLeft)
    {
        CoreCasterVisuals.Retire(Projectile, Kind, Mode);
        CoreCasterVisuals.Impact(Projectile.Center, Projectile.velocity, Kind, Mode > 0, Mode);
        if (Kind == CoreCasterKind.Eyjafjalla && Mode is 2 or 3 && CoreCasterRules.Authority)
        {
            float radius = Mode == 2 ? 104 : 62;
            CoreCasterRules.Spawn(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CoreCasterPulse>(), Math.Max(1, Projectile.damage / 2), Projectile.knockBack,
                Projectile.owner, (int)Kind, radius, primary + 1, Projectile.CritChance);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        CoreCasterVisuals.Bolt(Projectile, Kind, Mode);
        return false;
    }
}

public sealed class CoreCasterPulse : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private int age;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.friendly = true; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 24;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => age <= 3 && Projectile.damage > 0 ? null : false;
    public override bool? CanHitNPC(NPC target)
    {
        // Nightmare pulses affect only their marked target; Ignition splash excludes the direct-hit target.
        if ((CoreCasterKind)(int)Projectile.ai[0] == CoreCasterKind.Nightmare)
            return target.whoAmI == (int)Projectile.ai[2] - 1 ? null : false;
        return target.whoAmI == (int)Projectile.ai[2] - 1 ? false : null;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        float radius = Math.Clamp(Projectile.ai[1], 1, 160);
        return Vector2.DistanceSquared(nearest, Projectile.Center) <= radius * radius
            && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height);
    }
    public override void AI()
    {
        if (++age == 1) CoreCasterVisuals.Burst(Projectile.Center, (CoreCasterKind)(int)Projectile.ai[0], Projectile.ai[1]);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if ((CoreCasterKind)(int)Projectile.ai[0] == CoreCasterKind.Eyjafjalla) target.AddBuff(BuffID.OnFire3, 240);
    }
    public override bool PreDraw(ref Color lightColor) => false;
}

public sealed class CoreCasterMend : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6; Projectile.timeLeft = 90;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        Vector2 offset = owner.Center - Projectile.Center;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, offset.SafeNormalize(Vector2.Zero) * 18, .16f);
        if (Projectile.timeLeft % 3 == 0) CoreCasterVisuals.Mend(Projectile.Center, Projectile.velocity);
        if (offset.LengthSquared() < 24 * 24 && Projectile.owner == Main.myPlayer)
        { owner.GetModPlayer<CoreCasterPlayer>().ReceiveDrain(); Projectile.Kill(); }
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
