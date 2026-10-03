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
    internal const int RippleCount = 3;
    internal const int RippleSpacing = 18;
    internal const int RippleTravel = 44;
    internal const float RadiationReach = 560;
    internal const float HalfAngle = MathHelper.Pi / 36; // 总宽度10度，而非左右各10度。
    internal const int RadiationDuration = RippleSpacing * (RippleCount - 1) + RippleTravel;
    internal static float Radius(int age) => MathHelper.Lerp(100, RadiationReach, Math.Clamp(age / (float)RippleTravel, 0, 1));
    internal static bool RippleActive(int age) => age >= 0 && age < RippleTravel;
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
                EvolutionNestRules.RadiationWarning + EvolutionNestRules.RadiationDuration + 18);
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
        bool wallCrawler = Kind is EvolutionBrood.Tumor or EvolutionBrood.Bomb;
        int wallSide = wallCrawler ? Math.Sign(NPC.ai[2]) : 0;
        if (wallSide != 0 && HasClimbableWall(NPC, wallSide))
        {
            // 重复把碰撞箱压回墙边，减少坡面/拐角处的视觉穿插。
            float wallFace = wallSide > 0 ? NPC.Right.X : NPC.Left.X;
            int tileX = (int)MathF.Floor((wallFace - (wallSide < 0 ? .01f : 0)) / 16f);
            float exactFace = (tileX + (wallSide < 0 ? 1 : 0)) * 16;
            NPC.position.X += exactFace - wallFace;
            NPC.noGravity = true;
            NPC.noTileCollide = false;
            NPC.rotation = wallSide > 0 ? MathHelper.PiOver2 : -MathHelper.PiOver2;
            NPC.velocity = new Vector2(wallSide * 2.4f, -1.65f);
        }
        else
        {
            if (wallSide != 0) { NPC.ai[2] = 0; NPC.netUpdate = true; }
            NPC.rotation = 0;
            NPC.noGravity = false;
            NPC.noTileCollide = false;
        }
        if (Age < 35) { NPC.velocity.X *= .8f; return; }
        charging = true; // 这里代表接触伤害开启，不调用空中冲刺逻辑。
        NPC.damage = EvolutionDamageCockpit.BroodContactDamage(Kind);
        float direction = Math.Abs(player.Center.X - NPC.Center.X) < 20 ? 0 : Math.Sign(player.Center.X - NPC.Center.X);
        float speed = Kind switch { EvolutionBrood.Spider => 3.5f, EvolutionBrood.Tumor => 2.8f, EvolutionBrood.Bomb => 2.35f, EvolutionBrood.GiantSpider => 2, EvolutionBrood.Abomination => 1.7f, _ => 2.5f };
        if (wallSide == 0)
        {
            NPC.velocity.X = MathHelper.Lerp(NPC.velocity.X, direction * speed, .06f);
            NPC.direction = NPC.spriteDirection = direction == 0 ? NPC.direction : (int)direction;
            if (wallCrawler && NPC.collideX && HasClimbableWall(NPC, NPC.direction))
            {
                NPC.ai[2] = NPC.direction;
                NPC.noGravity = true;
                NPC.rotation = NPC.direction > 0 ? MathHelper.PiOver2 : -MathHelper.PiOver2;
                NPC.velocity = new Vector2(NPC.direction * speed, -1.65f);
                NPC.netUpdate = true;
            }
        }
        // 只能从地面跳过障碍，不因玩家飞得高而获得持续升力。
        if (NPC.collideX && NPC.collideY && Age % 24 == 0) NPC.velocity.Y = -5.6f;
        if (Kind == EvolutionBrood.Puppet && Age % 180 == 110 && Math.Abs(player.Center.X - NPC.Center.X) < 650)
        {
            NPC.velocity.X *= .25f;
            boss.Lob(EvolutionShot.Blood, NPC.Center, NPC.Center + new Vector2(NPC.direction * 330, 0), 60);
        }
        if (Kind == EvolutionBrood.Abomination && Age % 240 == 150)
            boss.SpawnBrood(EvolutionBrood.Tumor, NPC.Bottom - new Vector2(0, 9), side: NPC.direction);
    }

    private void DrawGroundBrood(SpriteBatch batch, Vector2 screenPos, Color light, float opacity)
    {
        string name = Kind switch { EvolutionBrood.Spider => "Spider", EvolutionBrood.GiantSpider => "GiantSpider", EvolutionBrood.Puppet => "Puppet", EvolutionBrood.Abomination => "Abomination", EvolutionBrood.Bomb => "Excrescence", _ => "Tumor" };
        Texture2D texture = EvolutionVisuals.Asset(name);
        int frames = Kind switch { EvolutionBrood.Spider => 15, EvolutionBrood.GiantSpider => 24, EvolutionBrood.Puppet => 19, EvolutionBrood.Abomination => 19, EvolutionBrood.Tumor or EvolutionBrood.Bomb => 3, _ => 1 };
        int height = texture.Height / frames;
        // 只循环行走帧，不把受击/死亡帧混进行走动画。
        int frame = Math.Abs(NPC.velocity.X) > .2f ? Age / 7 % Math.Min(8, frames) : 0;
        Rectangle source = new(0, frame * height, texture.Width, height);
        float scale = Kind == EvolutionBrood.Abomination ? .95f : 1;
        bool wallClimbing = (Kind is EvolutionBrood.Tumor or EvolutionBrood.Bomb) && NPC.noGravity && NPC.rotation != 0;
        Vector2 drawPosition = wallClimbing ? NPC.Center : NPC.Bottom + new Vector2(0, 2);
        Vector2 drawOrigin = wallClimbing ? new Vector2(texture.Width * .5f, height * .5f) : new Vector2(texture.Width * .5f, height);
        batch.Draw(texture, drawPosition - screenPos, source,
            Color.Lerp(light, Color.White, .22f) * opacity, NPC.rotation, drawOrigin, scale,
            NPC.spriteDirection < 0 ? SpriteEffects.None : SpriteEffects.FlipHorizontally, 0);
    }
}
