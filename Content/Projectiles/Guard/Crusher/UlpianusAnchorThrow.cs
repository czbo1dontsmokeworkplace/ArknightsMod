using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Crusher;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Crusher;

// The normal attack is the weapon itself, not a free projectile added to a melee swing.
public sealed class UlpianusAnchorThrow : ModProjectile
{
    public override string Texture => CrusherRules.Art(CrusherKind.Ulpianus);
    private readonly UlpianusThrowFlight flight = new();
    private readonly Vector2[] history = new Vector2[24];
    private readonly bool[] applied = new bool[Main.maxNPCs];
    private readonly bool[] exploded = new bool[Main.maxNPCs];
    private int samples, hits, skillMode, skillLevel = 1;
    private bool pursuitReleased, terrainBurst, terrainSpent;
    private float Charge => Math.Clamp(Projectile.ai[0], 0, 1);
    private int Special => (int)Projectile.ai[2];
    internal void CaptureSkill(int mode, int level) { skillMode = mode; skillLevel = level; Projectile.netUpdate = true; }
    internal static bool Exists(Player owner)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner.whoAmI && p.ModProjectile is UlpianusAnchorThrow) return true;
        return false;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = CrusherRules.AnchorContactSize;
        Projectile.DamageType = DamageClass.Melee; Projectile.friendly = true;
        Projectile.ignoreWater = true; Projectile.tileCollide = true; Projectile.penetrate = -1;
        Projectile.extraUpdates = 2; Projectile.timeLeft = 450; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = -1;
    }
    public override void OnSpawn(IEntitySource source) => CrusherAnchorVisuals.Launch(Projectile);
    public override bool CanHitPvp(Player target) => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.ModItem is not UlpianusAnchor || owner.selectedItem != (int)Projectile.ai[1])
        { Projectile.Kill(); return; }
        bool returning = flight.Returning;
        flight.Step(Projectile.Center, owner.MountedCenter, ref Projectile.velocity, Projectile.MaxUpdates, Charge);
        Projectile.tileCollide = !flight.Returning;
        if (returning != flight.Returning && Projectile.owner == Main.myPlayer) Projectile.netUpdate = true;
        if (flight.Returning && Vector2.DistanceSquared(Projectile.Center, owner.MountedCenter) < 36 * 36)
        { Projectile.Kill(); return; }
        Projectile.rotation = Projectile.velocity.ToRotation();
        for (int i = Math.Min(samples, history.Length - 1); i > 0; i--) history[i] = history[i - 1];
        history[0] = Projectile.Center; samples = Math.Min(samples + 1, history.Length);
        if (Projectile.numUpdates == 0 && (int)flight.Age % 3 == 0) CrusherAnchorVisuals.Flight(Projectile);
    }
    public override bool? Colliding(Rectangle projectileBox, Rectangle targetBox)
    {
        if (terrainBurst)
            return Vector2.DistanceSquared(targetBox.ClosestPointInRect(Projectile.Center), Projectile.Center) <= 150 * 150
                && Collision.CanHitLine(Projectile.Center, 1, 1, targetBox.TopLeft(), targetBox.Width, targetBox.Height);
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetBox.TopLeft(), targetBox.Size(), Projectile.Center - Projectile.velocity,
            Projectile.Center + Projectile.velocity, CrusherRules.AnchorContactSize, ref distance);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.HitDirectionOverride = target.Center.X >= Main.player[Projectile.owner].Center.X ? 1 : -1;
        if (terrainBurst) { modifiers.SourceDamage *= .6f; modifiers.Knockback *= 1.5f; }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (terrainBurst) return; // The ground shock is not physical contact for the charged pursuit.
        bool first = hits++ == 0;
        Main.player[Projectile.owner].GetModPlayer<CrusherPlayer>().Hit(target, first, Special, skillMode);
        CrusherCombatNet.Hit(Projectile, target);
        if (Projectile.owner != Main.myPlayer) return;
        if (!exploded[target.whoAmI])
        {
            exploded[target.whoAmI] = true;
            Vector2 point = target.Hitbox.ClosestPointInRect(Projectile.Center);
            int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, Vector2.Zero,
                ModContent.ProjectileType<UlpianusAnchorExplosion>(), Projectile.damage, Projectile.knockBack,
                Projectile.owner);
            if ((uint)index < Main.maxProjectiles)
            {
                Main.projectile[index].CritChance = Projectile.CritChance;
                Main.projectile[index].netUpdate = true;
            }
        }
        if (Charge >= 1 && !pursuitReleased)
        {
            pursuitReleased = true;
            CrusherUpheaval.Release(Projectile, target, CrusherKind.Ulpianus, Special, skillLevel);
            Projectile.netUpdate = true;
        }
    }
    internal void ApplyHitEffects(NPC target)
    {
        if (applied[target.whoAmI]) return;
        applied[target.whoAmI] = true;
        Main.player[Projectile.owner].GetModPlayer<CrusherPlayer>().ApplyHitEffects(target, skillMode, skillLevel);
    }
    private void Feedback(Vector2 point, float strength)
    {
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), point, Projectile.velocity.SafeNormalize(Vector2.UnitY),
            ModContent.ProjectileType<CrusherAnchorFeedback>(), 0, 0, Projectile.owner, strength);
    }
    public override bool OnTileCollide(Vector2 oldVelocity)
    {
        if (!terrainSpent && flight.Age >= 10 && Projectile.owner == Main.myPlayer)
        {
            terrainSpent = true;
            Feedback(Projectile.Center, MathHelper.Lerp(.75f, 1.5f, Charge) * 2);
            // Reuse this projectile's one-hit-per-NPC immunity for the one-frame 60% ground shock.
            // Direct hits therefore cannot also receive a second splash hit from the same throw.
            Vector2 center = Projectile.Center; int width = Projectile.width, height = Projectile.height;
            terrainBurst = true; Projectile.Resize(300, 300); Projectile.Center = center;
            Projectile.Damage();
            Projectile.Resize(width, height); Projectile.Center = center; terrainBurst = false;
        }
        Projectile.velocity = oldVelocity;
        flight.Recall(); Projectile.tileCollide = false; Projectile.netUpdate = true;
        return false;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        CrusherAnchorVisuals.DrawThrowTether(Projectile);
        CrusherAnchorVisuals.DrawFlight(Projectile, history, samples, false);
        return false;
    }
    public override void SendExtraAI(BinaryWriter w)
    {
        w.Write(skillMode); w.Write(skillLevel); w.Write(pursuitReleased); w.Write(terrainSpent);
        w.Write(flight.Age); w.Write(flight.Returning);
    }
    public override void ReceiveExtraAI(BinaryReader r)
    {
        skillMode = Math.Clamp(r.ReadInt32(), 0, 3); skillLevel = Math.Clamp(r.ReadInt32(), 1, 10);
        pursuitReleased = r.ReadBoolean(); terrainSpent = r.ReadBoolean(); flight.Restore(r.ReadSingle(), r.ReadBoolean());
        Projectile.tileCollide = !flight.Returning;
    }
}
