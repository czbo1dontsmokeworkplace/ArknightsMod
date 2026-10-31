using System;
using System.Collections.Generic;
using System.IO;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Systems.Gameplay.Damage;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

internal enum DefenderAreaKind { Impact, Electric, Flame, Magnet, Glue, Necrosis, Nervous, Corrosion, Burn, Frost, Saw, Arts }

// An authored field snapshots its source weapon: switching items cannot change damage class or element.
public sealed class DefenderSkillArea : ModProjectile
{
    private int age, period = 60, lifetime = 24;
    private bool follow;
    private readonly HashSet<int> hits = new();
    private DefenderAreaKind Kind => (DefenderAreaKind)(int)Projectile.ai[0];
    private float Radius => Projectile.ai[1];
    private bool Orbit => follow && Terraria.ID.ContentSamples.ItemsByType.TryGetValue((int)Projectile.ai[2], out Item item)
        && item.ModItem?.Name == "HoshigumaBlazingBlade";
    private Vector2 HitCenter => Orbit ? Projectile.Center + (age * .09f).ToRotationVector2() * Radius : Projectile.Center;
    private Player Owner => Main.player[Projectile.owner];
    private bool Arts => Kind is DefenderAreaKind.Electric or DefenderAreaKind.Flame or DefenderAreaKind.Magnet
        or DefenderAreaKind.Necrosis or DefenderAreaKind.Nervous or DefenderAreaKind.Burn or DefenderAreaKind.Arts;
    public override string Texture => "Terraria/Images/MagicPixel";
    internal static void Spawn(Player owner, Vector2 center, int damage, float radius, DefenderAreaKind kind,
        int lifetime = 24, int period = 60, bool follow = false, int sourceItemType = 0, int crit = -1)
    {
        if (owner.whoAmI != Main.myPlayer) return;
        if (kind == DefenderAreaKind.Flame)
            foreach (Projectile existing in Main.ActiveProjectiles)
                if (existing.owner == owner.whoAmI && existing.ModProjectile is DefenderSkillArea fire
                    && fire.Kind == kind && Vector2.DistanceSquared(existing.Center, center) < radius * radius * .25f)
                {
                    fire.age %= fire.period;
                    fire.lifetime = lifetime + fire.age; existing.timeLeft = lifetime;
                    existing.netUpdate = true;
                    return;
                }
        int index = Projectile.NewProjectile(owner.GetSource_Misc("DefenderSkillArea"), center, Vector2.Zero,
            ModContent.ProjectileType<DefenderSkillArea>(), Math.Max(1, damage), 5f, owner.whoAmI,
            (int)kind, radius, sourceItemType > 0 ? sourceItemType : owner.HeldItem.type);
        if (!Main.projectile.IndexInRange(index)) return;
        var field = (DefenderSkillArea)Main.projectile[index].ModProjectile;
        field.lifetime = Math.Clamp(lifetime, 12, 3600);
        field.period = Math.Max(15, period);
        field.follow = follow;
        field.Projectile.timeLeft = field.lifetime;
        field.Projectile.CritChance = crit >= 0 ? crit : owner.GetWeaponCrit(owner.HeldItem);
        field.Projectile.netUpdate = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.friendly = true; Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.penetrate = -1; Projectile.timeLeft = 24; Projectile.netImportant = true;
        Projectile.usesLocalNPCImmunity = true; Projectile.localNPCHitCooldown = 15;
    }
    public override void SendExtraAI(BinaryWriter writer)
    { writer.Write(lifetime); writer.Write(period); writer.Write(follow); writer.Write(age); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        lifetime = Math.Clamp(reader.ReadInt32(), 12, 3600); period = Math.Max(15, reader.ReadInt32());
        follow = reader.ReadBoolean(); age = Math.Clamp(reader.ReadInt32(), 0, lifetime);
        Projectile.timeLeft = lifetime - age;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (!Owner.active || Owner.dead) { Projectile.Kill(); return; }
        if (follow)
        {
            if (Owner.HeldItem.type != (int)Projectile.ai[2] || !DefenderRules.SkillActive(Owner))
            { Projectile.Kill(); return; }
            Projectile.Center = Owner.MountedCenter;
        }
        Projectile.DamageType = Arts ? DamageClass.Magic : DamageClass.Melee;
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = Arts;
        age++;
        bool pulse = age % period == 1;
        if (pulse) hits.Clear();
        // Status/elemental accumulation belongs to the server. Projectile damage remains owner-authoritative.
        if (Main.netMode != NetmodeID.MultiplayerClient && (pulse || Kind is DefenderAreaKind.Magnet or DefenderAreaKind.Glue))
        {
            HashSet<int> groups = new();
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile) || !Touches(npc.Hitbox)) continue;
                if (Kind is DefenderAreaKind.Magnet or DefenderAreaKind.Glue)
                {
                    // Do not suppress Boss AI, timers or projectiles, or pull segmented bosses.
                    if (!npc.boss && npc.realLife < 0 && npc.knockBackResist > 0f)
                    {
                        if (Kind == DefenderAreaKind.Magnet)
                            npc.velocity += (Projectile.Center - npc.Center).SafeNormalize(Vector2.Zero) * .12f * npc.knockBackResist;
                        else npc.AddBuff(BuffID.Slow, 30);
                    }
                }
                if (!pulse || !groups.Add(npc.realLife >= 0 ? npc.realLife : npc.whoAmI)) continue;
                if (Kind == DefenderAreaKind.Electric && Terraria.ID.ContentSamples.ItemsByType.TryGetValue((int)Projectile.ai[2], out Item item)
                    && item.ModItem?.Name == "BlitzPistol" && !npc.boss && npc.realLife < 0) npc.AddBuff(BuffID.Confused, 180);
                if (Kind == DefenderAreaKind.Flame) npc.AddBuff(BuffID.OnFire3, 120);
                DefenderRouteEffects.AddElement(npc, Kind, Math.Max(1, Projectile.damage / 3));
            }
        }
        Color color = Kind switch
        {
            DefenderAreaKind.Flame or DefenderAreaKind.Burn => new Color(255, 135, 65),
            DefenderAreaKind.Electric or DefenderAreaKind.Magnet => new Color(130, 210, 255),
            DefenderAreaKind.Necrosis => new Color(170, 105, 210),
            DefenderAreaKind.Nervous => new Color(240, 195, 95),
            DefenderAreaKind.Glue or DefenderAreaKind.Corrosion => new Color(155, 205, 125),
            _ => new Color(205, 185, 150)
        };
        DefenderSkillVisuals.Field(Projectile.Center, Radius, Kind, color, age);
        if (age == 1) DefenderSkillVisuals.Burst(Projectile.Center, color, 12);
    }
    private bool Touches(Rectangle box) => Vector2.DistanceSquared(HitCenter,
        Vector2.Clamp(HitCenter, box.TopLeft(), box.BottomRight())) <= (Orbit ? 45f * 45f : Radius * Radius)
        && Collision.CanHitLine(Projectile.Center, 1, 1, box.TopLeft(), box.Width, box.Height);
    public override bool? CanDamage() => age % period == 1
        && !(Projectile.damage <= 1 && Kind == DefenderAreaKind.Electric);
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) => Touches(targetHitbox);
    public override bool? CanHitNPC(NPC target)
    {
        if (hits.Contains(target.realLife >= 0 ? target.realLife : target.whoAmI)) return false;
        // Overlapping fire patches are one burn source per owner, not an unlimited stacking DoT.
        if (Kind == DefenderAreaKind.Flame)
            foreach (Projectile other in Main.ActiveProjectiles)
                if (other.owner == Projectile.owner && other.identity < Projectile.identity
                    && other.ModProjectile is DefenderSkillArea field && field.Kind == Kind && field.Touches(target.Hitbox)) return false;
        return null;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hits.Add(target.realLife >= 0 ? target.realLife : target.whoAmI);
        if (follow && Kind == DefenderAreaKind.Arts && Owner.HeldItem.ModItem?.Name == "HoshigumaBlazingBlade")
            Owner.GetModPlayer<DefenderCombatPlayer>().HealSelf(System.Math.Min(.04f, damageDone * .15f / System.Math.Max(1, Owner.statLifeMax2)), 30);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        if (Orbit)
        {
            var texture = ModContent.Request<Microsoft.Xna.Framework.Graphics.Texture2D>(
                "ArknightsMod/Content/Projectiles/Defender/Nian/NianShield").Value;
            Main.EntitySpriteDraw(texture, HitCenter - Main.screenPosition, null, Color.White,
                age * .15f, texture.Size() * .5f, .9f, Microsoft.Xna.Framework.Graphics.SpriteEffects.None);
        }
        OperatorTextureEffects.Glow(Projectile.Center, Math.Min(220f, Radius * 1.4f),
            Arts ? new Color(180, 145, 225) : new Color(205, 180, 135),
            Math.Min(1f, Projectile.timeLeft / 15f) * .08f);
        return false;
    }
}
