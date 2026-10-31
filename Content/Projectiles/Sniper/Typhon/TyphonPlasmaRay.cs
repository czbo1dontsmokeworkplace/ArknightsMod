using ArknightsMod.Content.Items.Weapons.Sniper.Typhon;
using ArknightsMod.Content.Projectiles.Guard.Reaper.Phoenix;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Typhon;

public sealed class TyphonPlasmaRay : ModProjectile
{
    internal const float Range = 1200, Width = 6;
    private readonly float[] scan = new float[3];
    private PhoenixBeamFx beam;
    private ReaperSwarmVisualNode muzzle, contact;
    private Vector2 end;
    private bool blocked;
    private Vector2 Axis => Projectile.velocity.SafeNormalize(Vector2.UnitX);
    public override string Texture => "Terraria/Images/Projectile_0";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 6;
        Projectile.friendly = true; Projectile.DamageType = DamageClass.Ranged;
        Projectile.penetrate = -1; Projectile.timeLeft = TyphonMagazine.BeamFrames;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 8;
        Projectile.netImportant = true;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || owner.HeldItem.ModItem is not TyphonBow)
        { Projectile.Kill(); return; }
        Vector2 axis = Axis;
        Projectile.Center = owner.MountedCenter + axis * 48 + new Vector2(-axis.Y, axis.X) * (Projectile.ai[0] * 11.3f);
        Projectile.rotation = axis.ToRotation();
        Trace();
        if (Main.dedServ) return;
        if (beam == null)
        {
            var style = PhoenixFx.Original();
            beam = new PhoenixBeamFx("projectile.player.75", () => Projectile.Center, () => end,
                () => Projectile.active, Range, style);
            muzzle = ReaperSwarmVisuals.Spawn("actor.player.weapon.75.muzzle", () => Projectile.Center,
                () => Axis, active: () => Projectile.active, style: style);
        }
        beam.Extend();
        if (blocked && contact == null)
            contact = ReaperSwarmVisuals.Spawn("actor.player.weapon.75.impact", () => end, () => -Axis,
                active: () => Projectile.active && blocked, style: PhoenixFx.Original());
        else if (!blocked) { contact?.Stop(); contact = null; }
    }
    private void Trace()
    {
        Vector2 axis = Axis;
        Collision.LaserScan(Projectile.Center, axis, Width, Range, scan);
        float length = (scan[0] + scan[1] + scan[2]) / 3;
        blocked = length < Range - 1;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy(Projectile)) continue;
            float distance = 0;
            if (Collision.CheckAABBvLineCollision(npc.position, npc.Size, Projectile.Center,
                Projectile.Center + axis * length, Width, ref distance) && distance < length)
            { length = System.Math.Max(0, distance); blocked = true; }
        }
        end = Projectile.Center + axis * length;
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        if (new Rectangle((int)end.X - 3, (int)end.Y - 3, 6, 6).Intersects(targetHitbox)) return true;
        float distance = 0;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, end, Width, ref distance);
    }
    public override void OnKill(int timeLeft) { beam?.Stop(); muzzle?.Stop(); contact?.Stop(); }
    public override bool PreDraw(ref Color lightColor) => false;
}
