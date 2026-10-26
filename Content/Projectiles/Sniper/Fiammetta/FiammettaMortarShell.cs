using System;
using System.IO;
using ArknightsMod.Content.Projectiles.Sniper.Artillery;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Fiammetta;

// Shared NukeOfBliss flight for Catapult/Meteorite/Fiammetta, with their original damage and explosions.
public class FiammettaMortarShell : ModProjectile
{
    private readonly BlissfulMortarFlight flight = new();
    private bool detonating;
    private int lockedTargetIndex = -1;
    private float gravityDirection = 1;
    private Vector2 MouseTarget => new(Projectile.ai[0], Projectile.ai[1]);
    protected virtual int FlightMode => Math.Clamp((int)Projectile.ai[2], 0, 3);
    protected virtual Color TrailOuter => new(255, 55, 20);
    protected virtual Color TrailInner => new(255, 233, 160);
    protected virtual float TrailWidth => FlightMode == 3 ? 26 : 23;
    public override string Texture => $"Terraria/Images/Projectile_{ProjectileID.RocketI}";
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 36;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = 16; Projectile.height = 28; Projectile.friendly = false;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.timeLeft = 700; Projectile.DamageType = DamageClass.Ranged;
        Projectile.netImportant = true; Projectile.extraUpdates = 2;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        gravityDirection = owner.gravDir < 0 ? -1 : 1;
        if (Projectile.owner == Main.myPlayer && flight.Time > 120 && (lockedTargetIndex < 0 || flight.Rain > 0)) LockTarget();
        NPC target = Main.npc.IndexInRange(lockedTargetIndex) ? Main.npc[lockedTargetIndex] : null;
        if (target != null && (!target.CanBeChasedBy(Projectile) || (target.Center.Y - Projectile.Center.Y) * gravityDirection <= 0))
        {
            target = null;
            if (Projectile.owner == Main.myPlayer) { lockedTargetIndex = -1; Projectile.netUpdate = true; }
        }
        Vector2 center = Projectile.Center; bool diving = flight.Diving;
        bool transfer = flight.Step(ref center, ref Projectile.velocity, MouseTarget, owner.Center, target?.Center, gravityDirection);
        Projectile.Center = center; Projectile.Opacity = flight.Opacity; Projectile.localAI[0] = flight.Time;
        if (transfer) Array.Fill(Projectile.oldPos, Projectile.position);
        if (flight.Rain <= 1 && flight.Time > 120) Projectile.extraUpdates = 10;
        if (flight.Diving && !diving && Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        if (flight.Staging) return;
        bool collideTiles = flight.Diving && (Projectile.Center.Y - MouseTarget.Y) * gravityDirection > 0;
        int steps = Math.Max(1, (int)MathF.Ceiling(Projectile.velocity.Length() / 4));
        Vector2 step = Projectile.velocity / steps;
        for (int i = 0; i < steps; i++)
        {
            Vector2 next = Projectile.position + step;
            if (collideTiles && Collision.SolidCollision(next, Projectile.width, Projectile.height)) { Detonate(); return; }
            Projectile.position = next;
            if (TouchesEnemy()) { Detonate(); return; }
        }
        if (flight.Diving)
        {
            Vector2 strike = target?.Center ?? MouseTarget;
            float distance = Vector2.Distance(Projectile.Center, strike);
            if (target == null && distance <= 26 || distance <= 74 && Vector2.Dot(strike - Projectile.Center, Projectile.velocity) < 0) { Detonate(); return; }
        }
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        if (!Main.dedServ && Projectile.Opacity > .2f)
        {
            Lighting.AddLight(Projectile.Center, TrailInner.ToVector3() * (.55f * Projectile.Opacity));
            Projectile.localAI[1] += Projectile.velocity.Length();
            if (Projectile.localAI[1] > 34) { Projectile.localAI[1] %= 34; SpawnFlightDust(); }
        }
    }
    private bool TouchesEnemy()
    {
        foreach (NPC npc in Main.ActiveNPCs) if (npc.CanBeChasedBy(Projectile) && npc.Hitbox.Intersects(Projectile.Hitbox)) return true;
        return false;
    }
    private void LockTarget()
    {
        Vector2 focus = flight.Rain == 0 ? Projectile.Center + Projectile.velocity * 4 : MouseTarget;
        float best = flight.Rain == 0 ? 350 : 250; int index = -1;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy(Projectile) || (npc.Center.Y - Projectile.Center.Y) * gravityDirection <= 0) continue;
            float distance = Vector2.Distance(focus, npc.Center);
            if (distance < best) { best = distance; index = npc.whoAmI; }
        }
        if (index != lockedTargetIndex) { lockedTargetIndex = index; Projectile.netUpdate = true; }
    }
    private void Detonate()
    {
        if (Projectile.owner != Main.myPlayer || detonating) return;
        detonating = true; Projectile.netUpdate = true; Projectile.Kill();
    }
    private void SpawnFlightDust() => ArtilleryFlightVisuals.Emit(Projectile, TrailOuter, TrailInner);
    public override void OnKill(int timeLeft)
    {
        if (detonating && Projectile.owner == Main.myPlayer)
            SpawnExplosion(Math.Max(1, (int)MathF.Round(Projectile.damage * (Projectile.velocity.Y * gravityDirection > 0 ? 1.25f : 1))));
    }
    protected virtual void SpawnExplosion(int damage) => FiammettaExplosion.Spawn(Projectile.GetSource_FromThis(), Projectile.Center,
        damage, Projectile.knockBack, Projectile.owner, FlightMode, Projectile.CritChance);
    public override bool PreDraw(ref Color lightColor)
    {
        if (flight.Staging || Projectile.Opacity < .01f) return false;
        ArtilleryFlightVisuals.Draw(Projectile, TrailOuter * Projectile.Opacity, TrailInner * Projectile.Opacity, TrailWidth);
        Texture2D texture = TextureAssets.Projectile[ProjectileID.RocketI].Value;
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor),
            Projectile.rotation, texture.Size() / 2, Projectile.scale, SpriteEffects.None);
        return false;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(flight.Time); writer.Write(flight.Rain); writer.Write(detonating); writer.Write((short)lockedTargetIndex); writer.Write(gravityDirection);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        bool wasDiving = flight.Diving;
        flight.Time = Math.Clamp(reader.ReadInt32(), 0, 10000); flight.Rain = Math.Clamp(reader.ReadInt32(), 0, 150);
        detonating = reader.ReadBoolean(); lockedTargetIndex = reader.ReadInt16(); gravityDirection = reader.ReadSingle() < 0 ? -1 : 1;
        Projectile.extraUpdates = flight.Time > 120 && flight.Rain <= 1 ? 10 : 2;
        if (wasDiving != flight.Diving || flight.Staging) Array.Fill(Projectile.oldPos, Projectile.position);
    }
}
