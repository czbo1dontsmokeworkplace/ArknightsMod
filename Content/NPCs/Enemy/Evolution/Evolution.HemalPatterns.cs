using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

public sealed partial class Evolution
{
    private void RadialHemalBurst(Player player)
    {
        // 锁定一次朝向，把玩家当时所在方向留作两束之间的通道；不是追踪式扫射。
        float angle = (PatternCenter - NPC.Center).ToRotation() + MathHelper.Pi / 8;
        for (int i = 0; i < 8; i++)
            Shoot(EvolutionShot.Beam, NPC.Center, (angle + i * MathHelper.PiOver4).ToRotationVector2(), 2100, 44, 84);
        EvolutionImpactSystem.Emit(NPC.Center, 370, 1.5f, true, 42);
    }

    private void DoHemalAxis(Player player)
    {
        const int setup = EvolutionRules.AxisSetupTicks;
        int fire = EvolutionRules.FireTime(EvolutionShot.Beam, EvolutionRules.LaserWarning(EvolutionRules.AxisChargeTicks));
        if (Timer == 0)
        {
            ClearBrood();
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                Anchor = player.Center + new Vector2(NPC.Center.X < player.Center.X ? -460 : 460, -90);
                NPC.netUpdate = true;
            }
        }
        if (Timer < setup) MoveTo(Anchor, 12, .065f);
        else NPC.velocity *= .82f;
        if (Timer == setup)
        {
            Shoot(EvolutionShot.Beam, NPC.Center, -Vector2.UnitY, 1750, EvolutionRules.AxisChargeTicks,
                beamStyle: EvolutionBeamStyle.Axis);
            EvolutionImpactSystem.Emit(NPC.Center, 720, 0, true, fire);
        }
        if (Timer >= setup + fire + EvolutionRules.AxisActiveTicks + 45) NextAttack();
    }
}

public sealed partial class EvolutionHazard
{
    internal bool IsAxis => Kind == EvolutionShot.Beam && BeamStyle == EvolutionBeamStyle.Axis;
    internal Vector2 AxisDirection => EvolutionRules.AxisAngle(Age - FireAge).ToRotationVector2();
    internal Vector2 AxisRayDirection(int ray) => AxisDirection.RotatedBy(ray * MathHelper.TwoPi / EvolutionRules.AxisRayCount);
    internal float AxisOpacity => Age < FireAge ? 0 : MathHelper.Clamp((Lifetime - Age) / 24f, 0, 1);

    private void UpdateHemalAxis(Evolution boss)
    {
        // 主体停止追人，仅让光轴缓慢转动。网络传输的时钟决定方向，不积累旋转误差。
        Projectile.Center = boss.NPC.Center;
        Projectile.velocity = AxisDirection;
        int active = Age - FireAge;
        if (active == 0)
        {
            EvolutionVisuals.Burst(Projectile.Center, 1.5f);
            EvolutionImpactSystem.Emit(Projectile.Center, 650, 7);
            // 原版455号幻象死光的发射声：Zombie104。整组三叉射线只播一次。
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Zombie104, Projectile.Center);
        }
        int lane = EvolutionRules.AxisBladeLane(active);
        if (lane == int.MinValue) return;
        Vector2 axis = AxisRayDirection(EvolutionRules.AxisBladeRay(active)), normal = axis.RotatedBy(MathHelper.PiOver2);
        // 三条射线由内向外轮流放出双侧刃，中心两个槽保留为空。
        Vector2 source = Projectile.Center + axis * (lane * 190);
        for (int side = -1; side <= 1; side += 2)
            boss.Shoot(EvolutionShot.Lance, source + normal * side * 186, normal * side * 17,
                1600, 26, 152, blade: true);
    }
}
