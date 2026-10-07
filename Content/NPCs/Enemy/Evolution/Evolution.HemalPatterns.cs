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
            lastAxisReflection = -1000;
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
        if (AxisHealthLocked && Main.netMode != NetmodeID.MultiplayerClient) ReflectAxisProjectiles();
        if (Timer >= setup + fire + EvolutionRules.AxisActiveTicks + 45) NextAttack();
    }

    private void ReflectAxisProjectiles()
    {
        Rectangle shield = NPC.Hitbox;
        shield.Inflate(34, 34);
        axisReflectionCandidates.Clear();
        foreach (Projectile shot in Main.ActiveProjectiles)
        {
            // 只接住真正飞向护盾的普通射弹；持续武器、仆从与大型效果不在此列。
            if (!shot.friendly || shot.hostile || shot.damage <= 0 || shot.minion || shot.sentry || shot.ownerHitCheck ||
                ProjectileID.Sets.MinionShot[shot.type] || ProjectileID.Sets.SentryShot[shot.type] ||
                !Main.player.IndexInRange(shot.owner) || !Main.player[shot.owner].active ||
                shot.width > 96 || shot.height > 96 || shot.velocity.LengthSquared() < 16 ||
                Vector2.Dot(shot.velocity, NPC.Center - shot.Center) <= 0) continue;

            float hit = 0;
            if (!shot.Hitbox.Intersects(shield) &&
                !Collision.CheckAABBvLineCollision(shield.TopLeft(), shield.Size(), shot.Center,
                    shot.Center + shot.velocity, Math.Max(shot.width, shot.height) * .5f, ref hit)) continue;
            axisReflectionCandidates.Add(shot);
        }

        // 先收集再处理，避免某些射弹 OnKill 生成的新射弹在同一轮被连锁吞入。
        foreach (Projectile shot in axisReflectionCandidates)
        {
            if (!shot.active) continue;
            Vector2 source = shot.Center;
            Vector2 reflectedDirection = -shot.velocity.SafeNormalize(Vector2.UnitY);
            shot.Kill();
            // 高射速武器也会被护盾接住，但反击最多每12帧一枚，避免玩家自己制造弹幕墙。
            if (Timer - lastAxisReflection < 12) continue;
            lastAxisReflection = Timer;
            Shoot(EvolutionShot.Reflection, source + reflectedDirection * 22, reflectedDirection * 12,
                delay: 12, lifetime: 116);
        }
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
                1600, 26, 152, blade: true, quietLaunch: true);
    }
}
