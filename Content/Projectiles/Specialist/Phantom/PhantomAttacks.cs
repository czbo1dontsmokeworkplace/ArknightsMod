using System;
using System.IO;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Weapons.Specialist.Phantom;
using ArknightsMod.Content.Projectiles.Specialist.Red;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Specialist.Phantom;

public sealed class PhantomHoldout : ModProjectile
{
    private float timer, front, back;
    private int age, swipe;
    private Player Owner => Main.player[Projectile.owner];
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Specialist/Red/RedDagger";
    internal static float Interval(Player owner) => Math.Clamp(5f / Math.Max(.1f, owner.GetTotalAttackSpeed(DamageClass.Melee)), 2f, 50f);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!Owner.active || Owner.dead || Owner.CCed || Owner.noItems || Owner.HeldItem.ModItem is not PhantomDaggers
            || (Projectile.owner == Main.myPlayer && !Owner.channel))
        {
            Projectile.Kill();
            return;
        }
        Projectile.Center = Owner.MountedCenter;
        Projectile.timeLeft = 2;
        if (Projectile.owner == Main.myPlayer)
        {
            Vector2 direction = (Main.MouseWorld - Owner.MountedCenter).SafeNormalize(new Vector2(Owner.direction, 0));
            if (Vector2.DistanceSquared(direction, Projectile.velocity) > .008f || age % 6 == 0) Projectile.netUpdate = true;
            Projectile.velocity = direction;
            if (timer <= 0)
            {
                timer += Interval(Owner);
                PhantomPerformer body = Owner.GetModPlayer<PhantomPlayer>().Performer(false);
                PhantomSlash.Spawn(Projectile, Owner.MountedCenter + direction * 14f, direction,
                    Owner.GetWeaponDamage(Owner.HeldItem), ++swipe, body?.Projectile.identity ?? -1, false);
            }
            timer--;
        }
        Vector2 aim = Projectile.velocity.SafeNormalize(new Vector2(Owner.direction, 0));
        float angle = aim.ToRotation();
        Owner.ChangeDir(aim.X < 0 ? -1 : 1);
        Owner.heldProj = Projectile.whoAmI;
        Owner.itemTime = Owner.itemAnimation = 2;
        Owner.itemRotation = angle + (Owner.direction < 0 ? MathHelper.Pi : 0);
        float swing = MathF.Sin(age * MathHelper.Pi / Interval(Owner)) * 1.05f;
        front = angle - MathHelper.PiOver2 + swing;
        back = angle - MathHelper.PiOver2 - swing;
        Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, front);
        Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, back);
        age++;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        PhantomVisuals.DrawKnife(Owner.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, back), back, .65f);
        PhantomVisuals.DrawKnife(Owner.GetFrontHandPosition(Player.CompositeArmStretchAmount.Full, front), front, 1f);
        return false;
    }
}

public sealed class PhantomSlash : ModProjectile
{
    private const int Lifetime = 20;
    private bool spentLayer;
    private int Age => Lifetime - Projectile.timeLeft;
    private bool IsEcho => Math.Abs(Projectile.ai[1]) > 1.5f;
    private Player Owner => Main.player[Projectile.owner];
    public override string Texture => "ArknightsMod/Content/Projectiles/Guard/Hellagur/HellagurSlashBody";
    private PhantomPerformer Parent
    {
        get
        {
            if (Projectile.ai[2] < 0) return null;
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.owner == Projectile.owner && p.identity == (int)Projectile.ai[2]
                    && p.ModProjectile is PhantomPerformer performer && performer.IsEcho == IsEcho) return performer;
            return null;
        }
    }
    internal static void Spawn(Projectile source, Vector2 center, Vector2 direction, int damage, int swipe, int parentIdentity, bool echo)
    {
        if (source.owner != Main.myPlayer) return;
        float angle = direction.ToRotation() + Main.rand.NextFloat(-.32f, .32f);
        int index = Projectile.NewProjectile(source.GetSource_FromThis(), center, angle.ToRotationVector2() * 4.5f,
            ModContent.ProjectileType<PhantomSlash>(), damage, 2f, source.owner,
            angle, ((swipe & 1) == 0 ? -1 : 1) * (echo ? 2 : 1), parentIdentity);
        if (Main.projectile.IndexInRange(index))
            Main.projectile[index].CritChance = Main.player[source.owner].GetWeaponCrit(Main.player[source.owner].HeldItem);
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 110;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = Lifetime;
    }
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanHitNPC(NPC target) => target.CanBeChasedBy(Projectile) ? null : false;
    public override bool? CanDamage() => Age is >= 1 and <= 10 ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        PhantomPerformer parent = Parent;
        Vector2 origin = IsEcho && parent != null ? parent.Projectile.Center : Owner.MountedCenter;
        return (!IsEcho || parent != null)
            && RedDaggerVisuals.CircleHits(Projectile.Center + Projectile.ai[0].ToRotationVector2() * 16, 38f, targetHitbox)
            && Collision.CanHitLine(origin, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height);
    }
    public override void AI()
    {
        if (!Owner.active || Owner.dead || Owner.HeldItem.ModItem is not PhantomDaggers
            || Vector2.DistanceSquared(Owner.Center, Projectile.Center) > 680f * 680f || (IsEcho && Parent == null))
        {
            Projectile.Kill();
            return;
        }
        Projectile.velocity = (Projectile.velocity * .9f).RotatedBy(.05236f * MathF.Sign(Projectile.ai[1]));
        if (Age == 0 && !Main.dedServ)
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = IsEcho ? .12f : .23f, Pitch = .35f,
                PitchVariance = .12f, MaxInstances = 4 }, Projectile.Center);
        Lighting.AddLight(Projectile.Center, new Vector3(.12f, .22f, .28f) * (1f - Age / 20f));
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        // 成功命中前读取剩余层数；空挥不消耗，单道斩痕最多消耗一层。
        if (!spentLayer && Parent is { } parent) modifiers.SourceDamage *= parent.DamageMultiplier;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (damageDone <= 0) return;
        if (!spentLayer && Projectile.owner == Main.myPlayer)
        {
            Parent?.ConsumeLayer();
            spentLayer = true;
            Projectile.netUpdate = true;
        }
        PhantomVisuals.Burst(target.Center, 5);
    }
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(spentLayer);
    public override void ReceiveExtraAI(BinaryReader reader) => spentLayer = reader.ReadBoolean();
    public override bool PreDraw(ref Color lightColor)
    {
        float p = Age / (float)Lifetime;
        float angle = Projectile.ai[0] + MathF.Sign(Projectile.ai[1]) * (-1.2f + (1f - MathF.Pow(1f - p, 3f)) * 4.3f);
        PhantomVisuals.DrawSlash(Projectile.Center, angle, MathF.Sin(p * MathHelper.Pi), Projectile.ai[1] < 0, IsEcho);
        return false;
    }
}

public sealed class PhantomCurtain : ModProjectile
{
    private int Age => 30 - Projectile.timeLeft;
    public override string Texture => ArknightsMod.noTexture;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 256;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
        Projectile.timeLeft = 30;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool CanHitPvp(Player target) => false;
    public override bool? CanCutTiles() => false;
    public override bool? CanDamage() => Age is >= 2 and <= 4 ? null : false;
    public override bool? CanHitNPC(NPC target) => target.CanBeChasedBy(Projectile) ? null : false;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
        RedDaggerVisuals.CircleHits(Projectile.Center, PhantomBalance.PulseRadius, targetHitbox)
        && Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height);
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead || owner.HeldItem.ModItem is not PhantomDaggers)
        {
            Projectile.Kill();
            return;
        }
        if (Age == 2)
        {
            PhantomVisuals.Burst(Projectile.Center, 32);
            // 控制仅由服务器施加，不依赖 owner-only 的命中回调。
            if (Main.netMode != NetmodeID.MultiplayerClient)
                foreach (NPC npc in Main.ActiveNPCs)
                {
                    if (!npc.CanBeChasedBy(Projectile) || npc.boss || npc.realLife >= 0
                        || Colliding(Projectile.Hitbox, npc.Hitbox) != true) continue;
                    int duration = PhantomBalance.ControlTime((int)Projectile.ai[0]);
                    switch ((int)Projectile.ai[1])
                    {
                        case 0: npc.AddBuff(ModContent.BuffType<PhantomSlow>(), duration); break;
                        case 1: npc.AddBuff(ModContent.BuffType<PhantomBind>(), duration); break;
                        default: OperatorStunNPC.TryApply(npc, duration); break;
                    }
                    npc.netUpdate = true;
                }
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        PhantomVisuals.DrawCurtain(Projectile.Center, Age / 30f);
        return false;
    }
}
