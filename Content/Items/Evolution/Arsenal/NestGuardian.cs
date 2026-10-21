using System;
using System.IO;
using ArknightsMod.Content.NPCs.Enemy.Evolution;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Evolution.Arsenal;

public sealed class NestGuardianBuff : ModBuff
{
    public override string LocalizationCategory => "Buffs.EvolutionArsenal";
    public override string Texture => EvolutionVisuals.Root + "CompanionBuffIcon";
    public override void SetStaticDefaults() { Main.buffNoTimeDisplay[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<NestGuardian>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public sealed partial class NestGuardian : EvolutionArsenalProjectile
{
    private Vector2 dashVelocity;
    private bool dashActive;
    private int jumpCooldown;
    internal bool Dashing => dashActive;
    internal int Tick => (int)Projectile.ai[1] % EvolutionArsenalRules.GuardianCycle;
    internal NPC Enemy => Projectile.ai[0] > 0 && Projectile.ai[0] <= Main.maxNPCs && Main.npc[(int)Projectile.ai[0] - 1].CanBeChasedBy()
        ? Main.npc[(int)Projectile.ai[0] - 1] : null;
    internal Vector2 BladeTip
    {
        get
        {
            Vector2 direction = new(Projectile.spriteDirection, 0);
            float angle = MathHelper.Lerp(-1.5f, 1.2f, EvolutionArsenalRules.SwingEase((Tick - 14) / 20f));
            return Projectile.Center + direction.RotatedBy(angle * Projectile.spriteDirection) * 155;
        }
    }
    public override string Texture => EvolutionVisuals.Root + "Abomination";
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 19;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true; ProjectileID.Sets.MinionSacrificable[Type] = true;
        ProjectileID.Sets.TrailCacheLength[Type] = 8; ProjectileID.Sets.TrailingMode[Type] = 2;
    }
    public override void SetDefaults()
    {
        Projectile.width = 64; Projectile.height = 50; Projectile.friendly = true; Projectile.minion = true; Projectile.minionSlots = 2;
        Projectile.DamageType = DamageClass.Summon; Projectile.penetrate = -1; Projectile.netImportant = true;
        Projectile.tileCollide = true; Projectile.ignoreWater = true; Projectile.timeLeft = 2;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 20;
    }
    public override bool MinionContactDamage() => true;
    public override bool? CanDamage() => Enemy != null && (EvolutionArsenalRules.GuardianSlash(Tick) || dashActive && EvolutionArsenalRules.GuardianDash(Tick));
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        => EvolutionArsenalRules.GuardianSlash(Tick) ?
            Collision.CanHitLine(Projectile.Center, 1, 1, targetHitbox.TopLeft(), targetHitbox.Width, targetHitbox.Height) && EvolutionArsenalRules.Capsule(targetHitbox, Projectile.Center, BladeTip, 30)
            : projHitbox.Intersects(targetHitbox);
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(dashVelocity.X); writer.Write(dashVelocity.Y); writer.Write(dashActive); writer.Write(jumpCooldown);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        dashVelocity = new Vector2(reader.ReadSingle(), reader.ReadSingle()); dashActive = reader.ReadBoolean(); jumpCooldown = reader.ReadInt32();
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; }
        if (owner.dead) owner.ClearBuff(ModContent.BuffType<NestGuardianBuff>());
        if (!owner.dead && owner.HasBuff<NestGuardianBuff>()) Projectile.timeLeft = 2;
        if (owner.dead) return;
        bool authority = Projectile.owner == Main.myPlayer;
        Projectile.localAI[0]++;
        if (authority && (!dashActive || Enemy == null) && ((int)Projectile.localAI[0] % 12 == 1 || Projectile.ai[0] > 0 && Enemy == null))
        {
            NPC selected = EvolutionArsenalRules.Target(Projectile.Center, 1200, owner, true);
            int target = selected == null ? 0 : selected.whoAmI + 1;
            if (Projectile.ai[0] != target)
            {
                Projectile.ai[0] = target;
                Projectile.ai[1] = Projectile.minionPos * 7 % 14;
                dashVelocity = Vector2.Zero; dashActive = false; Projectile.netUpdate = true;
            }
        }
        NPC enemy = Enemy;
        if (enemy != null && Vector2.DistanceSquared(owner.Center, enemy.Center) > 1600 * 1600) enemy = null;
        Projectile.ai[1] = (Projectile.ai[1] + 1) % EvolutionArsenalRules.GuardianCycle;
        int tick = Tick;
        Vector2 idle = owner.Bottom + new Vector2(-owner.direction * (90 + Projectile.minionPos * 70), -Projectile.height * .5f);
        if (Projectile.Distance(owner.Center) > 2400 && authority && TryFindGround(idle, out Vector2 landing))
        {
            Projectile.Center = landing; Projectile.velocity = Vector2.Zero; Projectile.ai[0] = 0; Projectile.ai[1] = 0;
            dashActive = false; dashVelocity = Vector2.Zero;
            Array.Clear(Projectile.oldPos); Projectile.netUpdate = true; return;
        }
        GroundMovement(owner, enemy, idle, tick, authority);
        if (!dashActive)
            foreach (Projectile other in Main.ActiveProjectiles)
                if (other.whoAmI != Projectile.whoAmI && other.owner == Projectile.owner && other.type == Type && Projectile.Distance(other.Center) < 60)
                    Projectile.velocity.X += Math.Sign(Projectile.Center.X - other.Center.X + (Projectile.whoAmI % 2 == 0 ? .1f : -.1f)) * .3f;
        if (enemy != null && EvolutionArsenalRules.GuardianSpits(tick))
        {
            Vector2 muzzle = Projectile.Center + new Vector2(Projectile.spriteDirection * 24, -18);
            if (authority)
                for (int i = -1; i <= 1; i += 2)
                    Projectile.NewProjectile(Projectile.GetSource_FromAI(), muzzle, (enemy.Center - muzzle).SafeNormalize(Vector2.UnitX).RotatedBy(i * .16f) * 10,
                        ModContent.ProjectileType<NestSpore>(), (int)(Projectile.damage * .6f), 2, Projectile.owner);
            if (!Main.dedServ) EvolutionWeaponVisuals.Sparks(muzzle, 5, 2);
        }
        bool attacking = enemy != null && tick >= 14 && tick <= 34;
        Projectile.frame = attacking ? 8 + Math.Min(7, (tick - 14) * 8 / 21) : Math.Abs(Projectile.velocity.X) > .5f ? (int)Projectile.localAI[0] / 4 % 8 : 0;
        Projectile.rotation = 0;
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = EvolutionVisuals.Asset("Abomination"), mask = EvolutionBroodOutlineVisuals.Mask("NestGuardian", texture);
        Rectangle frame = texture.Frame(1, 19, 0, Projectile.frame);
        SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        Vector2 position = Projectile.Center - Main.screenPosition, origin = frame.Size() * .5f;
        if (dashActive && Enemy != null)
            for (int i = 1; i < Projectile.oldPos.Length; i += 2)
                if (Projectile.oldPos[i] != Vector2.Zero)
                    Main.EntitySpriteDraw(mask, Projectile.oldPos[i] + Projectile.Size * .5f - Main.screenPosition, frame,
                        (EvolutionWeaponVisuals.Blood with { A = 0 }) * (.35f * (1 - i / 8f)), Projectile.rotation, origin, .95f, flip);
        for (int i = 0; i < 8; i++)
            Main.EntitySpriteDraw(mask, position + (i * MathHelper.TwoPi / 8).ToRotationVector2() * 2,
                frame, (EvolutionWeaponVisuals.Blood with { A = 0 }) * .65f, Projectile.rotation, origin, .95f, flip);
        Main.EntitySpriteDraw(texture, position, frame, Color.Lerp(lightColor, Color.White, .65f), Projectile.rotation, origin, .95f, flip);
        if (Enemy != null && Tick >= 14 && Tick <= 34 && !EvolutionWeaponRenderer.Ready)
            DrawClawArc(false);
        return false;
    }
    internal override void DrawMaterial()
    {
        if (Enemy == null) return;
        if (Tick >= 14 && Tick <= 34) DrawClawArc(true);
        if (dashActive)
            EvolutionWeaponVisuals.Ribbon(Projectile.Center - Projectile.velocity * 3, Projectile.Center, 34, .55f, Projectile.identity);
    }
    private void DrawClawArc(bool material)
    {
        float p = Math.Clamp((Tick - 14) / 20f, 0, 1), opacity = MathF.Sin(MathHelper.Pi * p);
        float angle = MathHelper.Lerp(-1.5f, 1.2f, EvolutionArsenalRules.SwingEase(p));
        Vector2 facing = new(Projectile.spriteDirection, 0);
        Vector2 previous = Projectile.Center + facing.RotatedBy((angle - .8f) * Projectile.spriteDirection) * 135;
        for (int i = 1; i <= 12; i++)
        {
            float f = i / 12f;
            Vector2 point = Projectile.Center + facing.RotatedBy((angle - .8f * (1 - f)) * Projectile.spriteDirection) * (135 + f * 20);
            if (material) EvolutionWeaponVisuals.Ribbon(previous, point, 22 * f, opacity * f, Projectile.identity, (i - 1) / 12f, 1 / 12f);
            else EvolutionWeaponVisuals.FallbackLine(previous, point, 10 * f, opacity * f);
            previous = point;
        }
    }
}
