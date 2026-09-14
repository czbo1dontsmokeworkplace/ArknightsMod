using System;
using ArknightsMod.Content.Projectiles.Caster.Necrass;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Necrass;

public sealed class NecrassPlayer : ModPlayer
{
    private bool keyConsumed;
    internal int SeedCooldown;
    internal bool Holding => Player.active && !Player.dead && Player.HeldItem.ModItem is NecrassScepter;
    internal void EnsureCourt()
    {
        if (!Holding || Player.whoAmI != Main.myPlayer) return;
        var skills = Player.GetModPlayer<WeaponPlayer>();
        int mode = skills.CurrentSkill?.Key.Item == nameof(NecrassScepter) ? Math.Clamp(skills.Skill, 0, 2) : 0;
        int rank = skills.CurrentSkill?.Key.Item == nameof(NecrassScepter)
            ? Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9) : 0;
        int stage = ((NecrassScepter)Player.HeldItem.ModItem).EliteStage;
        Projectile court = NecrassCourt.Find(Player.whoAmI);
        if (court == null)
        {
            int index = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, Vector2.Zero,
                ModContent.ProjectileType<NecrassCourt>(), Player.GetWeaponDamage(Player.HeldItem), 0, Player.whoAmI, mode, rank, stage);
            if (!Main.projectile.IndexInRange(index)) return;
            court = Main.projectile[index];
        }
        int damage = Player.GetWeaponDamage(Player.HeldItem);
        if ((int)court.ai[0] != mode || (int)court.ai[1] != rank || (int)court.ai[2] != stage || court.damage != damage)
        {
            court.ai[0] = mode; court.ai[1] = rank; court.ai[2] = stage; court.damage = damage;
            court.netUpdate = true;
        }
    }
    internal void Command(int mode, Vector2 center)
    {
        Projectile court = NecrassCourt.Find(Player.whoAmI);
        if (court == null) return;
        Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), center, Vector2.Zero,
            ModContent.ProjectileType<NecrassRitual>(), court.damage, 0, Player.whoAmI, mode, court.ai[1], WeaponPlayer.ActiveDurationMultiplier);
    }
    internal void TryActivate()
    {
        if (!Holding || Player.whoAmI != Main.myPlayer || keyConsumed || Player.CCed || Player.noItems) return;
        keyConsumed = true;
        var skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != nameof(NecrassScepter) || skills.StockCount <= 0 || skills.SkillActive) return;
        if (Player.ownedProjectileCounts[ModContent.ProjectileType<NecrassRitual>()] > 0) return;
        if (skills.Skill == 1 && NecrassCourt.Target(Player.Center, 760, Player) == null) return;
        EnsureCourt();
        Command(skills.Skill + 1, Player.Center);
        skills.DelStockCount();
        skills.SkillTimer = 0;
        skills.SkillActive = true;
    }
    public override void PostUpdate()
    {
        if (SeedCooldown > 0) SeedCooldown--;
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        if (!Holding || Player.whoAmI != Main.myPlayer) return;
        EnsureCourt();
        if (ArknightsKeybinds.SkillActivatePressed(Player)) TryActivate();
    }
    public override void UpdateDead() { SeedCooldown = 0; keyConsumed = false; }
}
