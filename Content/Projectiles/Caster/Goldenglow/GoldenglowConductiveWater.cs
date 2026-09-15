using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Goldenglow;

public sealed class GoldenglowConductiveWater : ModProjectile
{
    private readonly HashSet<Point> connected = new();
    private readonly List<Point> surface = new();
    private readonly Queue<Point> pending = new();
    private int updateTimer;
    private bool rebuildRequired = true;
    public override string Texture => "ArknightsMod/Assets/Effects/Goldenglow/LightningImpact";

    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = GoldenglowLightningBalance.WaterLifetime;
        // Only one field per owner is refreshed; different players keep independent damage cooldowns.
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = 12;
        Projectile.netImportant = true;
    }

    public override bool ShouldUpdatePosition() => false;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write((byte)Projectile.timeLeft);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Projectile.timeLeft = Math.Clamp((int)reader.ReadByte(), 1, GoldenglowLightningBalance.WaterLifetime);
        rebuildRequired = true;
    }
    internal static bool IsWater(Vector2 position)
    {
        Point tile = position.ToTileCoordinates();
        if (!WaterTile(tile))
            return false;
        Tile data = Main.tile[tile.X, tile.Y];
        float top = tile.Y * 16f + (255 - data.LiquidAmount) * (16f / 255f);
        return position.Y >= top;
    }

    private static bool WaterTile(Point point)
    {
        if (!WorldGen.InWorld(point.X, point.Y, 1))
            return false;
        Tile tile = Main.tile[point.X, point.Y];
        return tile.LiquidAmount > 0 && tile.LiquidType == LiquidID.Water &&
            !(tile.HasUnactuatedTile && Main.tileSolid[tile.TileType] && !Main.tileSolidTop[tile.TileType]);
    }

    internal static void Energize(Projectile strike, Vector2 water, bool strong)
    {
        if (strike.owner != Main.myPlayer)
            return;
        int damage = Math.Max(1, (int)(strike.damage * 0.45f * GoldenglowLightningBalance.FrequencyMultiplier));
        int type = ModContent.ProjectileType<GoldenglowConductiveWater>();
        foreach (Projectile field in Main.ActiveProjectiles)
        {
            if (field.type != type || field.owner != strike.owner)
                continue;
            field.Center = water;
            // Direct and sky strikes land together. Keep the stronger hit for that refresh only.
            field.damage = field.timeLeft >= GoldenglowLightningBalance.WaterLifetime - 1
                ? Math.Max(field.damage, damage) : damage;
            field.ai[0] = strong ? 1f : 0f;
            field.timeLeft = GoldenglowLightningBalance.WaterLifetime;
            field.netUpdate = true;
            if (field.ModProjectile is GoldenglowConductiveWater conductive)
                conductive.rebuildRequired = true;
            return;
        }
        Projectile.NewProjectile(strike.GetSource_FromThis(), water, Vector2.Zero,
            type, damage, 2f, strike.owner, strong ? 1f : 0f);
    }

    public override void AI()
    {
        if (rebuildRequired || updateTimer % 12 == 0)
        {
            RebuildWater();
            rebuildRequired = false;
        }
        // Refreshing the field must not reset the surface-arc clock during rapid overdrive hits.
        updateTimer++;
        if (connected.Count == 0)
        {
            Projectile.Kill();
            return;
        }
        if (!Main.dedServ && updateTimer % 3 == 0)
        {
            int samples = Projectile.ai[0] > 0f ? 8 : 4;
            for (int i = 0; i < samples && surface.Count > 0; i++)
            {
                Point cell = surface[Main.rand.Next(surface.Count)];
                Vector2 pos = WaterSurface(cell);
                Dust dust = Dust.NewDustPerfect(pos, DustID.Electric,
                    new Vector2(Main.rand.NextFloat(-2f, 2f), Main.rand.NextFloat(-5f, -1f)),
                    80, Color.LightSkyBlue, Projectile.ai[0] > 0f ? 1.15f : 0.8f);
                dust.noGravity = true;
                Lighting.AddLight(pos, 0.2f, 0.5f, 0.85f);
            }
        }
        // Short forked arcs follow the connected water surface. They never deal duplicate damage.
        if (Projectile.owner == Main.myPlayer && updateTimer % 8 == 0 && surface.Count > 1)
        {
            for (int i = 0; i < (Projectile.ai[0] > 0 ? 3 : 2); i++)
            {
                Point from = surface[Main.rand.Next(surface.Count)];
                Point to = surface[Main.rand.Next(surface.Count)];
                if (Vector2.DistanceSquared(from.ToVector2(), to.ToVector2()) > 24f * 24f)
                    continue;
                GoldenglowLightningStrike.Spawn(Projectile.GetSource_FromThis(), WaterSurface(from),
                    WaterSurface(to), Projectile.owner, 0, 0f, 4);
            }
        }
    }

    private static Vector2 WaterSurface(Point cell) => new(cell.X * 16f + 8f,
        cell.Y * 16f + (255 - Main.tile[cell.X, cell.Y].LiquidAmount) * (16f / 255f));

    private void RebuildWater()
    {
        connected.Clear();
        surface.Clear();
        pending.Clear();
        Point start = Projectile.Center.ToTileCoordinates();
        if (!WaterTile(start))
            return;
        connected.Add(start);
        pending.Enqueue(start);
        while (pending.Count > 0 && connected.Count < 4096)
        {
            Point point = pending.Dequeue();
            Tile tile = Main.tile[point.X, point.Y];
            if (tile.LiquidAmount < 255 || !WaterTile(new Point(point.X, point.Y - 1)))
                surface.Add(point);
            Visit(new Point(point.X - 1, point.Y));
            Visit(new Point(point.X + 1, point.Y));
            Visit(new Point(point.X, point.Y - 1));
            Visit(new Point(point.X, point.Y + 1));
        }
    }

    private void Visit(Point cell)
    {
        if (connected.Contains(cell) || !WaterTile(cell) || Vector2.DistanceSquared(
            cell.ToWorldCoordinates(), Projectile.Center) > GoldenglowLightningBalance.WaterRadius * GoldenglowLightningBalance.WaterRadius)
            return;
        connected.Add(cell);
        pending.Enqueue(cell);
    }

    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        Point min = targetHitbox.TopLeft().ToTileCoordinates();
        Point max = targetHitbox.BottomRight().ToTileCoordinates();
        // Iterate the overlapped water cells, so large NPCs can be shocked when only their feet are wet.
        for (int x = min.X; x <= max.X; x++)
            for (int y = min.Y; y <= max.Y; y++)
                if (connected.Contains(new Point(x, y)) && WaterTile(new Point(x, y)) &&
                    targetHitbox.Bottom > WaterSurface(new Point(x, y)).Y)
                    return true;
        return false;
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Electrified, 180);
    public override bool PreDraw(ref Color lightColor)
    {
        float opacity = Math.Min(1f, Projectile.timeLeft / 12f);
        for (int i = 0; i < surface.Count; i += 6)
            GoldenglowLightningRenderer.DrawFlare(WaterSurface(surface[i]), new Color(60, 150, 255),
                0.15f, opacity * 0.4f);
        return false;
    }
}
