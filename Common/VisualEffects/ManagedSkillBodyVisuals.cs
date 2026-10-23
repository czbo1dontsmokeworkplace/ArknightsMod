using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Vanguard.StandardBearers;
using ArknightsMod.Players;
using ArknightsMod.Content.Items.Weapons.Sniper.Typhon;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Common.VisualEffects;

// Owns only cosmetic lifetime. No gameplay fields, skill data, damage or SP are changed.
public sealed class ManagedSkillBodyPlayer : ModPlayer
{
    private int previousItem, previousSkill = -1;
    private Projectile controller;
    public override void PostUpdate()
    {
        if (Player.whoAmI != Main.myPlayer) return;
        WeaponPlayer state = Player.GetModPlayer<WeaponPlayer>();
        string name = Player.HeldItem.ModItem?.Name;
        bool eligible = !Player.dead && !Player.CCed && !Player.noItems && name != null
            && state.SkillActive && state.CurrentSkill?.Key.Item == name
            && ManagedSkillVisualCatalog.Eligible(name, state.CurrentSkill)
            && !(name == "TyphonBow" && state.Skill == 1 && Player.GetModPlayer<TyphonBow.TyphonState>().S2ActivationCount >= 2)
            && Player.HeldItem.ModItem is not StandardBearerWeapon;
        if (!eligible)
        { Cancel(); previousItem = 0; previousSkill = -1; return; }
        bool restart = previousItem != Player.HeldItem.type || previousSkill != state.Skill;
        if (restart || controller?.active != true)
        {
            Cancel();
            int index = Projectile.NewProjectile(Player.GetSource_Misc("ManagedSkillBody"), Player.Center, Vector2.Zero,
                ModContent.ProjectileType<ManagedSkillBodyController>(), 0, 0, Player.whoAmI,
                Player.HeldItem.type, state.Skill, Math.Max(1, ManagedSkillVisualCatalog.Duration(name, state.CurrentSkill) * 60 * WeaponPlayer.ActiveDurationMultiplier));
            if (index < Main.maxProjectiles) controller = Main.projectile[index];
        }
        previousItem = Player.HeldItem.type; previousSkill = state.Skill;
    }
    private void Cancel() { if (controller?.active == true && controller.owner == Player.whoAmI) controller.Kill(); controller = null; }
    public override void UpdateDead() { Cancel(); previousItem = 0; previousSkill = -1; }
}

// ai[] is sufficient for late joiners: item, skill and authored duration. Age is cosmetic;
// owner cancellation/heartbeat is authoritative, because some skills have custom timers.
public sealed class ManagedSkillBodyController : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    private int age;
    public override void SendExtraAI(BinaryWriter writer) => writer.Write(age);
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        age = Math.Max(0, reader.ReadInt32());
        Projectile.timeLeft = 90;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 90;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override void AI()
    {
        Player owner = Main.player[Projectile.owner];
        string name = owner.HeldItem.ModItem?.Name;
        if (!owner.active || owner.dead || owner.CCed || owner.noItems || owner.HeldItem.type != (int)Projectile.ai[0]
            || name == null || !ManagedSkillVisualCatalog.Profiles.ContainsKey(name)) { Projectile.Kill(); return; }
        if (Projectile.owner == Main.myPlayer)
        {
            WeaponPlayer state = owner.GetModPlayer<WeaponPlayer>();
            if (!state.SkillActive || state.Skill != (int)Projectile.ai[1]
                || state.CurrentSkill?.Key.Item != name || !ManagedSkillVisualCatalog.Eligible(name, state.CurrentSkill))
            { Projectile.Kill(); return; }
            Projectile.timeLeft = 90;
            if (age % 30 == 0) Projectile.netUpdate = true;
        }
        Projectile.Center = owner.MountedCenter;
        float seconds = Math.Max(1, Projectile.ai[2]) / 60;
        int tick = age++;
        if (Main.dedServ || Main.gamePaused || owner.Center.DistanceSQ(Main.screenPosition
            + new Vector2(Main.screenWidth, Main.screenHeight) * .5f) > 1400 * 1400) return;
        var profile = ManagedSkillVisualCatalog.Profile(name, (int)Projectile.ai[1]);
        // Remote late joiners get the quiet sustained indicator, not another activation burst.
        if (tick == 0 && Projectile.owner == Main.myPlayer)
            for (int i = 0; i < ManagedSkillVisualCatalog.Burst(seconds); i++) Emit(owner, profile, i, true);
        if (tick % ManagedSkillVisualCatalog.Interval(seconds) == 0) Emit(owner, profile, tick, false);
    }
    private static void Emit(Player owner, SkillBodyProfile profile, int phase, bool burst)
    {
        if (Particle.ParticleManager.activeParticles.Count >= 1200) return;
        int local = 0;
        foreach (var particle in Particle.ParticleManager.activeParticles)
            if (particle is T_SkillBodyMote mote && mote.Owner == owner.whoAmI && ++local >= 24) return;
        Vector2 offset = new(Main.rand.NextFloat(-owner.width * .65f, owner.width * .65f),
            Main.rand.NextFloat(-owner.height * .45f, owner.height * .4f));
        if (profile.Motif == SkillBodyMotif.Orbit) offset = new Vector2(MathF.Cos(phase * .7f) * 22, MathF.Sin(phase * .7f) * 9);
        Vector2 velocity = new Vector2(Main.rand.NextFloat(-.6f, .6f), -Main.rand.NextFloat(.5f, 1.2f)) * owner.gravDir;
        if (burst) velocity += offset.SafeNormalize(Vector2.UnitX) * 1.3f;
        new T_SkillBodyMote(owner.whoAmI, owner.Center + offset, velocity, profile,
            burst ? 20 : 30, burst ? 7 : 5).Spawn();
        if (!burst && phase % 2 == 0)
        {
            int dustType = profile.Motif == SkillBodyMotif.Ember ? DustID.Torch : DustID.TintableDustLighted;
            Dust dust = Dust.NewDustPerfect(owner.Center + offset, dustType, velocity * .6f, 150, profile.Color, .55f);
            dust.noGravity = true;
        }
    }
}
