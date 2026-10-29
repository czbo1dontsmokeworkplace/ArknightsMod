using System;
using System.IO;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Items.Weapons.Specialist.Phantom;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Specialist.Phantom;

// 本体部署状态与虚影各自保管技能快照、增伤层数和护盾，切技能不能改写现存虚影。
public sealed class PhantomPerformer : ModProjectile
{
    internal bool IsEcho => Projectile.ai[0] == 1;
    internal int Skill => Math.Clamp((int)Projectile.ai[1], 0, 2);
    internal int Rank => PhantomBalance.Rank((int)Projectile.ai[2]);
    internal int Age { get; private set; }
    internal int Layers { get; private set; }
    internal int Shield;
    private bool initialized;
    private PhantomPlayerShadow playerShadow;
    private float attackTimer;
    private float aim;
    private int swipe;
    private Player Owner => Main.player[Projectile.owner];
    public override string Texture => ArknightsMod.noTexture;
    public override void SetDefaults()
    {
        Projectile.width = 24;
        Projectile.height = 48;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = PhantomBalance.Duration;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(Age);
        writer.Write(Layers);
        writer.Write(Shield);
        writer.Write(aim);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Age = Math.Clamp(reader.ReadInt32(), 0, PhantomBalance.Duration);
        Layers = Math.Clamp(reader.ReadInt32(), 0, 10);
        Shield = Math.Max(0, reader.ReadInt32());
        aim = reader.ReadSingle();
        initialized = true;
    }
    internal float DamageMultiplier => Skill == 1 ? 1f + Layers * PhantomBalance.LayerBonus(Rank) : 1f;
    internal void ConsumeLayer()
    {
        if (Projectile.owner != Main.myPlayer || Skill != 1 || Layers <= 0) return;
        Layers--;
        Projectile.netUpdate = true;
    }
    public override void AI()
    {
        if (!Owner.active || Owner.dead || Owner.HeldItem.ModItem is not PhantomDaggers || Age >= PhantomBalance.Duration
            || (IsEcho && Vector2.DistanceSquared(Owner.Center, Projectile.Center) > PhantomBalance.EchoLeash * PhantomBalance.EchoLeash))
        {
            Projectile.Kill();
            return;
        }
        if (!IsEcho) Projectile.Center = Owner.MountedCenter;
        if (!initialized)
        {
            initialized = true;
            Layers = Skill == 1 ? PhantomBalance.Layers(Rank) : 0;
            Shield = Skill == 0 ? (int)(Owner.statLifeMax2 * PhantomBalance.ShieldFraction(Rank) * (IsEcho ? .65f : 1f)) : 0;
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.netUpdate = true;
                if (Skill == 2)
                {
                    int pulse = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                        ModContent.ProjectileType<PhantomCurtain>(), (int)(Projectile.damage * PhantomBalance.PulseMultiplier(Rank)),
                        6f, Projectile.owner, Rank, Main.rand.Next(3));
                    if (Main.projectile.IndexInRange(pulse)) Main.projectile[pulse].CritChance = Projectile.CritChance;
                }
            }
            PhantomVisuals.Burst(Projectile.Center, 22);
            if (!Main.dedServ) SoundEngine.PlaySound(SoundID.Item8 with { Volume = .45f, Pitch = -.3f }, Projectile.Center);
        }
        Projectile.timeLeft = PhantomBalance.Duration - Age + 1;
        if (IsEcho && Projectile.owner == Main.myPlayer && !Owner.CCed && !Owner.noItems)
        {
            // 虚影是固定部署点；只攻击自身近处可见敌人，不越墙追踪。
            NPC target = null;
            float best = 104f * 104f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile)) continue;
                Vector2 closest = new(Math.Clamp(Projectile.Center.X, npc.Left.X, npc.Right.X),
                    Math.Clamp(Projectile.Center.Y, npc.Top.Y, npc.Bottom.Y));
                float distance = Vector2.DistanceSquared(Projectile.Center, closest);
                if (distance >= best || !Collision.CanHitLine(Projectile.Center, 1, 1, npc.position, npc.width, npc.height)) continue;
                target = npc;
                best = distance;
            }
            attackTimer -= 1f;
            if (target != null)
            {
                Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                aim = direction.ToRotation();
                if (attackTimer <= 0)
                {
                    attackTimer = PhantomHoldout.Interval(Owner);
                    // 斩痕从虚影手边生成；最远104像素的目标由伸出的手臂补足距离。
                    Vector2 hand = Projectile.Center + direction * Math.Min(48f, MathF.Sqrt(best));
                    PhantomSlash.Spawn(Projectile, hand, direction, Projectile.damage, ++swipe, Projectile.identity, true);
                    Projectile.netUpdate = true;
                }
            }
            else attackTimer = Math.Max(0, attackTimer);
        }
        ExecutorStatusVisuals.Emit(Projectile, Age, StatusTint, StatusStrength);
        if (Projectile.owner == Main.myPlayer && Age % 60 == 0) Projectile.netUpdate = true;
        Age++;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = Math.Min(1f, Math.Min(Age / 12f, (PhantomBalance.Duration - Age) / 24f));
        if (IsEcho) (playerShadow ??= new PhantomPlayerShadow()).Draw(Owner, Projectile.Center, aim, fade);
        ExecutorStatusVisuals.Draw(Projectile.Center, Age, StatusTint, fade * StatusStrength,
            IsEcho ? 1f : Owner.gravDir);
        return false;
    }
    private Color StatusTint => Skill == 1
        ? new Color(166, 118, 188) : ExecutorStatusVisuals.Phantom;
    private float StatusStrength => Skill switch
    {
        0 => Shield > 0 ? .8f : .25f,
        1 => .25f + .65f * Layers / PhantomBalance.Layers(Rank),
        _ => .45f
    };
    public override void OnKill(int timeLeft) => PhantomVisuals.Burst(Projectile.Center, IsEcho ? 18 : 8);
}
