using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.BlastCasters;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.BlastCasters;

// A stationary, one-second flame bound to the actual struck tile face.
public sealed class IfritSurfaceFlame : ModProjectile
{
    private readonly Vector2[] positions = new Vector2[3];
    private readonly Vector2[] axes = new Vector2[3];
    private readonly float[] births = new float[3];
    private Vector2 Normal => Projectile.ai[2].ToRotationVector2();
    public override string Texture => "ArknightsMod/Common/Particle/DefaultParticle";

    internal static void Attach(Projectile source, Vector2 contact, Vector2 normal)
    {
        if (source.owner != Main.myPlayer) return;
        Point tile = (contact - normal * 2f).ToTileCoordinates();
        if (!WorldGen.InWorld(tile.X, tile.Y, 1)) return;
        // Repeated spray refreshes the same face instead of stacking damage on it.
        foreach (Projectile patch in Main.ActiveProjectiles)
        {
            if (patch.owner != source.owner || patch.ModProjectile is not IfritSurfaceFlame
                || patch.ai[0] != tile.X || patch.ai[1] != tile.Y
                || Math.Abs(MathHelper.WrapAngle(patch.ai[2] - normal.ToRotation())) > .01f) continue;
            patch.timeLeft = 60;
            patch.damage = Math.Max(1, source.damage / 3);
            patch.CritChance = source.CritChance;
            patch.netUpdate = true;
            return;
        }
        // Bound coverage while sweeping across a long wall.
        if (Main.player[source.owner].ownedProjectileCounts[ModContent.ProjectileType<IfritSurfaceFlame>()] >= 7) return;
        Projectile flame = Projectile.NewProjectileDirect(source.GetSource_FromThis(), contact + normal * 2f,
            Vector2.Zero, ModContent.ProjectileType<IfritSurfaceFlame>(), Math.Max(1, source.damage / 3),
            0f, source.owner, tile.X, tile.Y, normal.ToRotation());
        flame.CritChance = source.CritChance;
    }

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 96;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Ranged;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 60;
        Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 30;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        => Projectile.GetGlobalProjectile<global::ArknightsMod.Systems.Gameplay.Damage.ArtsProjectileMarker>().IsArtsDamage = true;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)Math.Clamp(Projectile.timeLeft, 0, 60));
    public override void ReceiveExtraAI(BinaryReader reader) => Projectile.timeLeft = reader.ReadByte();
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanCutTiles() => false;
    public override void AI()
    {
        int x = (int)Projectile.ai[0], y = (int)Projectile.ai[1];
        if (!WorldGen.InWorld(x, y, 1)) { Projectile.Kill(); return; }
        Tile tile = Framing.GetTileSafely(x, y);
        if (!tile.HasTile || tile.IsActuated || !Main.tileSolid[tile.TileType])
        { Projectile.Kill(); return; }
        Projectile.Opacity = Math.Min(1f, Projectile.timeLeft / 12f);
        Lighting.AddLight(Projectile.Center, new Vector3(.65f, .22f, .04f) * Projectile.Opacity);
    }
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
        return Vector2.DistanceSquared(Projectile.Center, nearest) <= 48f * 48f
            && Collision.CanHitLine(Projectile.Center + Normal * 4f, 1, 1,
                targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        => target.AddBuff(BuffID.OnFire3, 300);
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 normal = Normal, tangent = normal.RotatedBy(MathHelper.PiOver2);
        float clock = (float)(Main.GameUpdateCount % 36000) / 60f;
        for (int i = 0; i < 3; i++)
        {
            positions[i] = Projectile.Center + tangent * ((i - 1) * 13f) + normal * 9f;
            axes[i] = normal;
            // Reuse the flowing flame material while keeping every stamp on this face.
            births[i] = clock - (1f - Projectile.Opacity) * BlastCasterRules.FluxAfterglow * .95f;
        }
        BlastCasterFluxTrail.Draw(positions, axes, births, 3, clock, BlastCasterKind.Ifrit, .48f);
        return false;
    }
}
