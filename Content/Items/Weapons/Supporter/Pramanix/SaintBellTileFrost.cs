using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Drawing;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Supporter.Pramanix;

// 冰雪仅是客户端外观。不改动世界方块数据，也不参与寒域伤害计算。
public sealed class SaintBellTileFrost : ModSystem
{
    private const int StepTicks = 5;
    private const int FrostArrivalDelay = 36;
    private const int TravelTicksPerTile = 48;
    private const int LayerInterval = 360;
    private const int MeltLayerTicks = 720;
    private const int MaxCells = 8192;
    private const int MaxSnow = 80;
    private const int SnowRecovery = 6;
    private const int LayerCost = 2;
    private const int StyleCount = 4;
    private const int SnowflakeStyle = StyleCount;
    private const int CellSize = 16;
    // 两格以内的轻微走动不重开霜域，避免原地摆动反复重置传播。
    private const float WaveRelocationDistance = 2f * CellSize;
    private static readonly Point[] Offsets = BuildOffsets();

    private readonly Dictionary<Point, Frost> cells = new();
    private readonly List<Point> expired = new();
    private readonly List<DomainDraw> visibleDomains = new();
    private readonly byte[] sourceStates = new byte[Main.maxPlayers];
    private readonly ulong[] sourceSeen = new ulong[Main.maxPlayers];
    private readonly int[] snowValues = new int[Main.maxPlayers];
    private readonly FrostWave[] waves = new FrostWave[Main.maxPlayers];
    private readonly Dictionary<MaskKey, MaskRun[]> masks = new();
    private ReLogic.Content.Asset<Texture2D> atlas;
    private ulong nextWaveId;

    private sealed class FrostWave
    {
        public ulong Id;
        public Vector2 Anchor;
        public readonly Dictionary<Point, Arrival> Pending = new();
    }

    private readonly record struct Frost(ushort Type, byte Tier, ulong LastCovered, ulong LastLayer, ulong WaveId);
    private readonly record struct Arrival(ulong Due, ulong LastSeen);
    private readonly record struct DomainDraw(Vector2 Center, float Radius, byte Tier);
    private readonly record struct MaskRun(byte X, byte Y, byte Width, byte Height, byte Alpha);
    private readonly record struct MaskKey(Texture2D Texture, Rectangle Frame, SlopeType Slope, bool Half, SpriteEffects Effect);

    public override void Load()
    {
        if (!Main.dedServ)
            atlas = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Supporter/Pramanix/SaintBellTerrainFrost");
    }

    public override void OnWorldLoad() => Clear();
    public override void OnWorldUnload() => Clear();

    private void Clear()
    {
        cells.Clear();
        expired.Clear();
        visibleDomains.Clear();
        masks.Clear();
        Array.Clear(sourceStates);
        Array.Clear(sourceSeen);
        Array.Clear(waves);
        nextWaveId = 0;
        Array.Fill(snowValues, MaxSnow);
    }

    private static Point[] BuildOffsets()
    {
        int radius = (int)MathF.Ceiling(FrostDomainLogic.GetRadius(FrostDomainLogic.TierMax, true) / CellSize) + 1;
        var points = new List<Point>();
        for (int y = -radius; y <= radius; y++)
        for (int x = -radius; x <= radius; x++)
            points.Add(new Point(x, y));
        points.Sort((a, b) => (a.X * a.X + a.Y * a.Y).CompareTo(b.X * b.X + b.Y * b.Y));
        return points.ToArray();
    }

    private static byte GetSourceState(Player player)
    {
        if (!player.active || player.dead || player.HeldItem.ModItem is not SaintBell)
            return 0;
        var state = player.GetModPlayer<SaintBellPlayer>();
        return (byte)(Math.Clamp(state.FrostTier, 1, FrostDomainLogic.TierMax) | (state.Skill3Active ? 4 : 0));
    }

    // 天地两种寒域表现共用同一个等级、中心和最大半径；技能三扩大半径也一并同步。
    internal static bool TryGetVisualDomain(Player player, out byte tier, out float radius)
    {
        var system = ModContent.GetInstance<SaintBellTileFrost>();
        int index = player.whoAmI;
        byte state = !player.active || player.dead ? (byte)0
            : Main.netMode != NetmodeID.MultiplayerClient ? GetSourceState(player)
            : (uint)index < system.sourceStates.Length && Main.GameUpdateCount - system.sourceSeen[index] <= 180
                ? system.sourceStates[index] : (byte)0;
        tier = (byte)(state & 3);
        radius = FrostDomainLogic.GetRadius(tier, (state & 4) != 0);
        return tier > 0;
    }

    public override void PostUpdateWorld()
    {
        ulong now = Main.GameUpdateCount;
        if (now % StepTicks != 0)
            return;
        if (Main.netMode == NetmodeID.Server) {
            SyncSources(now);
            return;
        }

        expired.Clear();
        foreach (var (p, frost) in cells)
            if (VisibleTier(frost, now) == 0 || !Matches(p, frost))
                expired.Add(p);
        foreach (Point p in expired)
            cells.Remove(p);

        for (int i = 0; i < sourceStates.Length; i++) {
            Player player = Main.player[i];
            snowValues[i] = Math.Min(MaxSnow, snowValues[i] + SnowRecovery);
            if (!TryGetVisualDomain(player, out byte tier, out float radius)) {
                waves[i] = null;
                continue;
            }

            FrostWave wave = waves[i] ??= new();
            if (wave.Id == 0 || Vector2.DistanceSquared(player.Center, wave.Anchor) >= WaveRelocationDistance * WaveRelocationDistance) {
                wave.Id = ++nextWaveId;
                wave.Anchor = player.Center;
                wave.Pending.Clear();
            }
            Spread(player.Center, tier, radius, now, ref snowValues[i], wave);
        }
    }

    // 服务端只同步持有状态与强度，霜层和冰雪值在观看端模拟，无需为每块地块发包。
    private void SyncSources(ulong now)
    {
        for (int i = 0; i < sourceStates.Length; i++) {
            byte state = GetSourceState(Main.player[i]);
            if (state == sourceStates[i] && (state == 0 || now % 120 != 0))
                continue;
            sourceStates[i] = state;
            var packet = Mod.GetPacket();
            packet.Write((short)ArknightsMod.ArkMessageID.SaintBellFrostSource);
            packet.Write((byte)i);
            packet.Write(state);
            packet.Send();
        }
    }

    internal static void ReceiveSource(BinaryReader reader)
    {
        int i = reader.ReadByte();
        byte state = reader.ReadByte();
        if (i >= Main.maxPlayers || state > 7 || state == 4)
            return;
        var system = ModContent.GetInstance<SaintBellTileFrost>();
        system.sourceStates[i] = state;
        system.sourceSeen[i] = Main.GameUpdateCount;
    }

    private static byte VisibleTier(Frost frost, ulong now)
    {
        ulong lost = (now - frost.LastCovered) / MeltLayerTicks;
        return lost >= frost.Tier ? (byte)0 : (byte)(frost.Tier - lost);
    }

    private void Spread(Vector2 center, byte tier, float radius, ulong now, ref int snow, FrostWave wave)
    {
        var pending = wave.Pending;
        Point origin = center.ToTileCoordinates();
        int maxOffset = (int)MathF.Ceiling(radius / CellSize) + 1;
        foreach (Point offset in Offsets) {
            if (offset.X * offset.X + offset.Y * offset.Y > maxOffset * maxOffset)
                break;
            Point p = origin + offset;
            float distanceSquared = Vector2.DistanceSquared(center, p.ToWorldCoordinates());
            if (distanceSquared > radius * radius || !WorldGen.InWorld(p.X, p.Y, 1))
                continue;
            Tile tile = Main.tile[p.X, p.Y];
            if (!Eligible(tile))
                continue;
            if (cells.TryGetValue(p, out Frost frost)) {
                pending.Remove(p);
                // 圆形范围可能重叠，但新一轮（或其他玩家）不能为旧一轮的冰雪续期或升阶。
                if (frost.WaveId != wave.Id)
                    continue;
                byte remaining = VisibleTier(frost, now);
                if (remaining != frost.Tier)
                    frost = frost with { Tier = remaining, LastLayer = now };
                if (snow >= LayerCost && frost.Tier < tier && now - frost.LastLayer >= LayerInterval) {
                    snow -= LayerCost;
                    frost = frost with { Tier = (byte)(frost.Tier + 1), LastLayer = now };
                }
                cells[p] = frost with { LastCovered = now };
            }
            else {
                // 每格等霜线走完距离才显现；没有每批铺设数量限制。同一距离的多个方块可以一起结霜。
                if (!pending.TryGetValue(p, out Arrival arrival)) {
                    int travel = (int)MathF.Ceiling(MathF.Sqrt(distanceSquared) * (TravelTicksPerTile / (float)CellSize));
                    arrival = new Arrival(now + (ulong)(FrostArrivalDelay + travel), now);
                }
                if (now >= arrival.Due && snow >= LayerCost && cells.Count < MaxCells) {
                    snow -= LayerCost;
                    pending.Remove(p);
                    cells.Add(p, new Frost(tile.TileType, 1, now, now, wave.Id));
                }
                else
                    pending[p] = arrival with { LastSeen = now };
            }
        }
        // 玩家已离开范围的待结霜格不继续计时，下次进入时重新从霜线开始。
        expired.Clear();
        foreach (var (p, arrival) in pending)
            if (arrival.LastSeen != now)
                expired.Add(p);
        foreach (Point p in expired)
            pending.Remove(p);
    }

    private static bool Eligible(Tile tile) => tile.HasUnactuatedTile
        && (Main.tileSolid[tile.TileType] || Main.tileSolidTop[tile.TileType] || TileID.Sets.Platforms[tile.TileType]);

    private static bool Matches(Point p, Frost frost)
    {
        Tile tile = Main.tile[p.X, p.Y];
        return Eligible(tile) && tile.TileType == frost.Type;
    }

    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu)
            return;
        ulong now = Main.GameUpdateCount;
        Vector2 size = new(Main.screenWidth, Main.screenHeight);
        Vector2 half = size / (Main.GameViewMatrix.Zoom * 2f);
        Vector2 viewCenter = Main.screenPosition + size / 2f;
        visibleDomains.Clear();
        foreach (Player player in Main.ActivePlayers) {
            if (!TryGetVisualDomain(player, out byte tier, out float radius))
                continue;
            Vector2 distance = player.Center - viewCenter;
            if (Math.Abs(distance.X) <= half.X + radius + 16f && Math.Abs(distance.Y) <= half.Y + radius + 16f)
                visibleDomains.Add(new DomainDraw(player.Center - Main.screenPosition, radius, tier));
        }
        if (cells.Count == 0 && visibleDomains.Count == 0)
            return;

        var batch = Main.spriteBatch;
        batch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
            DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        Texture2D frostTexture = atlas.Value;
        foreach (var (p, frost) in cells) {
            Vector2 world = p.ToWorldCoordinates(0, 0);
            Vector2 delta = world - viewCenter;
            if (Math.Abs(delta.X) > half.X + 48f || Math.Abs(delta.Y) > half.Y + 48f || !Matches(p, frost))
                continue;
            Tile tile = Main.tile[p.X, p.Y];
            if (!TileDrawing.IsVisible(tile))
                continue;
            byte tier = VisibleTier(frost, now);
            if (tier == 0 || !TryGetMask(p, tile, out MaskRun[] mask, out int top))
                continue;
            int style = GetMaterialStyle(p, tier);
            int frameX = style * CellSize;
            int frameY = (tier - 1) * CellSize;
            Vector2 pos = world - Main.screenPosition + new Vector2(0, top);
            Color tint = Lighting.GetColor(p.X, p.Y) * MaterialCoverage(tier);
            foreach (MaskRun run in mask)
                batch.Draw(frostTexture, pos + new Vector2(run.X, run.Y),
                    new Rectangle(frameX + run.X, frameY + run.Y, run.Width, run.Height), tint * (run.Alpha / 255f));
        }
        // 范围特效独立于已结霜方块，空中或霜线尚未到达时也照常显示。
        foreach (var domain in visibleDomains)
            PramanixFrostDomainVisuals.Draw(batch, domain.Center, domain.Tier, domain.Radius);
        batch.End();
    }

    private static int GetMaterialStyle(Point p, byte tier)
    {
        uint hash = unchecked((uint)p.X * 0x9e3779b1u ^ (uint)p.Y * 0x85ebca77u);
        hash ^= hash >> 16;
        hash = unchecked(hash * 0x7feb352du);
        hash ^= hash >> 15;
        // 少量三级冰面夹有小雪花，按坐标固定，移动镜头或重新结霜时不会闪变。
        return tier == 3 && (hash & 31u) == 0 ? SnowflakeStyle : (int)(hash >> 5) & (StyleCount - 1);
    }

    // 固定的预乘 Alpha 材质混合，透出石纹、砖缝等底材细节；只随等级切换，不随时间渐隐。
    private static float MaterialCoverage(byte tier) => tier switch {
        1 => 0.42f, // 霜晶本身还有镂空，不把整格染蓝。
        2 => 0.46f,
        3 => 0.54f, // 稍厚但仍能看到冰下的方块纹理。
        _ => 0f
    };

    // 使用实际绘制帧的 Alpha：木平台的空隙等透明区域完全跳过。
    // 半砖与斜坡另外和实体轮廓求交，透明像素不会被画成一个完整矩形。
    private bool TryGetMask(Point p, Tile tile, out MaskRun[] mask, out int top)
    {
        short fx = tile.TileFrameX, fy = tile.TileFrameY;
        Main.instance.TilesRenderer.GetTileDrawData(p.X, p.Y, tile, tile.TileType, ref fx, ref fy,
            out int width, out int height, out top, out _, out int ax, out int ay, out SpriteEffects effect, out _, out _, out _);
        Texture2D texture = Main.instance.TilesRenderer.GetTileDrawTexture(tile, p.X, p.Y, 0);
        var frame = new Rectangle(fx + ax, fy + ay, Math.Min(width, CellSize), Math.Min(height, CellSize));
        mask = null;
        if (texture == null || texture.Format != SurfaceFormat.Color || frame.Width <= 0 || frame.Height <= 0
            || frame.X < 0 || frame.Y < 0 || frame.Right > texture.Width || frame.Bottom > texture.Height)
            return false;
        bool halfBlock = tile.IsHalfBlock;
        SlopeType slope = tile.Slope;
        var key = new MaskKey(texture, frame, slope, halfBlock, effect);
        if (masks.TryGetValue(key, out mask))
            return true;
        var pixels = new Color[frame.Width * frame.Height];
        texture.GetData(0, frame, pixels, 0, pixels.Length);
        if (masks.Count >= 2048)
            masks.Clear();
        if (!halfBlock && slope == SlopeType.Solid && Array.TrueForAll(pixels, pixel => pixel.A == 255)) {
            mask = [new MaskRun(0, 0, (byte)frame.Width, (byte)frame.Height, 255)];
            masks.Add(key, mask);
            return true;
        }
        var runs = new List<MaskRun>();
        bool flipX = (effect & SpriteEffects.FlipHorizontally) != 0;
        bool flipY = (effect & SpriteEffects.FlipVertically) != 0;
        int targetHeight = halfBlock ? Math.Min(frame.Height, 8) : frame.Height;
        for (int y = 0; y < targetHeight; y++) {
            int x = 0;
            while (x < frame.Width) {
                int start = x;
                byte alpha = PixelAlpha(x++, y);
                while (x < frame.Width && PixelAlpha(x, y) == alpha)
                    x++;
                if (alpha != 0)
                    runs.Add(new MaskRun((byte)start, (byte)(y + (halfBlock ? 8 : 0)), (byte)(x - start), 1, alpha));
            }
        }
        mask = runs.ToArray();
        masks.Add(key, mask);
        return true;

        byte PixelAlpha(int x, int y)
        {
            bool inside = slope switch {
                SlopeType.SlopeDownRight => y >= x,
                SlopeType.SlopeDownLeft => y >= 15 - x,
                SlopeType.SlopeUpRight => y <= 15 - x,
                SlopeType.SlopeUpLeft => y <= x,
                _ => true
            };
            int sx = flipX ? frame.Width - 1 - x : x;
            int sy = flipY ? frame.Height - 1 - y : y;
            return inside ? pixels[sy * frame.Width + sx].A : (byte)0;
        }
    }
}
