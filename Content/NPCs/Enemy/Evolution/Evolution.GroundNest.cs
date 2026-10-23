using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

// 第一阶段专用：地面育生核心，不复用后两阶段的飞行与光球攻击。
internal static class EvolutionNestRules
{
    internal const int RadiationInterval = 300; // 每五秒一次，包含一秒预警。
    internal const int RadiationWarning = 60;
    internal const int LaserActiveTicks = 8;
    internal const int LaserFadeTicks = 54;
    internal const int RadiationDuration = LaserActiveTicks;
    internal const float RadiationReach = 1450 * EvolutionRules.LaserReachMultiplier;
    internal const float LaserWidth = 64;
    internal static float Direction(int cycle, int ray) => -MathHelper.PiOver2 + cycle % 4 * MathHelper.Pi / 10 + ray * MathHelper.TwoPi / 5;

}

public sealed partial class Evolution
{
    private void PlaceGroundNest(Player player)
    {
        NPC.width = 132; NPC.height = 260;
        Vector2 near = player.Bottom + new Vector2(player.direction * 240, -32);
        if (TryNestFloor(near, NPC.width, NPC.height, out Vector2 center)) NPC.Center = center;
        else NPC.Center = near - new Vector2(0, NPC.height * .5f); // 无地面时正常下落，不悬空固定。
        Anchor = NPC.Center;
        Arena = NPC.Center - new Vector2(0, 220);
        NPC.noGravity = NPC.noTileCollide = false;
    }

    internal static bool TryNestFloor(Vector2 near, int width, int height, out Vector2 center)
    {
        // 查找完整容身空间，避免把大本体塞进平台下方或斜坡实体中。
        for (int offset = 0; offset < 9; offset++)
        {
            float x = MathHelper.Clamp(near.X + (offset == 0 ? 0 : (offset + 1) / 2 * 48 * (offset % 2 == 0 ? -1 : 1)), 180, Main.maxTilesX * 16 - 180);
            Vector2 probe = new(x, near.Y);
            for (int attempt = 0; attempt < 12; attempt++)
            {
                if (!EvolutionTerrain.TrySurface(probe, out Vector2 surface, 180)) break;
                center = surface - new Vector2(0, height * .5f);
                if (!Collision.SolidCollision(center - new Vector2(width, height) * .5f, width, height)) return true;
                probe = surface + new Vector2(0, 16);
            }
        }
        center = near;
        return false;
    }

    private void DoGroundNest(Player player)
    {
        PreparePattern(player);
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (Timer == 0)
        {
            EvolutionBrood[] opening =
            {
                EvolutionBrood.Spider, EvolutionBrood.Spider, EvolutionBrood.Spider,
                EvolutionBrood.GiantSpider, EvolutionBrood.GiantSpider,
                EvolutionBrood.Puppet, EvolutionBrood.Puppet,
                EvolutionBrood.Abomination, EvolutionBrood.Tumor, EvolutionBrood.Bomb
            };
            for (int i = 0; i < opening.Length; i++) BirthGroundBrood(opening[i], i);
        }
        // 小怪不会在辐射轮次之间被清空；持续产仔，但沿用总上限防止无限堆积。
        if (Timer > 0 && Timer % 100 == 0)
        {
            int clutch = Timer / 100;
            EvolutionBrood kind = (clutch % 4) switch
            {
                0 => EvolutionBrood.Spider, 1 => EvolutionBrood.Tumor,
                2 => EvolutionBrood.Puppet, _ => EvolutionBrood.Bomb
            };
            BirthGroundBrood(kind, clutch * 2);
            BirthGroundBrood(clutch % 2 == 0 ? EvolutionBrood.Spider : EvolutionBrood.Abomination, clutch * 2 + 1);
        }
        if (Timer % EvolutionNestRules.RadiationInterval == 60)
            Shoot(EvolutionShot.Radiation, NPC.Center, Vector2.Zero,
                Timer / EvolutionNestRules.RadiationInterval, EvolutionNestRules.RadiationWarning,
                EvolutionNestRules.RadiationWarning + 16 + EvolutionNestRules.LaserFadeTicks + 20);
    }

    private void BirthGroundBrood(EvolutionBrood kind, int slot)
    {
        float side = slot % 2 == 0 ? -1 : 1;
        Vector2 near = NPC.Bottom + new Vector2(side * (95 + slot % 4 * 23), -30);
        int height = EvolutionBroodNPC.GroundHeight(kind);
        int width = kind == EvolutionBrood.GiantSpider ? 58 : kind == EvolutionBrood.Abomination ? 66 : 36;
        Vector2 point = TryNestFloor(near, width, height, out Vector2 floor) ? floor : near - new Vector2(0, height * .5f);
        SpawnBrood(kind, point, side: side, formation: slot);
    }
}

public abstract partial class EvolutionBroodNPC
{
    private bool groundWasAirborne;
    private Vector2 groundPreviousPosition;
    private int groundStallTicks;
    internal bool GroundClimbing;
    internal float GroundDrawRotation;
    internal Vector2 GroundMuzzle => NPC.Center + new Vector2(NPC.spriteDirection * (NPC.width * .38f), -NPC.height * .18f);
    // 六种贴图的原始朝向相同；只翻转画面，不反转追击、冲刺和发射方向。
    internal SpriteEffects GroundSpriteEffects => NPC.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
    internal void InitializeGroundBody()
    {
        Vector2 feet = NPC.Bottom;
        NPC.width = Kind == EvolutionBrood.GiantSpider ? 58 : Kind == EvolutionBrood.Abomination ? 66 : 36;
        NPC.height = GroundHeight(Kind);
        NPC.Bottom = feet;
    }

    internal static int GroundHeight(EvolutionBrood kind) => kind switch
    {
        EvolutionBrood.Abomination => 64,
        EvolutionBrood.Puppet => 54,
        EvolutionBrood.Tumor => 18,       // 畸变恶性瘤：36x60，三帧，每帧20像素。
        EvolutionBrood.Bomb => 18,        // 第一阶段作为畸变赘生物：36x48，三帧，每帧16像素。
        _ => 32
    };

    internal static bool HasClimbableWall(NPC npc, int side)
    {
        if (side == 0) return false;
        float probeX = side > 0 ? npc.Right.X + 1 : npc.Left.X - 2;
        return Collision.SolidCollision(new Vector2(probeX, npc.Top.Y + 2), 1, Math.Max(4, npc.height - 4));
    }

    private void DoGroundBrood(Evolution boss, Player player)
    {
        InitializeGroundBody();
        bool evolved = boss.Phase == 3;
        bool wallCrawler = Kind is EvolutionBrood.Tumor or EvolutionBrood.Bomb or EvolutionBrood.Spider;
        int wallSide = 0;
        GroundClimbing = false;
        NPC.noGravity = NPC.noTileCollide = false;
        NPC.rotation = 0;
        // 低空带的下沿是生物可站立的能量边界；无地面时也不会把小怪丢在带外。
        bool boundaryFloor = boss.CorridorActive && NPC.Bottom.Y + Math.Max(0, NPC.velocity.Y) >= boss.CorridorBottom - 6;
        if (boundaryFloor)
        {
            NPC.Bottom = new Vector2(NPC.Bottom.X, boss.CorridorBottom - 6);
            NPC.velocity.Y = 0;
        }
        bool grounded = boundaryFloor || NPC.collideY && NPC.velocity.Y == 0;
        if (Age < 35) { NPC.velocity.X *= .8f; return; }
        if (!Main.dedServ && groundWasAirborne && grounded)
        {
            EvolutionArsenalVisuals.Smoke(NPC.Bottom, new Vector2(-2, -.5f), NPC.width + 16, 20, theme: boss.ThemeColor);
            EvolutionArsenalVisuals.Smoke(NPC.Bottom, new Vector2(2, -.5f), NPC.width + 16, 20, theme: boss.ThemeColor);
        }
        groundWasAirborne = !grounded;
        charging = true;
        NPC.damage = (int)(EvolutionDamageCockpit.BroodContactDamage(Kind) * (evolved ? 1.1f : 1f));
        float direction = evolved && EvolutionBroodRangedRules.IsHatchling(Kind)
            ? Math.Clamp((player.Center.X - NPC.Center.X) / 36, -1, 1) : GroundTravelDirection(player);
        float speed = Kind switch
        {
            EvolutionBrood.Spider => evolved ? 4.1f : 2.8f,
            EvolutionBrood.GiantSpider => evolved ? 2.5f : 1.6f,
            EvolutionBrood.Abomination => evolved ? 1.9f : 1.25f,
            EvolutionBrood.Tumor => evolved ? 4.2f : 2.8f,
            EvolutionBrood.Bomb => evolved ? 3.2f : 2.1f,
            _ => evolved ? 2.6f : 1.8f
        };
        if (grounded || wallCrawler) NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, direction * speed + GroundSeparation(), .11f);
        NPC.direction = NPC.spriteDirection = player.Center.X >= NPC.Center.X ? 1 : -1;
        int period = Kind switch
        {
            EvolutionBrood.Spider => evolved ? 138 : 185,
            EvolutionBrood.GiantSpider => evolved ? 165 : 220,
            EvolutionBrood.Puppet => evolved ? 165 : 220,
            EvolutionBrood.Tumor => evolved ? 85 : 115,
            EvolutionBrood.Bomb => evolved ? 125 : 165,
            _ => evolved ? 210 : 300
        };
        int beat = (Age + Formation * 19) % period;
        switch (Kind)
        {
            case EvolutionBrood.Spider:
                if (beat == 40 && Main.netMode != NetmodeID.MultiplayerClient)
                { FlightDirection = new Vector2(NPC.direction, 0); NPC.netUpdate = true; }
                if (beat >= 40 && beat < 66) { NPC.velocity.X *= .75f; warning = (beat - 39) / 26f; }
                if (beat >= 66 && beat < 100 && FlightDirection != Vector2.Zero)
                    NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, FlightDirection.X * (evolved ? 12 : 8.5f), .22f);
                if (beat >= 100 && beat < 120) NPC.velocity.X *= .86f;
                break;
            case EvolutionBrood.GiantSpider:
                if (beat == 45 && Main.netMode != NetmodeID.MultiplayerClient)
                { FlightDirection = new Vector2(NPC.direction, 0); NPC.netUpdate = true; }
                if (beat >= 45 && beat < 75) { NPC.velocity.X *= .8f; warning = (beat - 44) / 30f; }
                if (beat == 75 && grounded)
                { NPC.velocity = new Vector2((FlightDirection.X == 0 ? NPC.direction : FlightDirection.X) * (evolved ? 6.2f : 4.2f), evolved ? -13 : -10); NPC.netUpdate = true; }
                break;
            case EvolutionBrood.Puppet:
                if (beat == 70 && Main.netMode != NetmodeID.MultiplayerClient)
                {
                    FlightAnchor = player.Center + new Vector2(Math.Clamp(player.velocity.X * 12, -100, 100), 0);
                    NPC.netUpdate = true;
                }
                if (beat >= 70 && beat < 116) { NPC.velocity.X *= .72f; warning = Math.Min(1, (beat - 69) / 30f); }
                if (beat == 105)
                {
                    NPC.direction = NPC.spriteDirection = FlightAnchor.X >= NPC.Center.X ? 1 : -1;
                    if (!evolved) boss.LobBroodDust(GroundMuzzle, FlightAnchor, 80);
                }
                break;
            case EvolutionBrood.Abomination:
                if (beat >= 70 && beat < 100) { NPC.velocity.X *= .75f; warning = (beat - 69) / 30f; }
                if (beat == 100)
                    for (int i = 0; i < (evolved ? 2 : 1); i++) boss.SpawnBrood(EvolutionBrood.Tumor,
                        NPC.Bottom + new Vector2((i == 0 ? -1 : 1) * 38, -10), side: NPC.direction, formation: Formation + i);
                break;
            case EvolutionBrood.Tumor:
            case EvolutionBrood.Bomb:
                if (evolved)
                {
                    if (UpdateEvolvedHatchling(boss, player, Age - 35 + 45)) return;
                    break;
                }
                if (beat >= 45 && beat < 60 && grounded) { NPC.velocity.X *= .7f; warning = (beat - 44) / 15f; }
                if (beat == 60 && grounded)
                {
                    bool leaper = Kind == EvolutionBrood.Bomb;
                    NPC.velocity = new Vector2(NPC.direction * (leaper ? (evolved ? 7 : 5) : speed),
                        leaper ? (evolved ? -12 : -9) : (evolved ? -9 : -6));
                    NPC.netUpdate = true;
                }
                break;
        }
        if (evolved) UpdateEvolvedRangedAttack(boss, player, beat);
        NavigateGroundObstacle(boss, player, grounded, wallCrawler, evolved);
        wallSide = GroundClimbing ? Math.Sign(NPC.ai[2]) : 0;
        if (NPC.rotation == 0)
        {
            int facing = Math.Abs(NPC.velocity.X) > .35f ? Math.Sign(NPC.velocity.X) : NPC.direction;
            if (Kind == EvolutionBrood.Spider && beat >= 40 && beat < 120 && FlightDirection.X != 0)
                facing = Math.Sign(FlightDirection.X);
            if (Kind == EvolutionBrood.GiantSpider && beat >= 45 && beat <= 75 && FlightDirection.X != 0)
                facing = Math.Sign(FlightDirection.X);
            if (Kind == EvolutionBrood.Puppet && beat >= 70 && beat < 116)
                facing = FlightAnchor.X >= NPC.Center.X ? 1 : -1;
            NPC.direction = NPC.spriteDirection = facing == 0 ? 1 : facing;
        }
        if (!Main.dedServ && Age % 6 == 0)
        {
            if (Kind == EvolutionBrood.Spider && beat >= 66 && beat < 100)
                EvolutionArsenalVisuals.Smoke(NPC.Bottom, new Vector2(-NPC.direction * 2, -.4f), 22, 16, theme: boss.ThemeColor);
            else if (wallCrawler && NPC.noGravity)
                EvolutionArsenalVisuals.Smoke(NPC.Center + new Vector2(wallSide * NPC.width * .5f, 3), Vector2.UnitY, 13, 14, theme: boss.ThemeColor);
        }
        GroundDrawRotation = GroundDrawRotation.AngleTowards(GroundClimbing ? -wallSide * MathHelper.PiOver2 : 0, .18f);
        if (boss.CorridorActive && NPC.Top.Y < boss.CorridorTop + 6)
        {
            NPC.position.Y = boss.CorridorTop + 6;
            NPC.velocity.Y = Math.Max(0, NPC.velocity.Y);
        }
        if (evolved && (NPC.Distance(player.Center) > 2000 || Age > 2400) && Main.netMode != NetmodeID.MultiplayerClient)
        { NPC.ai[3] = -35; NPC.netUpdate = true; }
    }

    private void DrawGroundBrood(SpriteBatch batch, Vector2 screenPos, Color light, float opacity)
    {
        string name = Kind switch { EvolutionBrood.Spider => "Spider", EvolutionBrood.GiantSpider => "GiantSpider", EvolutionBrood.Puppet => "Puppet", EvolutionBrood.Abomination => "Abomination", EvolutionBrood.Bomb => "Excrescence", _ => "Tumor" };
        Texture2D texture = EvolutionVisuals.Asset(name);
        int frames = Kind switch { EvolutionBrood.Spider => 15, EvolutionBrood.GiantSpider => 24, EvolutionBrood.Puppet => 19, EvolutionBrood.Abomination => 19, EvolutionBrood.Tumor or EvolutionBrood.Bomb => 3, _ => 1 };
        int height = texture.Height / frames;
        // 只循环行走帧，不把受击/死亡帧混进行走动画。
        int frame = NPC.velocity.LengthSquared() > .04f ? Age / 7 % Math.Min(8, frames) : 0;
        Rectangle source = new(0, frame * height, texture.Width, height);
        float scale = Kind == EvolutionBrood.Abomination ? .95f : 1;
        bool wallClimbing = GroundClimbing || Math.Abs(GroundDrawRotation) > .01f;
        Vector2 drawPosition = wallClimbing ? NPC.Center + new Vector2(Math.Sign(NPC.ai[2]) * (NPC.width - height) * .5f, 0) : NPC.Bottom + new Vector2(0, 2);
        Vector2 drawOrigin = wallClimbing ? new Vector2(texture.Width * .5f, height * .5f) : new Vector2(texture.Width * .5f, height);
        Color theme = Parent?.ThemeColor ?? EvolutionVisuals.Sand;
        if (Molting)
        {
            float charge = EvolutionCinematicFrame.Smooth(0, EvolutionPresentationRules.BroodMoltBurst, MoltAge);
            drawPosition += new Vector2(MathF.Sin(MoltAge * 2.7f + NPC.whoAmI), MathF.Cos(MoltAge * 2.1f)) * (charge * 5);
            EvolutionVisuals.Glow(NPC.Center, theme * (.6f * charge * opacity), new Vector2(NPC.width + 45, NPC.height + 25));
            if (MoltAge >= EvolutionPresentationRules.BroodMoltBurst)
                EvolutionArsenalVisuals.Blast(NPC.Center, MoltAge - EvolutionPresentationRules.BroodMoltBurst, NPC.width + 32, theme);
        }
        if (Age < 35) EvolutionVisuals.Glow(NPC.Center, theme * (.35f * opacity), new Vector2(NPC.width + 35, NPC.height + 20));
        if (warning > 0)
        {
            EvolutionVisuals.Glow(NPC.Center, theme * (.35f * warning), new Vector2(NPC.width + 20, NPC.height + 10));
            if (Kind == EvolutionBrood.Spider && FlightDirection != Vector2.Zero)
                EvolutionVisuals.Line(NPC.Bottom - new Vector2(0, 9), NPC.Bottom + FlightDirection * 240 - new Vector2(0, 9), theme * (.5f * warning), 1.5f);
            if (Kind == EvolutionBrood.Puppet)
            {
                float flight = Parent?.Phase == 3 ? 65 : 75;
                bool evolved = Parent?.Phase == 3;
                flight = evolved ? EvolutionBroodRangedRules.FlightTicks(70, 1.3f) : 80;
                float gravity = .22f;
                for (int lane = 0; lane < 1; lane++)
                {
                    Vector2 point = GroundMuzzle;
                    Vector2 landing = FlightAnchor;
                    Vector2 velocity = new((landing.X - point.X) / flight,
                        (landing.Y - point.Y) / flight - gravity * (flight + 1) * .5f);
                    for (int step = 0; step < (int)flight; step++)
                    {
                        velocity.Y = Math.Min(15, velocity.Y + gravity);
                        Vector2 next = point + velocity;
                        if (Collision.SolidCollision(next - new Vector2(6), 12, 12)) break;
                        if (step % 8 < 4) EvolutionVisuals.Line(point, next, theme * (.45f * warning), 1, false);
                        point = next;
                    }
                }
            }
        }
        for (int edge = 0; edge < 4; edge++)
        {
            Vector2 offset = (edge * MathHelper.PiOver2).ToRotationVector2() * 1.5f;
            batch.Draw(EvolutionBroodOutlineVisuals.Mask(name, texture), drawPosition + offset - screenPos, source, (theme with { A = 0 }) * (.65f * opacity),
                GroundDrawRotation, drawOrigin, scale, GroundSpriteEffects, 0);
        }
        batch.Draw(texture, drawPosition - screenPos, source,
            Color.Lerp(light, Color.White, .22f) * opacity, GroundDrawRotation, drawOrigin, scale,
            GroundSpriteEffects, 0);
    }
}
