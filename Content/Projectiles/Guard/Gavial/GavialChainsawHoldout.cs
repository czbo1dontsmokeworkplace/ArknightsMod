using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Guard.Gavial;
using ArknightsMod.Content.Projectiles.BasePROJ;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Gavial;

public sealed class GavialChainsawHoldout : ModProjectile
{
    private int age, biteTimer, impactCooldown, visualMode;
    private float aimAngle, spool;
    private Vector2 hand, drawHand, tip;
    private readonly GavialVisuals.CutMark[] marks = new GavialVisuals.CutMark[24];
    private int nextMark;
    private Player Owner => Main.player[Projectile.owner];
    private int Mode => (int)Projectile.ai[1];
    private float Reach => Mode == 2 ? GavialBalance.AssaultReach : GavialBalance.Reach;
    private float Width => Mode == 2 ? 54f : GavialBalance.Width;
    public override string Texture => "Terraria/Images/Item_" + ItemID.ButchersChainsaw;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 18;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.aiStyle = -1;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }

    public override bool ShouldUpdatePosition() => false;

    public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        => aimAngle = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();

    public override void AI()
    {
        var player = Owner;
        if (!player.active || player.dead || player.noItems || player.CCed
            || player.HeldItem.ModItem is not GavialChainsaw || player.selectedItem != (int)Projectile.ai[0]
            || (player.whoAmI == Main.myPlayer && !player.channel))
        {
            Projectile.Kill();
            return;
        }
        Projectile.timeLeft = 2;

        if (player.whoAmI == Main.myPlayer)
        {
            Vector2 aim = (Main.MouseWorld - player.MountedCenter).SafeNormalize(new Vector2(player.direction, 0f));
            float nextAngle = aim.ToRotation();
            int mode = player.GetModPlayer<GavialChainsawPlayer>().ActiveMode;
            // 从当前物品重新求伤害，技能在持锯途中启停也即时生效；只由持有者写入同步值。
            int damage = player.GetWeaponDamage(player.HeldItem);
            if (mode != Mode || damage != Projectile.damage || age % 6 == 0
                || MathF.Abs(MathHelper.WrapAngle(nextAngle - aimAngle)) > 0.18f)
                Projectile.netUpdate = true;
            Projectile.ai[1] = mode;
            Projectile.damage = damage;
            aimAngle = nextAngle;
        }

        if (visualMode != Mode)
        {
            if (Mode > 0 && player.whoAmI != Main.myPlayer) GavialVisuals.Activate(player, Mode);
            visualMode = Mode;
            // 改变模式不提前刷新命中免疫，不能靠切换技能多打一次。
        }

        spool = MathHelper.Lerp(spool, 1f, 0.16f);
        Vector2 direction = aimAngle.ToRotationVector2();
        Vector2 normal = new(-direction.Y, direction.X);
        hand = player.MountedCenter + direction * 6f;
        drawHand = hand + direction * (MathF.Sin(age * 1.8f) * spool * (Mode == 3 ? 2.1f : 1.2f));
        tip = drawHand + direction * Reach;
        Projectile.Center = (hand + tip) * 0.5f;
        Projectile.velocity = direction;
        Projectile.rotation = aimAngle;
        player.itemTime = player.itemAnimation = 2;
        player.itemRotation = aimAngle + (direction.X < 0f ? MathHelper.Pi : 0f);
        BaseHeldMeleeSupport.HoldAndPose(player, Projectile, hand, direction.X < 0f);

        if (--biteTimer <= 0)
        {
            biteTimer = Mode == 3 ? GavialBalance.SoulBiteTicks : GavialBalance.NormalBiteTicks;
            Projectile.ResetLocalNPCHitImmunity();
        }
        if (Mode == 2 && age % 7 == 0) PullTargets();

        if (!Main.dedServ)
        {
            if (age % 6 == 0)
                SoundEngine.PlaySound(GavialVisuals.MotorSound with
                {
                    Volume = 0.38f, Pitch = Mode == 3 ? 0.24f : -0.18f, MaxInstances = 3
                }, hand);
            if (age % (Mode == 3 ? 2 : 4) == 0 && spool > 0.4f)
                GavialVisuals.Chips(Vector2.Lerp(hand, tip, Main.rand.NextFloat(0.45f, 0.98f)),
                    -direction + normal * Main.rand.NextFloat(-0.8f, 0.8f), 1, false);
            Lighting.AddLight(tip, new Vector3(0.12f, 0.15f, 0.06f) * spool);
            for (int i = 0; i < marks.Length; i++)
                if (marks[i].Time > 0) marks[i].Time--;
        }
        if (impactCooldown > 0) impactCooldown--;
        age++;
    }

    private void PullTargets()
    {
        // NPC 位移只在单人/服务器执行；抗击退目标、Boss 和多节生物不被拖动。
        if (Main.netMode == NetmodeID.MultiplayerClient || spool < 0.4f) return;
        Vector2 latch = hand + aimAngle.ToRotationVector2() * 58f;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy(Projectile) || npc.boss || npc.realLife >= 0
                || npc.knockBackResist <= 0f || !BladeHits(npc.Hitbox)) continue;
            Vector2 delta = latch - npc.Center;
            if (delta.LengthSquared() < 28f * 28f) continue;
            Vector2 desired = delta.SafeNormalize(Vector2.Zero) * 5f;
            npc.velocity = Vector2.Lerp(npc.velocity, desired, 0.4f * Math.Min(1f, npc.knockBackResist));
            npc.netUpdate = true;
        }
    }

    private bool BladeHits(Rectangle target)
    {
        if (!Collision.CanHit(Owner.MountedCenter, 1, 1, target.TopLeft(), target.Width, target.Height))
            return false;
        float point = 0f;
        return Collision.CheckAABBvLineCollision(target.TopLeft(), target.Size(),
            hand + aimAngle.ToRotationVector2() * 20f, tip, Width, ref point);
    }

    public override bool? CanDamage() => spool >= 0.4f ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => spool >= 0.4f && BladeHits(targetHitbox);

    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        modifiers.HitDirectionOverride = target.Center.X >= Owner.Center.X ? 1 : -1;
        if (Mode == 2 && !target.boss && target.knockBackResist > 0f)
            modifiers.Knockback *= 0f; // 锯齿咬住目标，避免普通击退与拖拽相互抵消。
    }

    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        Owner.GetModPlayer<GavialChainsawPlayer>().HealFromSaw(target, damageDone);
        if (Main.dedServ || impactCooldown > 0) return;
        impactCooldown = 4;
        Vector2 contact = target.Hitbox.ClosestPointInRect(Vector2.Lerp(hand, tip, 0.75f));
        GavialVisuals.Chips(contact, -aimAngle.ToRotationVector2(), Mode == 3 ? 7 : 5, true);
        marks[nextMark] = new GavialVisuals.CutMark
        {
            Position = contact, Angle = aimAngle + Main.rand.NextFloat(-0.45f, 0.45f),
            Time = 12, Scale = Mode == 0 ? 1f : 1.3f
        };
        nextMark = (nextMark + 1) % marks.Length;
        SoundEngine.PlaySound(SoundID.NPCHit18 with { Volume = 0.48f, Pitch = -0.3f, MaxInstances = 3 }, contact);
        GavialVisuals.Shake(Owner, Mode == 3 ? 2.4f : 1.5f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        if (Main.dedServ) return false;
        Main.instance.LoadItem(ItemID.ButchersChainsaw);
        Texture2D weapon = TextureAssets.Item[ItemID.ButchersChainsaw].Value;
        GavialVisuals.DrawWeapon(weapon, drawHand, aimAngle, Reach, lightColor);
        BaseHeldMeleeSupport.BeginAdditive(Main.spriteBatch);
        GavialVisuals.DrawChain(hand, tip, age, spool, Mode);
        foreach (var mark in marks) GavialVisuals.DrawCut(mark);
        BaseHeldMeleeSupport.EndAdditive(Main.spriteBatch);
        return false;
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(aimAngle);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        float received = reader.ReadSingle();
        if (float.IsFinite(received)) aimAngle = received;
    }
}
