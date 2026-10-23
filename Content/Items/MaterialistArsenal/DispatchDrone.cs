using System;
using System.IO;
using ArknightsMod.Content.NPCs.Enemy.MaterialistAntagonizer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

public sealed class DispatchDroneBuff : ModBuff
{
    public override string LocalizationCategory => "Buffs.MaterialistArsenal";
    public override string Texture => $"Terraria/Images/Item_{ItemID.XenoStaff}";
    public override void SetStaticDefaults() { Main.buffNoTimeDisplay[Type] = true; Main.buffNoSave[Type] = true; }
    public override void Update(Player player, ref int buffIndex)
    {
        if (player.ownedProjectileCounts[ModContent.ProjectileType<DispatchDrone>()] > 0) player.buffTime[buffIndex] = 18000;
        else { player.DelBuff(buffIndex); buffIndex--; }
    }
}

public sealed class DispatchDrone : ModProjectile
{
    private Item ammoWeapon;
    private int age, targetType, shotSerial, visualSerial, followUpAge;
    private Vector2 shotDirection = Vector2.UnitX;
    private float muzzleFlash;
    public override string LocalizationCategory => "Projectiles.MaterialistArsenal";
    public override string Texture => "ArknightsMod/Content/NPCs/Enemy/ThroughChapter4/Drone";
    public override void SetStaticDefaults()
    {
        Main.projFrames[Type] = 2;
        ProjectileID.Sets.MinionTargettingFeature[Type] = true;
        ProjectileID.Sets.MinionSacrificable[Type] = true;
    }
    public override void SetDefaults()
    {
        Projectile.width = 40; Projectile.height = 30; Projectile.friendly = true;
        Projectile.minion = true; Projectile.minionSlots = 1; Projectile.DamageType = DamageClass.Summon;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.penetrate = -1;
        Projectile.netImportant = true; Projectile.timeLeft = 2;
    }
    public override void OnSpawn(IEntitySource source)
    {
        if (source is IEntitySource_WithStatsFromItem itemSource && itemSource.Item.type == ModContent.ItemType<FighterDispatchTerminal>())
            ammoWeapon = itemSource.Item.Clone();
    }
    public override bool MinionContactDamage() => false;
    public override bool? CanDamage() => false;
    private NPC Enemy => Projectile.ai[0] > 0 && Projectile.ai[0] <= Main.maxNPCs &&
        Main.npc[(int)Projectile.ai[0] - 1] is NPC npc && npc.type == targetType && npc.CanBeChasedBy() ? npc : null;
    private Vector2 Muzzle => Projectile.Center + new Vector2(Projectile.spriteDirection * 17, 8).RotatedBy(Projectile.rotation);
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        if (!owner.active) { Projectile.Kill(); return; }
        if (owner.dead) owner.ClearBuff(ModContent.BuffType<DispatchDroneBuff>());
        if (!owner.dead && owner.HasBuff<DispatchDroneBuff>()) Projectile.timeLeft = 2;
        if (owner.dead) return;
        age++; Projectile.ai[1]++;
        int slot = 0;
        foreach (Projectile other in Main.ActiveProjectiles)
            if (other.owner == Projectile.owner && other.type == Type && other.identity < Projectile.identity) slot++;
        bool authority = Projectile.owner == Main.myPlayer;
        if (authority && (age % 12 == 1 || Projectile.ai[0] > 0 && Enemy == null))
        {
            NPC target = MaterialistArsenalRules.Target(Projectile.Center, 1000, owner, true, (int)Projectile.ai[0] - 1);
            int selected = target?.whoAmI + 1 ?? 0;
            if (selected != (int)Projectile.ai[0] || target != null && target.type != targetType)
            {
                Projectile.ai[0] = selected; targetType = target?.type ?? 0; Projectile.netUpdate = true;
            }
        }
        NPC enemy = Enemy;
        int cycle = (int)Projectile.ai[1], mode = (cycle / 180 + slot) % 3;
        Vector2 idle = owner.Center + new Vector2((slot % 3 - 1) * 72 - owner.direction * 35, -85 - slot / 3 * 45);
        Vector2 goal = idle;
        if (enemy != null)
        {
            float side = slot % 2 == 0 ? -1 : 1;
            goal = mode switch
            {
                0 => enemy.Center + new Vector2(MathF.Cos(cycle * .025f + slot * 2.4f) * 230,
                    -110 + MathF.Sin(cycle * .05f + slot) * 85),
                1 => enemy.Center + new Vector2(side * (180 + slot / 2 * 28), -90 - slot % 3 * 44),
                _ => enemy.Center + new Vector2(MathF.Sin(cycle * .035f + slot) * 320,
                    -100 - MathF.Cos(cycle * .07f + slot) * 75)
            };
            Projectile.spriteDirection = enemy.Center.X >= Projectile.Center.X ? 1 : -1;
        }
        else if (Math.Abs(Projectile.velocity.X) > .3f) Projectile.spriteDirection = Projectile.velocity.X > 0 ? 1 : -1;
        goal = Vector2.Clamp(goal, new Vector2(32), new Vector2(Main.maxTilesX * 16 - 32, Main.maxTilesY * 16 - 32));
        if (authority && Projectile.Distance(owner.Center) > 2000)
        {
            Projectile.Center = idle; Projectile.velocity = Vector2.Zero; Projectile.ai[0] = 0;
            followUpAge = 0;
            Projectile.netUpdate = true;
        }
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, MaterialistArsenalRules.Limit((goal - Projectile.Center) * .075f, enemy == null ? 18 : 16), .10f);
        Projectile.rotation = MathHelper.Lerp(Projectile.rotation, MathHelper.Clamp(Projectile.velocity.X * .018f, -.22f, .22f), .12f);
        int interval = mode == 1 ? 30 : mode == 2 ? 32 : 24;
        if (authority && followUpAge > 0 && age >= followUpAge)
        {
            followUpAge = 0;
            if (enemy != null && Projectile.Distance(enemy.Center) < 850 &&
                Collision.CanHitLine(Muzzle, 1, 1, enemy.position, enemy.width, enemy.height)) Fire(owner, enemy);
        }
        if (authority && enemy != null && age >= 36 && (cycle + slot * 7) % interval == 0 &&
            Projectile.Distance(enemy.Center) < 850 && Collision.CanHitLine(Muzzle, 1, 1, enemy.position, enemy.width, enemy.height))
            if (Fire(owner, enemy)) followUpAge = age + 5;
        muzzleFlash *= .73f;
        if (visualSerial != shotSerial)
        {
            visualSerial = shotSerial; muzzleFlash = 1;
            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item11 with { Volume = .25f, Pitch = .3f, MaxInstances = 4 }, Muzzle);
                MaterialistVisuals.Nano(Muzzle, shotDirection * 3, MaterialistVisuals.Cyan, .8f, 16);
                MaterialistBallistics.Muzzle(Muzzle, shotDirection * 12, -1);
            }
        }
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, .06f, .18f, .22f);
    }
    private bool Fire(Player player, NPC enemy)
    {
        ammoWeapon ??= new Item(ModContent.ItemType<FighterDispatchTerminal>());
        ammoWeapon.useAmmo = AmmoID.Bullet; ammoWeapon.shoot = ProjectileID.Bullet;
        Item selected = player.ChooseAmmo(ammoWeapon);
        if (selected == null || selected.IsAir) return false;
        int ammoDamage = selected.damage;
        if (!player.PickAmmo(ammoWeapon, out int type, out float speed, out _, out float knockback, out _)) return false;
        // PickAmmo owns ammo hooks/conservation, but its ranged ammo damage must not leak into this summon.
        StatModifier ammoModifier = player.GetTotalDamage(DamageClass.Summon);
        // The minion's current damage already includes flat/base bonuses; add only scaled ammunition damage.
        ammoModifier.Flat = ammoModifier.Base = 0;
        int damage = Projectile.damage + Math.Max(0, (int)ammoModifier.ApplyTo(ammoDamage));
        Vector2 aim = enemy.Center + Vector2.Clamp(enemy.velocity * 4, new Vector2(-28), new Vector2(28));
        shotDirection = (aim - Muzzle).SafeNormalize(Vector2.UnitX * Projectile.spriteDirection);
        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), Muzzle, shotDirection * MathHelper.Clamp(speed, 8, 24),
            type, damage, knockback, Projectile.owner);
        if (index < Main.maxProjectiles)
        {
            Projectile shot = Main.projectile[index];
            shot.GetGlobalProjectile<DispatchAmmoGlobal>().Bind(shot);
            shot.netUpdate = true;
        }
        shotSerial++; Projectile.netUpdate = true;
        return true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    { writer.Write(targetType); writer.Write(shotSerial); writer.Write(shotDirection.X); writer.Write(shotDirection.Y); }
    public override void ReceiveExtraAI(BinaryReader reader)
    { targetType = reader.ReadInt32(); shotSerial = reader.ReadInt32(); shotDirection = new Vector2(reader.ReadSingle(), reader.ReadSingle()); }
    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
        Rectangle frame = texture.Frame(1, 2, 0, age / 6 % 2);
        float appear = MathHelper.SmoothStep(0, 1, Math.Clamp(age / 36f, 0, 1));
        SpriteEffects flip = Projectile.spriteDirection < 0 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
        MaterialistTechRenderer.ChassisMaterial(texture, Projectile.Center, frame, Projectile.rotation, new Vector2(.85f),
            flip, MaterialistVisuals.Cyan * (.85f * appear), 4);
        Main.spriteBatch.Draw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, .7f) * appear,
            Projectile.rotation, frame.Size() * .5f, .85f, flip, 0);
        if (age < 36)
            MaterialistTechRenderer.ChassisMaterial(texture, Projectile.Center, frame, Projectile.rotation, new Vector2(.85f),
                flip, MaterialistVisuals.Cyan * (1 - appear), 1, appear);
        if (muzzleFlash > .05f)
        {
            MaterialistVisuals.Glow(Muzzle + shotDirection * 12, MaterialistVisuals.Cyan * muzzleFlash, new Vector2(45, 18), shotDirection.ToRotation());
            MaterialistVisuals.Glow(Muzzle, Color.White * muzzleFlash, new Vector2(14));
        }
        return false;
    }
}
