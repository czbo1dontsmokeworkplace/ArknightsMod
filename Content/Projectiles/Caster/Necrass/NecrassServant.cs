using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster.Necrass;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass;

public sealed class NecrassServant : ModProjectile
{
    private const string SmallTexture = "ArknightsMod/Content/Projectiles/Caster/Necrass/LamentingServant";
    private const string LargeTexture = "ArknightsMod/Content/Projectiles/Caster/Necrass/CrownedServant";
    public override string Texture => SmallTexture;
    internal bool Special => Projectile.ai[0] == 1;
    internal int Level => Math.Clamp((int)Projectile.ai[1], 0, Special ? 6 : 1);
    internal bool Dying => state == 4;
    internal float HealthRatio => health / (float)MaxHealth;
    private int MaxHealth => Special ? 780 + Level * 180 : (Level == 0 ? 260 : 520);
    private int health, timer, hurtCooldown, attackNumber;
    private byte state;
    private bool initialized;
    private int flash;
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 5;
        ProjectileID.Sets.TrailingMode[Type] = 0;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 34;
        Projectile.height = 48;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.netImportant = true;
        Projectile.timeLeft = 180;
        Projectile.DamageType = DamageClass.Magic;
        // 塑灵仆役有自己的三名上限；不占普通召唤栏，也不使用接触伤害。
    }
    public override bool? CanDamage() => false;
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(state); writer.Write((short)timer); writer.Write(health); writer.Write((short)attackNumber);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        state = reader.ReadByte(); timer = reader.ReadInt16(); health = reader.ReadInt32(); attackNumber = reader.ReadInt16();
        initialized = true;
    }
    internal void Dismiss()
    {
        if (Dying) return;
        state = 4; timer = 0; health = 0;
        Projectile.netUpdate = true;
    }
    internal void Upgrade(int amount, bool fullHeal)
    {
        if (Dying || !NecrassCourt.Authority) return;
        Projectile.ai[1] = Math.Min(Special ? 6 : 1, Level + amount);
        health = fullHeal ? MaxHealth : Math.Min(MaxHealth, health + (int)(MaxHealth * .2f * amount));
        Projectile.netUpdate = true;
        NecrassImpact.Spawn(Projectile, Projectile.Center, Vector2.Zero, 0, 5, 62 + Level * 5);
    }
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!initialized) { initialized = true; health = MaxHealth; }
        if ((!owner.active || owner.dead || owner.HeldItem.ModItem is not NecrassScepter)
            && (NecrassCourt.Authority || Projectile.owner == Main.myPlayer)) Dismiss();
        Projectile.timeLeft = 180;
        timer++;
        if (flash > 0) flash--;
        if (Dying)
        {
            Projectile.velocity *= .82f;
            if (timer == 1) NecrassVisuals.Embers(Projectile.Center, 16, 2);
            if (timer >= 36) Projectile.Kill();
            return;
        }
        if (!Main.dedServ)
        {
            Lighting.AddLight(Projectile.Center, .2f, .06f, .35f);
            if (Main.GameUpdateCount % 9 == (ulong)(Projectile.whoAmI % 9))
                NecrassVisuals.Embers(Projectile.Center + new Vector2(0, -14), 1, .8f);
        }
        if (state == 0)
        {
            Projectile.velocity *= .8f;
            if (timer >= (Special ? 32 : 36)) ChangeState(1);
            return;
        }
        if (NecrassCourt.Authority) TakeContactDamage();
        if (Dying) return;
        NPC target = null;
        int targetIndex = (int)Projectile.ai[2] - 1;
        if (targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex].CanBeChasedBy()) target = Main.npc[targetIndex];
        if (NecrassCourt.Authority && state == 1 && (timer % 15 == 0 || target == null))
        {
            target = NecrassCourt.Target(Projectile.Center, 650, owner);
            float next = target == null ? 0 : target.whoAmI + 1;
            if (Projectile.ai[2] != next) { Projectile.ai[2] = next; Projectile.netUpdate = true; }
        }
        if (state is 2 or 3)
        {
            Projectile.velocity *= .72f;
            if (target != null) Projectile.spriteDirection = target.Center.X >= Projectile.Center.X ? 1 : -1;
            int strikeFrame = state == 3 ? 12 : 15;
            if (timer == strikeFrame && NecrassCourt.Authority && target != null
                && target.Distance(Projectile.Center) < (Special ? 200 + Level * 12 : 135)
                && Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height)) Strike(target);
            if (timer >= (Special ? 48 : 44)) ChangeState(1);
            return;
        }
        Vector2 destination;
        if (target != null && target.Distance(owner.Center) < 950)
            destination = target.Center + new Vector2((Projectile.Center.X < target.Center.X ? -1 : 1) * (Special ? 85 : 56), 8);
        else
        {
            int slot = 0;
            foreach (var s in NecrassCourt.Servants(owner.whoAmI))
                if (s.Projectile.whoAmI < Projectile.whoAmI) slot++;
            destination = owner.Center + new Vector2(-owner.direction * (65 + slot * 54), 8);
        }
        if (Projectile.Distance(owner.Center) > 1400)
        {
            Projectile.Center = owner.Center;
            Projectile.velocity = Vector2.Zero;
            Projectile.netUpdate = true;
            NecrassVisuals.Embers(Projectile.Center, 16, 3);
        }
        Vector2 delta = destination - Projectile.Center;
        // 灵体通过悬浮跟随适应平台与空中敌人；隔墙时仅能移动，不能造成近战伤害。
        float speed = target != null ? (Special ? 8 : 10) : 12;
        Vector2 wanted = delta.SafeNormalize(Vector2.Zero) * Math.Min(speed, delta.Length() * .12f);
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, wanted, .14f);
        if (Math.Abs(Projectile.velocity.X) > .25f) Projectile.spriteDirection = Projectile.velocity.X > 0 ? 1 : -1;
        if (NecrassCourt.Authority && target != null && timer > 10
            && Projectile.Distance(target.Center) < (Special ? 145 : 100)
            && Collision.CanHitLine(Projectile.Center, 1, 1, target.position, target.width, target.height))
        {
            attackNumber++;
            ChangeState(Special && Level >= 3 && attackNumber % 3 == 0 ? (byte)3 : (byte)2);
        }
    }
    private void ChangeState(byte next)
    {
        state = next; timer = 0;
        if (NecrassCourt.Authority) Projectile.netUpdate = true;
    }
    private void Strike(NPC target)
    {
        Projectile court = NecrassCourt.Find(Projectile.owner);
        if (court?.ModProjectile is not NecrassCourt data) return;
        float multiplier = Special ? 1.4f + Level * .4f : .65f * (Level == 0 ? 1 : data.Elite == 2 ? 2 : 1.5f);
        Vector2 aim = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX * Projectile.spriteDirection);
        int reach = Special ? 160 + (Level / 3) * 40 : 105;
        NecrassImpact.Spawn(Projectile, Projectile.Center, aim, (int)(court.damage * multiplier), 2, reach);
        if (state == 3)
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, aim * 15,
                ModContent.ProjectileType<NecrassSoulBolt>(), (int)(court.damage * multiplier * .55f), 4, Projectile.owner, 1);
    }
    private void TakeContactDamage()
    {
        if (hurtCooldown > 0) { hurtCooldown--; return; }
        int hit = 0;
        foreach (NPC npc in Main.ActiveNPCs)
            if (!npc.friendly && npc.damage > 0 && !npc.dontTakeDamage && npc.Hitbox.Intersects(Projectile.Hitbox))
            { hit = Math.Max(hit, npc.damage); }
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.hostile && p.damage > 0 && p.Hitbox.Intersects(Projectile.Hitbox)) hit = Math.Max(hit, p.damage);
        if (hit == 0) return;
        health -= Math.Max(1, hit - (Special ? 32 + Level * 6 : 12 + Level * 12));
        hurtCooldown = 30; flash = 10; Projectile.netUpdate = true;
        if (health <= 0) Dismiss();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Special ? LargeTexture : SmallTexture).Value;
        int frame;
        if (Dying) frame = Special ? 31 + Math.Min(8, timer / 4) : 19;
        else if (state == 0) frame = Math.Min(Special ? 7 : 8, timer / 4);
        else if (state == 2) frame = (Special ? 8 : 9) + Math.Min(10, timer / 3);
        else if (state == 3) frame = 19 + Math.Min(11, timer / 3);
        else frame = Special ? 30 : 19;
        int height = Special ? 200 : 160;
        Rectangle source = new(0, frame * height, texture.Width, height);
        Vector2 origin = Special ? new Vector2(92, 138) : new Vector2(59, 103);
        SpriteEffects effect = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        if (effect != SpriteEffects.None) origin.X = texture.Width - origin.X;
        float scale = Special ? .88f + Level * .012f : .88f;
        float opacity = Dying ? 1 - timer / 36f : 1;
        Vector2 feet = Projectile.Bottom + new Vector2(0, MathF.Sin(Main.GlobalTimeWrappedHourly * 3 + Projectile.identity) * 2);
        NecrassVisuals.Seal(feet + new Vector2(0, 4), Special ? 44 : 28, Main.GlobalTimeWrappedHourly, opacity * .45f);
        Color color = Color.Lerp(lightColor, Color.White, .48f) * opacity;
        for (int i = 3; i > 0; i--)
        {
            Vector2 offset = new Vector2(MathF.Sin(Main.GlobalTimeWrappedHourly * 2 + i) * 2, -i * 2);
            Main.EntitySpriteDraw(texture, feet - Main.screenPosition + offset, source,
                NecrassVisuals.Glow(NecrassVisuals.Violet, opacity * .09f), 0, origin, scale, effect);
        }
        Main.EntitySpriteDraw(texture, feet - Main.screenPosition, source, color, 0, origin, scale, effect);
        if (flash > 0) Main.EntitySpriteDraw(texture, feet - Main.screenPosition, source,
            NecrassVisuals.Glow(Color.White, flash / 18f), 0, origin, scale, effect);
        if (Special || Level > 0)
            NecrassVisuals.Crown(feet + new Vector2(0, Special ? -84 : -65), Special ? 20 : 12,
                Main.GlobalTimeWrappedHourly, opacity, Special ? Level : 1);
        if (HealthRatio < .99f && !Dying) NecrassVisuals.Health(feet + new Vector2(0, 12), HealthRatio, Special ? 40 : 28);
        return false;
    }
}
