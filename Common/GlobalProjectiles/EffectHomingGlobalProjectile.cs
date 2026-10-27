using System;
using ArknightsMod.Content.Items.Armor.Caster.Mornia;
using ArknightsMod.Content.Items.Armor.Caster.Steward;
using ArknightsMod.Content.Items.Armor.Sniper.Adnachiel;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Common.GlobalProjectiles;

/// <summary>套装、饰品和增益赋予的弹幕追踪统一入口。新增效果只需在 Load 的规则表添加一项。</summary>
public sealed class EffectHomingGlobalProjectile : GlobalProjectile
{
    public enum TargetPriority { Nearest, HighestDefense }

    /// <param name="IsActive">按弹幕属主判断效果是否激活，每次更新重新判断。</param>
    /// <param name="DamageTypes">任意匹配一种即可；使用 CountsAsClass，支持继承职业。</param>
    /// <param name="Range">以弹幕中心为圆心的索敌半径，单位像素。</param>
    /// <param name="TurnDegreesPerTick">每游戏帧最大转向角度，自动平摊到 extraUpdates。</param>
    public sealed record HomingRule(
        Func<Player, bool> IsActive,
        DamageClass[] DamageTypes,
        float Range,
        float TurnDegreesPerTick,
        TargetPriority Priority = TargetPriority.Nearest,
        Func<NPC, bool> TargetFilter = null,
        bool IncludeArtsDamage = false,
        bool RequireLineOfSight = true);

    // 所有弹幕共享规则配置；非 InstancePerEntity 的 GlobalProjectile 不允许实例字段。
    private static HomingRule[] rules;

    public override void Load() {
        // 多条规则同时生效时按表中顺序选第一条能找到目标的规则，不重复转向、不叠加强度。
        rules = new[] {
            new HomingRule(p => p.GetModPlayer<StewardSetPlayer>().StewardSetActive,
                new[] { DamageClass.Magic }, 600f, 3f,
                Priority: TargetPriority.HighestDefense, IncludeArtsDamage: true),
            new HomingRule(p => p.GetModPlayer<MorniaSetPlayer>().SetActive,
                new[] { DamageClass.Magic }, 600f, 3f),
            new HomingRule(p => p.GetModPlayer<AdnachielSetPlayer>().AdnachielSetActive,
                new[] { DamageClass.Ranged }, 600f, 3f),
        };
    }

    public override void Unload() => rules = null;

    public override void PostAI(Projectile projectile) {
        if (rules == null || !CanHome(projectile)) return;
        Player owner = Main.player[projectile.owner];
        foreach (HomingRule rule in rules) {
            if (!rule.IsActive(owner) || !MatchesDamage(projectile, rule)) continue;
            NPC target = FindTarget(projectile, rule);
            if (target == null) continue;
            TurnTowards(projectile, target.Center, rule.TurnDegreesPerTick);
            break;
        }
    }

    private static bool CanHome(Projectile p) {
        if (p.owner < 0 || p.owner >= Main.maxPlayers || !p.friendly || p.hostile || p.damage <= 0
            || p.minion || p.sentry || p.aiStyle is 19 or 75
            || !float.IsFinite(p.velocity.X) || !float.IsFinite(p.velocity.Y)
            || p.velocity.LengthSquared() < .01f || p.ModProjectile?.ShouldUpdatePosition() == false)
            return false;
        if (p.ModProjectile is ICoreCasterGuidance core && !core.GuidanceReady) return false;
        Player owner = Main.player[p.owner];
        return owner.active && !owner.dead && owner.heldProj != p.whoAmI;
    }

    private static bool MatchesDamage(Projectile p, HomingRule rule) {
        foreach (DamageClass type in rule.DamageTypes)
            if (p.DamageType.CountsAsClass(type)) return true;
        return rule.IncludeArtsDamage && p.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage;
    }

    private static NPC FindTarget(Projectile p, HomingRule rule) {
        NPC best = null;
        float bestDistance = float.MaxValue;
        foreach (NPC npc in Main.ActiveNPCs) {
            if (npc.friendly || npc.dontTakeDamage || npc.immortal || npc.lifeMax <= 5
                || !npc.CanBeChasedBy(p) || rule.TargetFilter?.Invoke(npc) == false) continue;
            float distance = Vector2.DistanceSquared(p.Center, npc.Center);
            if (distance > rule.Range * rule.Range || (rule.RequireLineOfSight
                && !Collision.CanHitLine(p.Center, 1, 1, npc.Center, 1, 1))) continue;
            bool preferDefense = rule.Priority == TargetPriority.HighestDefense;
            if (best == null || (preferDefense && npc.defense > best.defense)
                || ((!preferDefense || npc.defense == best.defense) && distance < bestDistance)) {
                best = npc;
                bestDistance = distance;
            }
        }
        return best;
    }

    private static void TurnTowards(Projectile p, Vector2 destination, float degrees) {
        Vector2 offset = destination - p.Center;
        if (offset.LengthSquared() < .0001f) return;
        // 参考星极 ArrowStormTracking：索敌后调整 velocity；保留原弹速和自然寿命。
        float maxTurn = MathHelper.ToRadians(degrees) / p.MaxUpdates;
        float delta = MathHelper.WrapAngle(offset.ToRotation() - p.velocity.ToRotation());
        p.velocity = p.velocity.RotatedBy(MathHelper.Clamp(delta, -maxTurn, maxTurn));
    }
}
