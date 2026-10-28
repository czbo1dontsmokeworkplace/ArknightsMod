using ArknightsMod.Content.Buffs;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Sniper.Rosmontis;

public class RosmontisTacticalEquipment : ModProjectile
{
    public const int MaxLife = 500;
    public const int Defense = 48;
    public const int Aggro = 0;
    public const float DebuffRange = 180f;
    public const int DefenseReduction = 40;
    public const float ResistanceReduction = .20f;
    public const int DeploymentTicks = 4; // 约 0.067 秒（60 tick/秒）

    private Vector2 deploymentStart;
    private Vector2 deploymentTarget;
    private bool deploymentInitialized;
    public bool IsDeployed => deploymentInitialized && Projectile.ai[1] >= 0f;

    public int Life { get => (int)Projectile.ai[0]; private set => Projectile.ai[0] = value; }
    public override string Texture => "ArknightsMod/Content/Projectiles/Sniper/Rosmontis/RosmontisTacticalEquipment";

    public override void SetDefaults() {
        Projectile.width = 48;
        Projectile.height = 64;
        Projectile.friendly = false;
        Projectile.hostile = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 20 * 60 + DeploymentTicks;
        Projectile.penetrate = -1;
        Projectile.netImportant = true;
    }

    public override void OnSpawn(IEntitySource source) => Life = MaxLife;
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;

    public void BeginDeployment(Vector2 destination) {
        deploymentStart = Projectile.Center;
        deploymentTarget = destination;
        deploymentInitialized = true;
        Projectile.ai[1] = -DeploymentTicks;
        Projectile.netUpdate = true;
    }

    public override void SendExtraAI(BinaryWriter writer) {
        writer.Write(deploymentInitialized);
        writer.Write(deploymentStart.X);
        writer.Write(deploymentStart.Y);
        writer.Write(deploymentTarget.X);
        writer.Write(deploymentTarget.Y);
    }

    public override void ReceiveExtraAI(BinaryReader reader) {
        deploymentInitialized = reader.ReadBoolean();
        deploymentStart = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        deploymentTarget = new Vector2(reader.ReadSingle(), reader.ReadSingle());
    }

    public override void AI() {
        Projectile.velocity = Vector2.Zero;
        if (!deploymentInitialized) return;
        if (!IsDeployed) {
            Projectile.ai[1]++;
            float progress = MathHelper.Clamp(1f + Projectile.ai[1] / DeploymentTicks, 0f, 1f);
            Projectile.Center = Vector2.Lerp(deploymentStart, deploymentTarget, MathHelper.SmoothStep(0f, 1f, progress));
            if (IsDeployed) {
                Projectile.Center = deploymentTarget;
                Projectile.timeLeft = 20 * 60;
                Projectile.netUpdate = true;
            }
            else return;
        }
        // 单机/服务器负责生命、眩晕及拦截；ai 字段随弹幕同步。
        if (Main.netMode == NetmodeID.MultiplayerClient) return;
        if (Life <= 0) { Projectile.Kill(); return; }

        if (Projectile.ai[2] == 0f) {
            Projectile.ai[2] = 1f;
            Projectile.netUpdate = true;
            foreach (NPC npc in Main.ActiveNPCs) {
                if (!npc.friendly && npc.CanBeChasedBy()
                    && Vector2.DistanceSquared(npc.Center, Projectile.Center) <= 160f * 160f)
                    OperatorStunNPC.TryApply(npc, 6 * 60);
            }
        }

        foreach (Projectile hostile in Main.ActiveProjectiles) {
            if (hostile.whoAmI == Projectile.whoAmI || !hostile.hostile || hostile.damage <= 0) continue;
            // 使用敌对弹幕自己的碰撞形状，兼容细长射线等自定义碰撞。
            if (!hostile.Colliding(hostile.Hitbox, Projectile.Hitbox)) continue;
            // 按 NPC 的常规防御换算：48 防御抵消 24 点伤害，最低受到 1 点。
            int damage = DamageAfterDefense(hostile.damage);
            Life = System.Math.Max(0, Life - damage);
            Projectile.netUpdate = true;
            hostile.Kill();
            if (Life <= 0) { Projectile.Kill(); return; }
        }
    }

    internal static int DamageAfterDefense(int damage) => System.Math.Max(1, damage - Defense / 2);

    // 每次命中时检查是否仍在有效装备范围内，不修改敌人的基础属性，不叠加多台装备。
    public static bool IsInDebuffRange(NPC npc) {
        foreach (Projectile gear in Main.ActiveProjectiles) {
            if (gear.ModProjectile is RosmontisTacticalEquipment equipment && equipment.IsDeployed && equipment.Life > 0
                && Vector2.DistanceSquared(npc.Center, gear.Center) <= DebuffRange * DebuffRange)
                return true;
        }
        return false;
    }

    public override void PostDraw(Color lightColor) {
        if (Main.dedServ || !IsDeployed || Life >= MaxLife) return;
        Main.instance.DrawHealthBar(Projectile.Center.X, Projectile.Bottom.Y + 10f,
            Life, MaxLife, 1f, 1f);
    }
}
