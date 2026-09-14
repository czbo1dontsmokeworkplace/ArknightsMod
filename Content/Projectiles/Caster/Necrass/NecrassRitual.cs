using System;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Necrass;

public sealed class NecrassRitual : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private int age;
    private int Mode => (int)Projectile.ai[0];
    private float DurationScale => Math.Clamp(Projectile.ai[2], .5f, 1);
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
        Projectile.netImportant = true; Projectile.timeLeft = 180;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Projectile courtProjectile = NecrassCourt.Find(Projectile.owner);
        if (courtProjectile?.ModProjectile is not NecrassCourt court)
        {
            // 等待同次使用创建的控制弹幕到达；不能跳过 age == 2 的施法执行点。
            if (Projectile.timeLeft < 150 && NecrassCourt.Authority) Projectile.Kill();
            return;
        }
        age++;
        if (age == 1)
        {
            NecrassVisuals.Embers(Projectile.Center, 30, 4);
            SoundEngine.PlaySound(SoundID.Item117 with { Volume = .55f, Pitch = -.6f }, Projectile.Center);
        }
        if (!NecrassCourt.Authority) return;
        if (Mode != 0 && court.Mode != Mode - 1) { Projectile.Kill(); return; }
        if (age == 2)
        {
            if (Mode == 0) court.Seed(Projectile.Center);
            if (Mode == 1) court.Reassemble();
            if (Mode == 2) Sleep(court);
            // 特殊仆役由召唤规则生成；不能把已有普通仆役直接改成大兵。
            if (Mode == 3 && !NecrassCourt.Servants(Projectile.owner).Exists(s => s.Special))
            {
                var servants = NecrassCourt.Servants(Projectile.owner);
                if (servants.Count >= (court.Elite == 0 ? 2 : 3)) servants[^1].Dismiss();
                court.Summon(Projectile.Center);
            }
        }
        int repeats = court.Rank >= 7 ? 3 : 2;
        int firstPulse = (int)(12 * DurationScale), interval = (int)(28 * DurationScale);
        if (Mode == 3 && age >= firstPulse && (age - firstPulse) % interval == 0 && (age - firstPulse) / interval < repeats)
        {
            NecrassImpact.Spawn(Projectile, Main.player[Projectile.owner].Center, Vector2.Zero,
                (int)(courtProjectile.damage * (4.5f + court.Rank * 3.5f / 9)), 1, 360);
            var servants = NecrassCourt.Servants(Projectile.owner);
            NecrassServant crowned = servants.Find(s => s.Special);
            NecrassServant sacrifice = servants.Find(s => !s.Special);
            if (crowned != null && sacrifice != null)
            {
                int levels = sacrifice.Level == 0 ? 1 : 2;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), sacrifice.Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<NecrassOffering>(), 0, 0, Projectile.owner, crowned.Projectile.identity);
                sacrifice.Dismiss();
                crowned.Upgrade(levels, false);
            }
        }
        if (age >= (Mode == 3 ? (int)(96 * DurationScale) : 40)) Projectile.Kill();
    }
    private void Sleep(NecrassCourt court)
    {
        Player owner = Main.player[Projectile.owner];
        int count = court.Rank >= 7 ? 3 : 2;
        for (int i = 0; i < count; i++)
        {
            NPC selected = null;
            float best = 760 * 760;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || NecrassSleep.HasBrand(npc, owner.whoAmI)) continue;
                float distance = npc.DistanceSQ(owner.Center);
                if (distance < best && Collision.CanHitLine(owner.Center, 1, 1, npc.position, npc.width, npc.height))
                { best = distance; selected = npc; }
            }
            if (selected == null) break;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), selected.Center, Vector2.Zero,
                ModContent.ProjectileType<NecrassSleep>(), (int)(Projectile.damage * (.7f + court.Rank * .9f / 9)),
                0, owner.whoAmI, selected.whoAmI, 720 * DurationScale);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        float fade = MathHelper.Clamp(1 - age / (Mode == 3 ? 96f * DurationScale : 40f), 0, 1);
        NecrassVisuals.Seal(Projectile.Center, 34 + Math.Min(age, 28) * 2, age * .035f, fade);
        if (Mode == 3) NecrassVisuals.Crown(Projectile.Center + new Vector2(0, -48), 50, age * .04f, fade, 6);
        return false;
    }
}

public sealed class NecrassOffering : ModProjectile
{
    public override string Texture => "Terraria/Images/Projectile_0";
    private Vector2 start;
    private int age;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 30;
        Projectile.tileCollide = false; Projectile.ignoreWater = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        if (age++ == 0) start = Projectile.Center;
        foreach (var s in NecrassCourt.Servants(Projectile.owner))
        {
            if (s.Projectile.identity != (int)Projectile.ai[0]) continue;
            float t = Math.Clamp(age / 28f, 0, 1);
            Projectile.Center = Vector2.Lerp(start, s.Projectile.Center, t) + new Vector2(0, -MathF.Sin(t * MathHelper.Pi) * 60);
            NecrassVisuals.Embers(Projectile.Center, 2, 1);
            return;
        }
        Projectile.Kill();
    }
    public override bool PreDraw(ref Color lightColor)
    {
        NecrassVisuals.Crown(Projectile.Center, 9, age * .1f, 1, 3);
        return false;
    }
}
