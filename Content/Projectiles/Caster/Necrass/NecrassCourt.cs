using System;
using System.Collections.Generic;
using ArknightsMod.Content.Items.Weapons.Caster.Necrass;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass;

public sealed class NecrassCourt : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    internal static bool Authority => Main.netMode != NetmodeID.MultiplayerClient;
    internal int Mode => Math.Clamp((int)Projectile.ai[0], 0, 2);
    internal int Rank => Math.Clamp((int)Projectile.ai[1], 0, 9);
    internal int Elite => Math.Clamp((int)Projectile.ai[2], 0, 2);
    private readonly Queue<(Vector2 Location, int Attack)> souls = new();
    private int budgetTimer, refillCooldown;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 120;
        Projectile.penetrate = -1;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.ModItem is not NecrassScepter)
        {
            if (Authority || Projectile.owner == Main.myPlayer) Projectile.Kill();
            return;
        }
        Projectile.Center = owner.MountedCenter;
        Projectile.timeLeft = 120;
        if (!Authority) return;
        // 群怪死亡最多每六帧处理一次召唤；队列有上限，避免连锁爆炸无限递归。
        if (++budgetTimer >= 6)
        {
            budgetTimer = 0;
            if (souls.Count > 0) { var soul = souls.Dequeue(); Summon(soul.Location, soul.Attack); }
        }
        if (refillCooldown > 0) refillCooldown--;
        foreach (var servant in Servants(owner.whoAmI))
            if (Mode != 2 && servant.Special) servant.Dismiss();
    }
    internal void QueueSouls(Vector2 location, int count, int attack)
    {
        if (!Authority) return;
        if (souls.Count == 0 && budgetTimer == 0 && count > 0)
        {
            Summon(location, attack);
            count--;
            budgetTimer = 1;
        }
        for (int i = 0; i < count && souls.Count < 12; i++) souls.Enqueue((location, Math.Max(1, attack)));
    }
    internal bool Seed(Vector2 center)
    {
        if (!Authority || refillCooldown > 0 || Servants(Projectile.owner).Count > 0) return false;
        refillCooldown = 300;
        Summon(center);
        return true;
    }
    internal void Reassemble()
    {
        List<int> attacks = [];
        foreach (var servant in Servants(Projectile.owner))
        {
            attacks.Add(servant.InheritedAttack);
            if (servant.Level > 0) attacks.Add(servant.InheritedAttack);
            servant.Dismiss();
        }
        souls.Clear();
        if (attacks.Count == 0) attacks.Add(ResolveAttack(Projectile.Center));
        for (int i = 0; i < attacks.Count; i++) Summon(Projectile.Center + new Vector2((i - (attacks.Count - 1) * .5f) * 65, 0), attacks[i]);
    }
    private int ResolveAttack(Vector2 location)
    {
        int attack = Target(location, 760, Main.player[Projectile.owner])?.damage ?? 40;
        foreach (var servant in Servants(Projectile.owner)) attack = Math.Max(attack, servant.InheritedAttack);
        return Math.Max(1, attack);
    }
    internal void Summon(Vector2 location, int attack = -1)
    {
        if (!Authority) return;
        attack = attack < 0 ? ResolveAttack(location) : Math.Max(1, attack);
        Player owner = Main.player[Projectile.owner];
        var servants = Servants(Projectile.owner);
        int cap = Elite == 0 ? 2 : 3;
        if (servants.Count >= cap)
        {
            NecrassServant chosen = null;
            foreach (var s in servants)
                if (!s.Special && (chosen == null || (s.Level == 0 && chosen.Level > 0)
                    || (s.Level == chosen.Level && s.HealthRatio < chosen.HealthRatio))) chosen = s;
            if (chosen != null)
            {
                bool upgrading = chosen.Level == 0;
                chosen.InheritAttack(Math.Max(chosen.InheritedAttack, attack));
                chosen.Upgrade(1, true);
                if (upgrading) BirthExplosion(chosen.Projectile.Center, chosen.InheritedAttack);
            }
            return;
        }
        bool special = Mode == 2 && !servants.Exists(s => s.Special);
        Vector2 center = SafePosition(owner, location);
        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero,
            ModContent.ProjectileType<NecrassServant>(), attack, 3, Projectile.owner, special ? 1 : 0);
        if (!Main.projectile.IndexInRange(index)) return;
        Main.projectile[index].netUpdate = true;
        BirthExplosion(center, attack);
    }
    private void BirthExplosion(Vector2 center, int attack)
    {
        int damage = Mode == 0 ? (int)(attack * (2.5f + Rank * 2f / 9)) : 0;
        NecrassImpact.Spawn(Projectile, center, Vector2.Zero, damage, 0, 90);
    }
    internal static Projectile Find(int owner)
    {
        int type = ModContent.ProjectileType<NecrassCourt>();
        foreach (Projectile p in Main.ActiveProjectiles) if (p.owner == owner && p.type == type) return p;
        return null;
    }
    internal static List<NecrassServant> Servants(int owner)
    {
        List<NecrassServant> result = [];
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == owner && p.ModProjectile is NecrassServant servant && !servant.ArmorServant && !servant.Dying) result.Add(servant);
        return result;
    }
    internal static Vector2 SafePosition(Player owner, Vector2 wanted)
    {
        Vector2 delta = wanted - owner.Center;
        if (delta.LengthSquared() > 600 * 600) delta = delta.SafeNormalize(Vector2.UnitX) * 600;
        Vector2 result = owner.Center;
        for (float distance = 16; distance <= delta.Length(); distance += 16)
        {
            Vector2 next = owner.Center + delta.SafeNormalize(Vector2.UnitX) * distance;
            if (next.X < 32 || next.Y < 32 || next.X > Main.maxTilesX * 16 - 32 || next.Y > Main.maxTilesY * 16 - 32
                || Collision.SolidCollision(next - new Vector2(20, 28), 40, 56)) break;
            result = next;
        }
        return result;
    }
    internal static NPC Target(Vector2 center, float range, Player owner)
    {
        NPC chosen = null;
        float best = range * range;
        int forced = owner.MinionAttackTargetNPC;
        if (forced >= 0 && forced < Main.maxNPCs)
        {
            NPC npc = Main.npc[forced];
            if (npc.CanBeChasedBy() && Vector2.DistanceSquared(center, npc.Center) <= best
                && Collision.CanHitLine(center, 1, 1, npc.position, npc.width, npc.height)) return npc;
        }
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float distance = Vector2.DistanceSquared(center, npc.Center);
            if (distance < best && npc.CanBeChasedBy() && Collision.CanHitLine(center, 1, 1, npc.position, npc.width, npc.height))
            { chosen = npc; best = distance; }
        }
        return chosen;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float time = Main.GlobalTimeWrappedHourly;
        Vector2 center = Projectile.Center + new Vector2(-Main.player[Projectile.owner].direction * 25, -9);
        NecrassVisuals.Crown(center, 20, time, .65f, Mode == 2 ? 6 : 3);
        return false;
    }
}

public sealed class NecrassDeaths : GlobalNPC
{
    public override void OnKill(NPC npc)
    {
        if (!NecrassCourt.Authority || npc.friendly || npc.lifeMax <= 5 || npc.realLife >= 0 || npc.SpawnedFromStatue) return;
        foreach (Player player in Main.ActivePlayers)
        {
            if (player.dead || player.HeldItem.ModItem is not NecrassScepter) continue;
            Projectile p = NecrassCourt.Find(player.whoAmI);
            if (p?.ModProjectile is not NecrassCourt court) continue;
            bool inRange = player.Distance(npc.Center) <= 760;
            if (!inRange)
                foreach (var servant in NecrassCourt.Servants(player.whoAmI))
                    if (servant.Projectile.Distance(npc.Center) <= 300) { inRange = true; break; }
            if (inRange) court.QueueSouls(npc.Center, 1 + (NecrassSleep.HasBrand(npc, player.whoAmI) ? 2 : 0), npc.damage);
        }
        // 清除绑定，避免 NPC 槽位重新使用后将沉睡转移给新敌人。
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.ModProjectile is NecrassSleep && (int)p.ai[0] == npc.whoAmI) p.Kill();
    }
}
