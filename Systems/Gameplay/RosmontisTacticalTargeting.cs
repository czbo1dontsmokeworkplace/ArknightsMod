using ArknightsMod.Content.Projectiles.Sniper.Rosmontis;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ModLoader;

namespace ArknightsMod.Systems.Gameplay;

// 装备按 aggro=0 与玩家竞争目标，不强制覆盖高仇恨或更近的玩家。
// 使用 targetRect / GetTargetData 的 AI 可以瞄准装备；直接读取 Main.player[target] 的专属 AI
// 仍会瞄准玩家，不能用伪造玩家坐标的方式改变它们。
public sealed class RosmontisTacticalTargeting : ModSystem
{
    public override void Load() {
        On_NPC.TargetClosest += TargetClosest;
        On_NPC.TargetClosestUpgraded += TargetClosestUpgraded;
        On_NPC.GetTargetData += GetTargetData;
    }

    public override void Unload() {
        On_NPC.TargetClosest -= TargetClosest;
        On_NPC.TargetClosestUpgraded -= TargetClosestUpgraded;
        On_NPC.GetTargetData -= GetTargetData;
    }

    private static Projectile SelectEquipment(NPC npc, Vector2 origin) {
        if (!npc.active || npc.friendly || npc.damage < 0) return null;
        float bestScore = float.MaxValue;
        foreach (Player player in Main.ActivePlayers) {
            if (player.dead || player.ghost) continue;
            float score = Score(origin, player.Center, player.aggro);
            if (player.npcTypeNoAggro[npc.type] && npc.direction != 0) score += 1000f;
            if (score < bestScore) bestScore = score;
        }
        Projectile best = null;
        foreach (Projectile gear in Main.ActiveProjectiles) {
            if (gear.ModProjectile is not RosmontisTacticalEquipment equipment || !equipment.IsDeployed || equipment.Life <= 0
                || gear.owner < 0 || gear.owner >= Main.maxPlayers
                || !Main.player[gear.owner].active || Main.player[gear.owner].dead) continue;
            float score = Score(origin, gear.Center, RosmontisTacticalEquipment.Aggro);
            if (score < bestScore) { bestScore = score; best = gear; }
        }
        return best;
    }

    private static float Score(Vector2 origin, Vector2 target, int aggro)
        => System.Math.Abs(origin.X - target.X) + System.Math.Abs(origin.Y - target.Y) - aggro;

    private static void ApplyTarget(NPC npc, Projectile gear, bool faceTarget) {
        if (gear == null) return;
        // 保留合法的玩家目标索引，避免旧式 AI 把 NPC 索引当成玩家索引导致越界。
        npc.target = gear.owner;
        npc.targetRect = gear.Hitbox;
        if (faceTarget) {
            npc.direction = gear.Center.X < npc.Center.X ? -1 : 1;
            npc.directionY = gear.Center.Y < npc.Center.Y ? -1 : 1;
        }
    }

    private static void TargetClosest(On_NPC.orig_TargetClosest orig, NPC npc, bool faceTarget) {
        orig(npc, faceTarget);
        ApplyTarget(npc, SelectEquipment(npc, npc.Center), faceTarget);
    }

    private static void TargetClosestUpgraded(On_NPC.orig_TargetClosestUpgraded orig, NPC npc,
        bool faceTarget, Vector2? checkPosition) {
        orig(npc, faceTarget, checkPosition);
        ApplyTarget(npc, SelectEquipment(npc, checkPosition ?? npc.Center), faceTarget);
    }

    private static NPCAimedTarget GetTargetData(On_NPC.orig_GetTargetData orig, NPC npc, bool ignorePlayerTankPets) {
        Projectile gear = SelectEquipment(npc, npc.Center);
        if (gear == null) return orig(npc, ignorePlayerTankPets);
        return new NPCAimedTarget {
            Type = NPCTargetType.PlayerTankPet,
            Hitbox = gear.Hitbox,
            Width = gear.width,
            Height = gear.height,
            Position = gear.position,
            Velocity = Vector2.Zero,
        };
    }
}
