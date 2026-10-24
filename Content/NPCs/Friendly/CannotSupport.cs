using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.NPCs.Friendly;

public sealed class CannotSleep : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.PotionSickness;
    public override void SetStaticDefaults() {
        Main.debuff[Type] = true;
        Main.buffNoSave[Type] = true;
    }
}

public sealed class CannotUndyingRage : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Wrath;
    public override void SetStaticDefaults() => Main.buffNoSave[Type] = true;
}

// Status and projectile ownership are per entity, and never applied to ordinary world spawns.
public sealed class CannotSupportNPC : GlobalNPC
{
    public override bool InstancePerEntity => true;
    public int SleepTicks;
    public int RageTicks;
    private int healTicks;
    private bool sleeping;
    private bool originalNoGravity;
    private bool originalInvulnerable;
    private bool originalImmortal;
    private bool rageProtected;
    private bool originalRageInvulnerable;
    private int contactBase;
    private int contactBoosted;
    private bool damageBoosted;
    private int attackParent = -1;
    private int attackParentType;
    private bool proxyEnraged;
    private NPC AttackParent => Main.npc.IndexInRange(attackParent) && Main.npc[attackParent].active &&
        Main.npc[attackParent].type == attackParentType ? Main.npc[attackParent] : null;

    public bool IsSleeping => SleepTicks > 0 || sleeping;
    public bool IsRaging => RageTicks > 0;

    public override void OnSpawn(NPC npc, IEntitySource source) {
        // CasterShoot is an NPC-backed projectile. It inherits attack effects, not wave ownership.
        if (source is EntitySource_Parent { Entity: NPC parent } &&
            parent.GetGlobalNPC<CannotSummonedTag>().OwnerCannot >= 0 && NPCID.Sets.ProjectileNPC[npc.type]) {
            attackParent = parent.whoAmI;
            attackParentType = parent.type;
            proxyEnraged = parent.GetGlobalNPC<CannotSupportNPC>().IsRaging;
            npc.netUpdate = true;
        }
    }

    public bool ApplySleep(NPC npc) {
        int buff = ModContent.BuffType<CannotSleep>();
        if (npc.buffImmune[buff] || npc.dontTakeDamage || npc.life <= 0) return false;
        originalNoGravity = npc.noGravity;
        originalInvulnerable = npc.dontTakeDamage;
        SleepTicks = 600;
        healTicks = 0;
        npc.AddBuff(buff, SleepTicks);
        npc.netUpdate = true;
        return true;
    }

    public bool ApplyRage(NPC npc) {
        if (npc.dontTakeDamage || npc.life <= 0) return false;
        originalImmortal = npc.immortal;
        originalRageInvulnerable = npc.dontTakeDamage;
        RageTicks = 720;
        npc.AddBuff(ModContent.BuffType<CannotUndyingRage>(), RageTicks);
        npc.netUpdate = true;
        return true;
    }

    public override bool PreAI(NPC npc) {
        RestoreContactDamage(npc);
        if (sleeping && SleepTicks <= 0) {
            npc.noGravity = originalNoGravity;
            npc.dontTakeDamage = originalInvulnerable;
            sleeping = false;
            npc.netUpdate = true;
        }
        if (rageProtected && (!IsRaging || npc.life > 1)) {
            npc.dontTakeDamage = originalRageInvulnerable;
            npc.immortal = originalImmortal;
            rageProtected = false;
            npc.netUpdate = true;
        }
        if (IsSleeping) {
            if (!sleeping) {
                sleeping = true;
            }
            npc.noGravity = true;
            npc.dontTakeDamage = true;
            npc.velocity = Vector2.Zero;
            return false;
        }
        if (IsRaging) {
            ProtectLastLife(npc);
            // An additional AI step advances cooldowns AND attack windups, so equality-based
            // firing frames cannot be skipped. Terraria still moves the entity only once.
            // Only these encounter-owned NPCs receive the extra step.
            if ((!npc.dontTakeDamage || rageProtected) && npc.ModNPC != null) {
                npc.ModNPC.AI();
                // Some legacy enemies fire on an animation frame rather than an AI timer.
                npc.ModNPC.FindFrame(npc.frame.Height);
            }
            ProtectLastLife(npc);
        }
        return true;
    }

    private void ProtectLastLife(NPC npc) {
        if (npc.life > 1) return;
        npc.life = 1;
        if (!rageProtected) {
            rageProtected = true;
        }
        // Prevent lethal hits from entering a ModNPC's delayed-death animation at 1 HP.
        npc.dontTakeDamage = true;
        npc.immortal = true;
    }

    private void RestoreContactDamage(NPC npc) {
        if (!damageBoosted) return;
        if (npc.damage == contactBoosted) npc.damage = contactBase;
        damageBoosted = false;
    }

    public override void PostAI(NPC npc) {
        if (SleepTicks > 0) {
            npc.velocity = Vector2.Zero;
            npc.lifeRegen = 0;
            if (++healTicks % 60 == 0 && Main.netMode != NetmodeID.MultiplayerClient) {
                int amount = CannotSupportMath.HealForSecond(npc.lifeMax, healTicks / 60);
                int healed = Math.Min(npc.lifeMax - npc.life, amount);
                npc.life += healed;
                if (healed > 0) npc.HealEffect(healed);
                npc.netUpdate = true;
            }
            SleepTicks--;
        }
        if (IsRaging) {
            npc.life = Math.Max(1, npc.life);
            ProtectLastLife(npc);
            contactBase = npc.damage;
            contactBoosted = contactBase * 2;
            npc.damage = contactBoosted;
            damageBoosted = true;
            RageTicks--;
        }
        if (!Main.dedServ && (IsSleeping || IsRaging) && Main.rand.NextBool(5)) {
            Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height,
                IsSleeping ? DustID.GreenTorch : DustID.RedTorch, 0f, -1f, 120, default, .9f);
            dust.noGravity = true;
        }
    }

    public override void UpdateLifeRegen(NPC npc, ref int damage) {
        if (IsSleeping || IsRaging && npc.life <= 1) {
            npc.lifeRegen = 0;
            npc.lifeRegenCount = 0;
            damage = 0;
        }
        else if (IsRaging && npc.lifeRegen < 0) {
            // DOT is processed before PreAI. Reserve 1 HP before it can trigger a delayed death.
            npc.lifeRegenCount = CannotSupportMath.ClampUndyingRegen(npc.lifeRegenCount, npc.lifeRegen, npc.life);
        }
    }

    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers) {
        if (IsRaging) modifiers.SetMaxDamage(Math.Max(1, npc.life - 1));
    }

    public override bool CheckDead(NPC npc) {
        if (!IsRaging) return true;
        npc.life = 1;
        npc.netUpdate = true;
        return false;
    }

    public override bool CanHitPlayer(NPC npc, Player target, ref int cooldownSlot) => !IsSleeping && AttackParent?.GetGlobalNPC<CannotSupportNPC>().IsSleeping != true;
    public override bool CanHitNPC(NPC npc, NPC target) => !IsSleeping && AttackParent?.GetGlobalNPC<CannotSupportNPC>().IsSleeping != true;
    public override void ModifyHitPlayer(NPC npc, Player target, ref Player.HurtModifiers modifiers) {
        if (proxyEnraged) modifiers.SourceDamage *= 2f;
    }
    public override bool? CanBeHitByItem(NPC npc, Player player, Item item) => IsSleeping || IsRaging && npc.life <= 1 ? false : null;
    public override bool? CanBeHitByProjectile(NPC npc, Projectile projectile) => IsSleeping || IsRaging && npc.life <= 1 ? false : null;
    public override bool CanBeHitByNPC(NPC npc, NPC attacker) => !IsSleeping && !(IsRaging && npc.life <= 1);

    public override void SendExtraAI(NPC npc, BitWriter bits, BinaryWriter writer) {
        writer.Write((short)SleepTicks);
        writer.Write((short)RageTicks);
        writer.Write((short)attackParent);
        writer.Write(attackParentType);
        writer.Write(proxyEnraged);
        writer.Write(originalNoGravity);
        writer.Write(originalInvulnerable);
        writer.Write(originalImmortal);
        writer.Write(originalRageInvulnerable);
    }

    public override void ReceiveExtraAI(NPC npc, BitReader bits, BinaryReader reader) {
        SleepTicks = reader.ReadInt16();
        RageTicks = reader.ReadInt16();
        attackParent = reader.ReadInt16();
        attackParentType = reader.ReadInt32();
        proxyEnraged = reader.ReadBoolean();
        originalNoGravity = reader.ReadBoolean();
        originalInvulnerable = reader.ReadBoolean();
        originalImmortal = reader.ReadBoolean();
        originalRageInvulnerable = reader.ReadBoolean();
    }

    public void Clear(NPC npc) {
        SleepTicks = RageTicks = 0;
        RestoreContactDamage(npc);
        if (sleeping) { npc.noGravity = originalNoGravity; npc.dontTakeDamage = originalInvulnerable; }
        if (rageProtected) { npc.dontTakeDamage = originalRageInvulnerable; npc.immortal = originalImmortal; }
        sleeping = rageProtected = false;
        int buff = npc.FindBuffIndex(ModContent.BuffType<CannotSleep>());
        if (buff >= 0) npc.DelBuff(buff);
        buff = npc.FindBuffIndex(ModContent.BuffType<CannotUndyingRage>());
        if (buff >= 0) npc.DelBuff(buff);
        npc.netUpdate = true;
    }
}

public sealed class CannotSupportProjectile : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private int sourceNPC = -1;
    private int sourceType;
    private int ownerCannot = -1;
    private bool enraged;
    private NPC Source => Main.npc.IndexInRange(sourceNPC) && Main.npc[sourceNPC].active &&
        Main.npc[sourceNPC].type == sourceType && Main.npc[sourceNPC].GetGlobalNPC<CannotSummonedTag>().OwnerCannot == ownerCannot
        ? Main.npc[sourceNPC] : null;

    public override void OnSpawn(Projectile projectile, IEntitySource source) {
        if (source is not EntitySource_Parent parent) return;
        if (parent.Entity is NPC npc) {
            int owner = npc.GetGlobalNPC<CannotSummonedTag>().OwnerCannot;
            if (owner < 0) return;
            sourceNPC = npc.whoAmI;
            sourceType = npc.type;
            ownerCannot = owner;
            enraged = npc.GetGlobalNPC<CannotSupportNPC>().IsRaging;
        }
        else if (parent.Entity is Projectile other) {
            var inherited = other.GetGlobalProjectile<CannotSupportProjectile>();
            sourceNPC = inherited.sourceNPC;
            sourceType = inherited.sourceType;
            ownerCannot = inherited.ownerCannot;
            enraged = inherited.enraged;
        }
        if (ownerCannot < 0) return;
        // Legacy enemy AI also runs on clients. Only the server creates encounter attacks.
        if (Main.netMode == NetmodeID.MultiplayerClient) projectile.active = false;
        else projectile.netUpdate = true;
    }

    public override bool? CanDamage(Projectile projectile) => Source?.GetGlobalNPC<CannotSupportNPC>().IsSleeping == true ? false : null;
    public override void ModifyHitPlayer(Projectile projectile, Player target, ref Player.HurtModifiers modifiers) {
        if (enraged) modifiers.SourceDamage *= 2f;
    }
    public override void SendExtraAI(Projectile projectile, BitWriter bits, BinaryWriter writer) {
        writer.Write((short)sourceNPC); writer.Write(sourceType); writer.Write((short)ownerCannot); writer.Write(enraged);
    }
    public override void ReceiveExtraAI(Projectile projectile, BitReader bits, BinaryReader reader) {
        sourceNPC = reader.ReadInt16(); sourceType = reader.ReadInt32(); ownerCannot = reader.ReadInt16(); enraged = reader.ReadBoolean();
    }
}

// A short, harmless spray carries the visible cast to all clients.
public sealed class CannotPotionSpray : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.None;
    public override void SetDefaults() {
        Projectile.width = Projectile.height = 8;
        Projectile.timeLeft = 45;
        Projectile.tileCollide = false;
        Projectile.penetrate = -1;
    }
    public override bool? CanDamage() => false;
    public override void AI() {
        Projectile.velocity *= .96f;
        if (Main.dedServ) return;
        if (Projectile.localAI[0]++ == 0) SoundEngine.PlaySound(SoundID.Item13 with { Volume = .65f }, Projectile.Center);
        for (int i = 0; i < 4; i++) {
            Vector2 offset = Main.rand.NextVector2Circular(30f, 18f);
            var dust = Dust.NewDustPerfect(Projectile.Center + offset,
                Projectile.ai[0] == 0 ? DustID.GreenTorch : DustID.RedTorch,
                Projectile.velocity + Main.rand.NextVector2Circular(2f, 2f), 120, default, 1.2f);
            dust.noGravity = true;
        }
    }
    public override bool PreDraw(ref Color lightColor) => false;
}
