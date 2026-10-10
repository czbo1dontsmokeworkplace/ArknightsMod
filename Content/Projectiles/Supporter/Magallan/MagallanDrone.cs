using System;
using System.IO;
using ArknightsMod.Content.Buffs.Supporter.Magallan;
using ArknightsMod.Content.Items.Weapons.Supporter.Magallan;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Supporter.Magallan;

public sealed class MagallanDrone : ModProjectile
{
    private const float VisualScale = 1f;
    private int cooldown;
    private int observedModule = -1;
    private int idleTimer;

    public int Module => Math.Clamp((int)Projectile.ai[0], 0, 2);
    public bool Boosted => Projectile.ai[1] > 0;
    public bool Recalling => Projectile.ai[2] < 0f;
    public Vector2 VisualCenter => Projectile.Center + new Vector2(0f,
        MathF.Sin((float)Main.GameUpdateCount * 0.045f + Projectile.identity * 1.7f) * 3f);
    public Vector2 Muzzle => VisualCenter + new Vector2(Projectile.spriteDirection * 4f, 5f) * VisualScale;
    private int AttackDuration => Module == 1 && Boosted ? 20 : 36;

    internal static int AttackDamage(int baseDamage, int module, bool boosted) => module switch
    {
        0 => Math.Max(1, (int)(baseDamage * 0.65f)),
        1 => baseDamage,
        _ => Math.Max(1, (int)(baseDamage * (boosted ? 2f : 1.5f)))
    };

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.CultistIsResistantTo[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
        // Right-click recalls drones, so the vanilla minion target selector must not intercept it.
        ProjectileID.Sets.DrawScreenCheckFluff[Type] = 420;
    }

    public override void SetDefaults()
    {
        Projectile.width = 30;
        Projectile.height = 36;
        Projectile.DamageType = DamageClass.Summon;
        Projectile.minion = true;
        Projectile.sentry = false;
        Projectile.minionSlots = 1f;
        Projectile.netImportant = true;
        Projectile.friendly = true;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        // Allow the summon buff to arrive on remote clients before the ordinary two-tick refresh starts.
        Projectile.timeLeft = 60;
    }

    public override bool? CanDamage() => false;
    public override bool? CanCutTiles() => false;
    public override bool ShouldUpdatePosition() => true;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write((sbyte)Projectile.spriteDirection);
    public override void ReceiveExtraAI(BinaryReader reader) => Projectile.spriteDirection = reader.ReadSByte();

    public void ApplyCommand(int module)
    {
        if (Recalling) return;
        if (Module != module)
        {
            Projectile.ai[2] = 0f;
            // Preserve the fire cooldown across mode changes: switching is not an extra shot.
        }
        Projectile.ai[0] = module;
        Projectile.ai[1] = 1f;
        Projectile.netUpdate = true;
    }

    public void BeginRecall()
    {
        if (Recalling) return;
        Projectile.ai[2] = -1f;
        Projectile.localAI[0] = 0f;
        Projectile.timeLeft = Math.Max(Projectile.timeLeft, 600);
        Projectile.netUpdate = true;
        MagallanVisuals.SwitchBurst(VisualCenter, Module);
    }

    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active || owner.dead) { Projectile.Kill(); return; }
        if (Recalling)
        {
            RecallAI(owner);
            return;
        }
        if (owner.HasBuff<MagallanDroneBuff>()) Projectile.timeLeft = 2;
        FollowOwner(owner);
        if (Projectile.ai[2] > 0) Projectile.ai[2]--;
        idleTimer++;
        if (observedModule != Module)
        {
            if (observedModule < 0) cooldown = 15 + Projectile.identity % 20;
            observedModule = Module;
            MagallanVisuals.SwitchBurst(VisualCenter, Module);
        }
        if (!Main.dedServ)
            Lighting.AddLight(VisualCenter, MagallanVisuals.ModuleColor(Module).ToVector3() * (Boosted ? 0.45f : 0.2f));

        // Only the owner creates attacks; ordinary projectile replication carries them to peers.
        if (Projectile.owner != Main.myPlayer) return;
        if (cooldown > 0) cooldown--;
        float range = Module switch { 0 => Boosted ? 280f : 230f, 1 => 15f * 16f, _ => 150f * 16f };
        NPC target = FindTarget(range);
        if (target == null || cooldown > 0) return;
        Projectile.spriteDirection = target.Center.X >= Projectile.Center.X ? 1 : -1;
        Vector2 aim = (target.Center - Muzzle).SafeNormalize(Vector2.UnitX);
        Projectile.ai[2] = Module == 0 ? 0f : AttackDuration;
        Projectile.netUpdate = true;
        int type;
        int damage = AttackDamage(Projectile.damage, Module, Boosted);
        Vector2 spawn = Muzzle;
        Vector2 velocity = Vector2.Zero;
        float a0 = 0f, a1 = 0f, a2 = Boosted ? 1f : 0f;
        switch (Module)
        {
            case 0:
                cooldown = Boosted ? 60 : 90;
                type = ModContent.ProjectileType<MagallanColdPulse>();
                spawn = VisualCenter;
                a0 = range;
                break;
            case 1:
                cooldown = Boosted ? 20 : 36;
                type = ModContent.ProjectileType<MagallanLaser>();
                velocity = aim;
                a0 = Projectile.identity;
                a1 = range;
                a2 = (target.whoAmI + 1) * (Boosted ? 1 : -1);
                break;
            default:
                cooldown = 40;
                type = ModContent.ProjectileType<MagallanShell>();
                velocity = MagallanShell.LaunchVelocity;
                a0 = target.whoAmI + 1;
                break;
        }
        Projectile.NewProjectile(Projectile.GetSource_FromAI(), spawn, velocity, type, damage,
            Projectile.knockBack, Projectile.owner, a0, a1, a2);
    }

    private void FollowOwner(Player owner)
    {
        int index = 0;
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == Projectile.owner && p.identity < Projectile.identity
                && p.ModProjectile is MagallanDrone drone && !drone.Recalling)
                index++;
        float angle = index * 2.399963f;
        float radius = 36f + MathF.Sqrt(index) * 26f;
        Vector2 destination = owner.MountedCenter + new Vector2(
            MathF.Cos(angle) * radius, -62f + MathF.Sin(angle) * radius * 0.55f);
        Vector2 delta = destination - Projectile.Center;
        float distance = delta.Length();
        if (distance < 10f)
            Projectile.velocity *= 0.76f;
        else
        {
            float speed = MathHelper.Clamp(distance * 0.1f, 2f, 60f);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity,
                delta.SafeNormalize(Vector2.UnitY) * speed, 0.2f);
        }
        if (Projectile.owner == Main.myPlayer && idleTimer % 30 == 0)
            Projectile.netUpdate = true;
    }

    private void RecallAI(Player owner)
    {
        Projectile.localAI[0]++;
        if (Projectile.localAI[0] == 1f)
            Projectile.timeLeft = Math.Max(Projectile.timeLeft, 600);
        Vector2 destination = owner.MountedCenter + new Vector2((Projectile.identity % 5 - 2) * 9f, -28f);
        Vector2 toOwner = destination - Projectile.Center;
        float distance = toOwner.Length();
        float speed = MathHelper.Clamp(distance * 0.065f, 14f, 60f);
        Projectile.velocity = Vector2.Lerp(Projectile.velocity,
            toOwner.SafeNormalize(Vector2.UnitY) * speed, 0.28f);
        Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
        if (Projectile.localAI[0] % 3f == 0f)
            MagallanVisuals.Puff(VisualCenter, -Projectile.velocity * 0.07f,
                Color.Lerp(MagallanVisuals.ModuleColor(Module), new Color(160, 175, 185), 0.65f),
                22f, 26, 0.35f);
        if (Projectile.owner != Main.myPlayer) return;
        if ((int)Projectile.localAI[0] % 10 == 0) Projectile.netUpdate = true;
        if (distance < 28f && Projectile.localAI[0] >= 14f) Projectile.Kill();
    }

    public override void OnKill(int timeLeft)
    {
        if (!Recalling || Main.dedServ) return;
        for (int i = 0; i < 5; i++)
            MagallanVisuals.Puff(VisualCenter + Main.rand.NextVector2Circular(7f, 7f),
                Main.rand.NextVector2Circular(1.2f, 1.2f),
                Color.Lerp(MagallanVisuals.ModuleColor(Module), new Color(183, 196, 205), 0.7f),
                Main.rand.NextFloat(18f, 31f), 28, 0.4f);
    }

    private NPC FindTarget(float range)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.HasMinionAttackTargetNPC)
        {
            NPC marked = Main.npc[owner.MinionAttackTargetNPC];
            if (ValidTarget(marked, range)) return marked;
        }
        NPC best = null;
        float distance = range * range;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            float next = Vector2.DistanceSquared(npc.Center, Muzzle);
            if (next < distance && ValidTarget(npc, range)) { distance = next; best = npc; }
        }
        return best;
    }

    private bool ValidTarget(NPC npc, float range) => npc.CanBeChasedBy(Projectile)
        && Vector2.DistanceSquared(npc.Center, Muzzle) <= range * range
        && (Module == 2 || Collision.CanHitLine(Muzzle, 1, 1, npc.position, npc.width, npc.height));

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture + (Module switch
        {
            0 => "_Cold",
            1 => "_Laser",
            _ => "_Missile"
        })).Value;
        int frameHeight = Module switch { 0 => 36, 1 => 44, _ => 70 };
        int frame = Module == 0 ? 0
            : Recalling ? Math.Clamp((int)Projectile.localAI[0], 0, 11)
            : Projectile.ai[2] > 0 ? Math.Clamp((int)((1f - Projectile.ai[2] / AttackDuration) * 12f), 0, 11)
            : 0;
        Rectangle source = new(0, frame * frameHeight, texture.Width, frameHeight);
        // Each module uses its own complete artwork, with its own transparent frame padding.
        ReadOnlySpan<float> laserCenters = [22, 20, 20, 18, 20, 24, 26, 24, 26, 24, 26, 24];
        ReadOnlySpan<float> missileCenters = [35, 37, 39, 39, 35, 33, 31, 30, 32, 34, 35, 35];
        Vector2 origin = Module switch
        {
            0 => new Vector2(15f, 18f),
            1 => new Vector2(15f, laserCenters[frame]),
            _ => new Vector2(23f, missileCenters[frame])
        };
        SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (flip != SpriteEffects.None) origin.X = texture.Width - origin.X;
        Vector2 center = VisualCenter - Main.screenPosition;
        float scale = VisualScale * (Recalling ? MathHelper.Lerp(0.65f, 1f,
            MathHelper.Clamp(Vector2.Distance(Projectile.Center, Main.player[Projectile.owner].MountedCenter) / 96f, 0f, 1f)) : 1f);
        Color color = MagallanVisuals.ModuleColor(Module);
        float glow = 0.65f + MathF.Sin(idleTimer * 0.06f) * 0.1f + (Boosted ? 0.25f : 0f);
        Texture2D outline = MagallanVisuals.OutlineMask(texture);
        for (int i = 0; i < 8; i++)
        {
            Vector2 offset = (i * MathHelper.PiOver4).ToRotationVector2() * 1.5f * VisualScale;
            Main.EntitySpriteDraw(outline, center + offset, source, color * glow, 0f, origin, scale, flip);
        }
        Main.EntitySpriteDraw(texture, center, source, Color.Lerp(lightColor, Color.White, 0.55f), 0f, origin, scale, flip);
        MagallanVisuals.Glow(Muzzle, color, Boosted ? 22f : 14f, 0.35f);
        return false;
    }
}
