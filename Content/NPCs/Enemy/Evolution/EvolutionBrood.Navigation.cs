using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public abstract partial class EvolutionBroodNPC
{
    private float GroundTravelDirection(Player player)
    {
        int slot = (Formation + NPC.whoAmI * 3) % 7;
        float flank = (NPC.whoAmI + Formation) % 2 == 0 ? -1 : 1;
        float radius = Kind == EvolutionBrood.Puppet ? 250 + slot * 35 :
            Kind == EvolutionBrood.Abomination ? 150 + slot * 20 : 30 + slot * 23;
        float targetX = player.Center.X + flank * radius;
        // 抵达分散槽位后也小幅游走，避免所有小怪在玩家正下方收成一团。
        targetX += MathF.Sin((Age + NPC.whoAmI * 13) * .023f) * 45;
        return MathHelper.Clamp((targetX - NPC.Center.X) / 36, -1, 1);
    }
    private float GroundSeparation()
    {
        float force = 0;
        foreach (NPC other in Main.ActiveNPCs)
        {
            if (other.whoAmI == NPC.whoAmI || other.ModNPC is not EvolutionBroodNPC brood ||
                brood.Encounter != Encounter || brood.Retiring || brood.SupportOnly || Math.Abs(other.Center.Y - NPC.Center.Y) > 70) continue;
            float dx = NPC.Center.X - other.Center.X;
            float spacing = (NPC.width + other.width) * .5f + 26;
            if (Math.Abs(dx) >= spacing) continue;
            float side = Math.Abs(dx) < 1 ? (NPC.whoAmI > other.whoAmI ? 1 : -1) : Math.Sign(dx);
            force += side * (1 - Math.Abs(dx) / spacing) * 2.6f;
        }
        return Math.Clamp(force, -3.2f, 3.2f);
    }
    private void NavigateGroundObstacle(Evolution boss, Player player, bool grounded, bool crawler, bool evolved)
    {
        if (Vector2.DistanceSquared(NPC.position, groundPreviousPosition) < .5f && grounded) groundStallTicks++;
        else groundStallTicks = Math.Max(0, groundStallTicks - 2);
        groundPreviousPosition = NPC.position;
        int toward = Math.Abs(NPC.velocity.X) > .1f ? Math.Sign(NPC.velocity.X) : NPC.direction;
        int wall = HasClimbableWall(NPC, toward) ? toward :
            Math.Abs(NPC.ai[2]) == 1 && HasClimbableWall(NPC, Math.Sign(NPC.ai[2])) ? Math.Sign(NPC.ai[2]) : 0;
        bool climb = wall != 0 && (crawler || groundStallTicks > 20 || Math.Abs(GroundDrawRotation)>.2f);
        if (climb)
        {
            if (NPC.ai[2] != wall) { NPC.ai[2] = wall; NPC.netUpdate = true; }
            GroundClimbing = true;
            NPC.noGravity = true;
            NPC.rotation = -wall * MathHelper.PiOver2;
            NPC.direction = NPC.spriteDirection = wall;
            NPC.velocity = new Vector2(wall * .5f, evolved ? -3.2f : -2.3f);
            // 墙顶转上台面：先检查整身空间，不能卡着砖角不停原地攀爬。
            Vector2 ledge = NPC.position + new Vector2(wall * 18, -16);
            if (!Collision.SolidCollision(ledge, NPC.width, NPC.height))
            { NPC.velocity = new Vector2(wall * 3, -4); NPC.ai[2] = 0; GroundClimbing = false; }
        }
        else
        {
            if (Math.Abs(GroundDrawRotation) > .2f && !grounded)
                NPC.velocity = new Vector2(NPC.spriteDirection * 3.5f, Math.Min(NPC.velocity.Y, -3));
            if (NPC.ai[2] != 0) { NPC.ai[2] = 0; NPC.netUpdate = true; }
            if (grounded && (NPC.collideX || groundStallTicks > 25 || player.Bottom.Y < NPC.Top.Y - 65) &&
                (Age + NPC.whoAmI * 7) % 24 == 0)
            {
                NPC.velocity = new Vector2((toward == 0 ? 1 : toward) * (evolved ? 5 : 3.5f), evolved ? -10 : -8);
                NPC.netUpdate = true;
            }
        }
        if (groundStallTicks > 80)
        {
            // 障碍前持续主动脱困，错开每个单位时序；不传送、不穿墙。
            NPC.velocity.X = -toward * (evolved ? 5 : 3.5f);
            if (grounded) NPC.velocity.Y = -9;
            groundStallTicks = 0; NPC.netUpdate = true;
        }
    }
}
