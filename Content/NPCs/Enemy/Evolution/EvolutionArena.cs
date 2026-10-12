using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed class EvolutionArenaPlayer : ModPlayer
{
    private int encounter;
    private Vector2 safePosition;
    public override void PostUpdate()
    {
        if (Player.dead || !Player.active) { encounter = 0; return; }
        Evolution arenaBoss = null;
        foreach (NPC n in Main.ActiveNPCs)
            if (n.ModNPC is Evolution boss && (boss.ArenaActive || boss.CorridorActive) && boss.Encounter != 0) { arenaBoss = boss; break; }
        if (arenaBoss == null) { encounter = 0; return; }
        if (arenaBoss.CorridorActive) { ConstrainCorridor(arenaBoss); return; }
        float radius = EvolutionRules.ArenaRadius - 40;
        float distance = Player.Distance(arenaBoss.Arena);
        if (encounter != arenaBoss.Encounter)
        {
            // Only players entering the arena participate; remote players are not dragged across the world.
            if (distance > radius || Collision.SolidCollision(Player.position, Player.width, Player.height)) return;
            encounter = arenaBoss.Encounter; safePosition = Player.position;
        }
        if (distance < radius - 24 && !Collision.SolidCollision(Player.position, Player.width, Player.height)) safePosition = Player.position;
        if (distance <= radius) return;
        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer) return;
        Vector2 outward = (Player.Center - arenaBoss.Arena).SafeNormalize(Vector2.UnitX);
        Vector2 destination = arenaBoss.Arena + outward * (radius - 24) - Player.Size * .5f;
        if (Collision.SolidCollision(destination, Player.width, Player.height)) destination = safePosition;
        Player.Teleport(destination, TeleportationStyleID.RodOfDiscord);
        float speed = Vector2.Dot(Player.velocity, outward);
        if (speed > 0) Player.velocity -= outward * speed;
        Player.fallStart = (int)(Player.position.Y / 16);
        if (Main.netMode == NetmodeID.Server)
            NetMessage.SendData(MessageID.TeleportEntity, -1, -1, null, 0, Player.whoAmI, destination.X, destination.Y, TeleportationStyleID.RodOfDiscord);
    }

    private void ConstrainCorridor(Evolution boss)
    {
        if (encounter != boss.Encounter)
        {
            if (Player.Distance(boss.NPC.Center) > 2400) return;
            encounter = boss.Encounter;
        }
        float top = boss.CorridorTop + 4, bottom = boss.CorridorBottom - Player.height - 4;
        if (Player.position.Y >= top && Player.position.Y <= bottom)
        {
            if (!Collision.SolidCollision(Player.position, Player.width, Player.height)) safePosition = Player.position;
            return;
        }
        if (Main.netMode == NetmodeID.MultiplayerClient && Player.whoAmI != Main.myPlayer) return;
        bool above = Player.position.Y < top;
        Vector2 destination = new(Player.position.X, MathHelper.Clamp(Player.position.Y, top, bottom));
        bool found = !Collision.SolidCollision(destination, Player.width, Player.height);
        // 地形起伏时先在同一X附近向带内找容身空间，不能直接把玩家夹进实体砖。
        for (int depth = 0; !found && depth < 48; depth++)
            for (int column = 0; !found && column < 13; column++)
            {
                float dx = column == 0 ? 0 : (column + 1) / 2 * 16 * (column % 2 == 0 ? -1 : 1);
                Vector2 probe = new(MathHelper.Clamp(Player.position.X + dx, 16, Main.maxTilesX * 16 - Player.width - 16),
                    MathHelper.Clamp(destination.Y + (above ? 1 : -1) * depth * 16, top, bottom));
                if (Collision.SolidCollision(probe, Player.width, Player.height)) continue;
                destination = probe; found = true;
            }
        if (!found && safePosition.Y >= top && safePosition.Y <= bottom &&
            !Collision.SolidCollision(safePosition, Player.width, Player.height)) { destination = safePosition; found = true; }
        if (!found) return; // 全部为实体时不破坏地形；下一帧继续尝试。
        Vector2 velocity = Player.velocity;
        // 边界是连续限位，直接校正位置，避免贴边飞行每帧触发传送白闪/镜头插值。
        Player.position = destination;
        Player.velocity = new Vector2(velocity.X, above ? System.Math.Max(0, velocity.Y) : System.Math.Min(0, velocity.Y));
        Player.fallStart = (int)(Player.position.Y / 16);
        safePosition = destination;
        // 越界钩爪不能下一帧把玩家重新拖出场地；坐骑仍可在场内正常使用。
        Player.RemoveAllGrapplingHooks();
        if (Main.netMode != NetmodeID.SinglePlayer)
            NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Player.whoAmI);
    }
}

public sealed class EvolutionArenaSystem : ModSystem
{
    public override void PostSetupContent() { if (!Main.dedServ) EvolutionVisuals.Preload(); }
    public override void Unload() => EvolutionVisuals.Unload();
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        Evolution boss = null;
        foreach (NPC n in Main.ActiveNPCs) if (n.ModNPC is Evolution b && (b.ArenaActive || b.CorridorActive)) { boss = b; break; }
        if (boss == null) return;
        Main.spriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
            Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp, Microsoft.Xna.Framework.Graphics.DepthStencilState.None,
            Microsoft.Xna.Framework.Graphics.RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        float fade = MathHelper.Clamp(boss.NPC.ai[3] / 75f, 0, 1);
        if (boss.CorridorActive)
        {
            float halfWidth = Main.screenWidth / Main.GameViewMatrix.Zoom.X * .5f + 100;
            float x = Main.screenPosition.X + Main.screenWidth * .5f;
            foreach (float y in new[] { boss.CorridorTop, boss.CorridorBottom })
            {
                Vector2 left = new(x - halfWidth, y), right = new(x + halfWidth, y);
                EvolutionVisuals.Line(left, right, EvolutionVisuals.Blood * .7f, 3);
                EvolutionVisuals.Line(left, right, EvolutionVisuals.Core * .55f, 1, false);
            }
            Main.spriteBatch.End();
            return;
        }
        float radius = EvolutionRules.ArenaRadius;
        EvolutionVisuals.Ring(boss.Arena, radius, 0, boss.ThemeColor * (.8f * fade), 14, false);
        EvolutionVisuals.Ring(boss.Arena, radius + 14 + MathF.Sin(boss.NPC.ai[3] * .08f) * 4, 0, EvolutionVisuals.Core * (.2f * fade), 3, false);
        Main.spriteBatch.End();
    }
}
