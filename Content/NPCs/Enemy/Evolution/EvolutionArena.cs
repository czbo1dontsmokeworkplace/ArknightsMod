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
            if (n.ModNPC is Evolution boss && boss.ArenaActive && boss.Encounter != 0) { arenaBoss = boss; break; }
        if (arenaBoss == null) { encounter = 0; return; }
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
}

public sealed class EvolutionArenaSystem : ModSystem
{
    public override void Unload() => EvolutionVisuals.Unload();
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu) return;
        Evolution boss = null;
        foreach (NPC n in Main.ActiveNPCs) if (n.ModNPC is Evolution b && b.ArenaActive) { boss = b; break; }
        if (boss == null) return;
        Main.spriteBatch.Begin(Microsoft.Xna.Framework.Graphics.SpriteSortMode.Deferred, Microsoft.Xna.Framework.Graphics.BlendState.AlphaBlend,
            Microsoft.Xna.Framework.Graphics.SamplerState.LinearClamp, Microsoft.Xna.Framework.Graphics.DepthStencilState.None,
            Microsoft.Xna.Framework.Graphics.RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        float fade = MathHelper.Clamp(boss.NPC.ai[3] / 75f, 0, 1);
        if (boss.Phase == 2 && boss.Timer > 525) fade *= (600 - boss.Timer) / 75f;
        float radius = EvolutionRules.ArenaRadius;
        EvolutionVisuals.Ring(boss.Arena, radius, 0, EvolutionVisuals.Blood * (.8f * fade), 14, false);
        EvolutionVisuals.Ring(boss.Arena, radius + 14 + MathF.Sin(boss.NPC.ai[3] * .08f) * 4, 0, EvolutionVisuals.Core * (.2f * fade), 3, false);
        Main.spriteBatch.End();
    }
}
