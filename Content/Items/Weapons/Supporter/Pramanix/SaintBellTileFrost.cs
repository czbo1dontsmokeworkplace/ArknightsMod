using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent;
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
    private const byte FrostAbove = 1, FrostBelow = 2, FrostLeft = 4, FrostRight = 8;
    private const byte FrostAllSides = FrostAbove | FrostBelow | FrostLeft | FrostRight;
    // 第一层保证泥土等高对比底纹上看得见冰裂纹；后两层轻叠，避免高级材质发白。
    private static readonly float[] IceLayerOpacity = [0.32f, 0.14f, 0.12f];
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
    private readonly Dictionary<OutlineKey, Rectangle[]> outlines = new();
    private readonly Dictionary<Point, byte> neighborCache = new();
    private ReLogic.Content.Asset<Texture2D> atlas;
    private ReLogic.Content.Asset<Texture2D> iceFill;
    private readonly Point[] icePhase = new Point[FrostDomainLogic.TierMax];
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
    private readonly record struct OutlineKey(MaskRun[] Mask, byte Neighbors);

    public override void Load()
    {
        if (!Main.dedServ) {
            atlas = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Supporter/Pramanix/SaintBellTerrainFrost");
            iceFill = ModContent.Request<Texture2D>("ArknightsMod/Content/Items/Weapons/Supporter/Pramanix/SaintBellTerrainIceFill");
        }
    }

    public override void OnWorldLoad()
    {
        Clear();
        var random = new Random(Main.worldID);
        for (int i = 0; i < icePhase.Length; i++)
            icePhase[i] = new Point(random.Next(24), random.Next(24));
    }
    public override void OnWorldUnload() => Clear();

    private void Clear()
    {
        cells.Clear();
        expired.Clear();
        visibleDomains.Clear();
        masks.Clear();
        outlines.Clear();
        neighborCache.Clear();
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
        Texture2D iceTexture = iceFill.Value;
        neighborCache.Clear();
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
            Color light = Lighting.GetColor(p.X, p.Y);
            byte neighbors = GetFrostNeighbors(p, now);
            if (neighbors != FrostAllSides) {
                foreach (MaskRun run in mask)
                    DrawOldMaterial(batch, frostTexture, pos, frameX, frameY, run, light * MaterialCoverage(tier));
                Color edge = light * 0.80f;
                foreach (Rectangle run in GetOutline(mask, neighbors))
                    batch.Draw(TextureAssets.MagicPixel.Value,
                        new Rectangle((int)pos.X + run.X, (int)pos.Y + run.Y, run.Width, 1),
                        new Rectangle(0, 0, 1, 1), edge);
                continue;
            }

            byte transition = GetTransitionSides(p, now);
            foreach (MaskRun run in mask) {
                if (transition == 0)
                    DrawIceLayers(batch, iceTexture, p, pos, run, tier, light, 1f);
                else
                    DrawTransition(batch, frostTexture, iceTexture, p, pos, frameX, frameY, run, tier, light, transition);
            }
        }
        // 范围特效独立于已结霜方块，空中或霜线尚未到达时也照常显示。
        foreach (var domain in visibleDomains)
            PramanixFrostDomainVisuals.Draw(batch, domain.Center, domain.Tier, domain.Radius);
        batch.End();
    }

    private byte GetFrostNeighbors(Point p, ulong now)
    {
        if (neighborCache.TryGetValue(p, out byte cached))
            return cached;
        byte mask = 0;
        if (HasVisibleFrost(new Point(p.X, p.Y - 1), now)) mask |= FrostAbove;
        if (HasVisibleFrost(new Point(p.X, p.Y + 1), now)) mask |= FrostBelow;
        if (HasVisibleFrost(new Point(p.X - 1, p.Y), now)) mask |= FrostLeft;
        if (HasVisibleFrost(new Point(p.X + 1, p.Y), now)) mask |= FrostRight;
        neighborCache.Add(p, mask);
        return mask;
    }

    // 当前格处于内部，若某一侧邻格已是外圈，则当前格从该侧向里过渡。
    private byte GetTransitionSides(Point p, ulong now)
    {
        byte mask = 0;
        if (GetFrostNeighbors(new Point(p.X, p.Y - 1), now) != FrostAllSides) mask |= FrostAbove;
        if (GetFrostNeighbors(new Point(p.X, p.Y + 1), now) != FrostAllSides) mask |= FrostBelow;
        if (GetFrostNeighbors(new Point(p.X - 1, p.Y), now) != FrostAllSides) mask |= FrostLeft;
        if (GetFrostNeighbors(new Point(p.X + 1, p.Y), now) != FrostAllSides) mask |= FrostRight;
        return mask;
    }

    private static void DrawOldMaterial(SpriteBatch batch, Texture2D texture, Vector2 pos,
        int frameX, int frameY, MaskRun run, Color tint)
        => batch.Draw(texture, pos + new Vector2(run.X, run.Y),
            new Rectangle(frameX + run.X, frameY + run.Y, run.Width, run.Height), tint * (run.Alpha / 255f));

    private void DrawIceLayers(SpriteBatch batch, Texture2D texture, Point tile, Vector2 pos,
        MaskRun run, byte tier, Color light, float blend)
    {
        float coverage = blend * (run.Alpha / 255f);
        int cols = texture.Width / CellSize, rows = texture.Height / CellSize;
        for (int layer = 0; layer < tier; layer++) {
            Point phase = icePhase[layer];
            // 世界坐标连续采样；偏移对齐整格，所以相邻方块读取相邻像素且无需缩放。
            int x = ((tile.X + phase.X) % cols) * CellSize + run.X;
            int y = ((tile.Y + phase.Y) % rows) * CellSize + run.Y;
            batch.Draw(texture, pos + new Vector2(run.X, run.Y),
                new Rectangle(x, y, run.Width, run.Height), light * (IceLayerOpacity[layer] * coverage));
        }
    }

    private void DrawTransition(SpriteBatch batch, Texture2D edgeTexture, Texture2D iceTexture, Point tile,
        Vector2 pos, int frameX, int frameY, MaskRun run, byte tier, Color light, byte sides)
    {
        bool alongX = (sides & (FrostLeft | FrostRight)) != 0;
        bool alongY = (sides & (FrostAbove | FrostBelow)) != 0;
        int right = run.X + run.Width, bottom = run.Y + run.Height;
        for (int y = run.Y; y < bottom;) {
            int nextY = alongY ? Math.Min(bottom, (y | 1) + 1) : bottom;
            for (int x = run.X; x < right;) {
                int nextX = alongX ? Math.Min(right, (x | 1) + 1) : right;
                float blend = GetIceBlend(x, y, sides);
                var patch = new MaskRun((byte)x, (byte)y, (byte)(nextX - x), (byte)(nextY - y), run.Alpha);
                if (blend < 1f)
                    DrawOldMaterial(batch, edgeTexture, pos, frameX, frameY, patch, light * (MaterialCoverage(tier) * (1f - blend)));
                if (blend > 0f)
                    DrawIceLayers(batch, iceTexture, tile, pos, patch, tier, light, blend);
                x = nextX;
            }
            y = nextY;
        }
    }

    private static float GetIceBlend(int x, int y, byte sides)
    {
        int band = 7;
        if ((sides & FrostAbove) != 0) band = Math.Min(band, y / 2);
        if ((sides & FrostBelow) != 0) band = Math.Min(band, (15 - y) / 2);
        if ((sides & FrostLeft) != 0) band = Math.Min(band, x / 2);
        if ((sides & FrostRight) != 0) band = Math.Min(band, (15 - x) / 2);
        return band / 7f;
    }

    private bool HasVisibleFrost(Point p, ulong now) => cells.TryGetValue(p, out Frost frost)
        && VisibleTier(frost, now) > 0 && Matches(p, frost) && TileDrawing.IsVisible(Main.tile[p.X, p.Y]);

    private Rectangle[] GetOutline(MaskRun[] mask, byte neighbors)
    {
        var key = new OutlineKey(mask, neighbors);
        if (outlines.TryGetValue(key, out Rectangle[] outline))
            return outline;

        // 描边只看原方块的形状。即使雪纹在方块边缘透明，轮廓也保持连续。
        Span<ushort> rows = stackalloc ushort[CellSize];
        rows.Clear();
        foreach (MaskRun run in mask) {
            ushort span = (ushort)(((1 << run.Width) - 1) << run.X);
            for (int y = run.Y; y < run.Y + run.Height; y++)
                rows[y] |= span;
        }
        Span<ushort> outside = stackalloc ushort[CellSize];
        FindExterior(rows, outside);
        var runs = new List<Rectangle>();
        for (int y = 0; y < CellSize; y++) {
            int border = OutlineRow(rows, outside, y, neighbors);
            for (int x = 0; x < CellSize; x++) {
                if ((border & (1 << x)) == 0)
                    continue;
                int start = x;
                while (x + 1 < CellSize && (border & (1 << (x + 1))) != 0)
                    x++;
                runs.Add(new Rectangle(start, y, x - start + 1, 1));
            }
        }
        outline = runs.ToArray();
        if (outlines.Count >= 4096)
            outlines.Clear();
        outlines.Add(key, outline);
        return outline;
    }

    private static void FindExterior(ReadOnlySpan<ushort> rows, Span<ushort> outside)
    {
        // 只认与格子外相连的空白；方块贴图自身封闭的镂空也不描边。
        const int sidePixels = 1 | (1 << (CellSize - 1));
        for (int y = 0; y < CellSize; y++)
            outside[y] = (ushort)(~rows[y] & (y == 0 || y == CellSize - 1 ? ushort.MaxValue : sidePixels));

        bool changed;
        do {
            changed = false;
            for (int y = 0; y < CellSize; y++) {
                int row = outside[y];
                int expanded = row | (row << 1) | (row >> 1);
                if (y > 0) expanded |= outside[y - 1];
                if (y < CellSize - 1) expanded |= outside[y + 1];
                ushort next = (ushort)(expanded & ~rows[y]);
                changed |= next != row;
                outside[y] = next;
            }
        } while (changed);
    }

    private static ushort OutlineRow(ReadOnlySpan<ushort> rows, ReadOnlySpan<ushort> outside, int y, byte neighbors)
    {
        const int full = ushort.MaxValue;
        int above = y > 0 ? outside[y - 1] : (neighbors & FrostAbove) == 0 ? full : 0;
        int below = y < CellSize - 1 ? outside[y + 1] : (neighbors & FrostBelow) == 0 ? full : 0;
        int left = (outside[y] << 1) | ((neighbors & FrostLeft) == 0 ? 1 : 0);
        int right = (outside[y] >> 1) | ((neighbors & FrostRight) == 0 ? 1 << (CellSize - 1) : 0);
        return (ushort)(rows[y] & (above | below | left | right));
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
