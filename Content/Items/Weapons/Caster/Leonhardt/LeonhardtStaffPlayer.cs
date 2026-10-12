using System;
using ArknightsMod.Content.Projectiles.Caster.Leonhardt;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.Leonhardt;

public sealed class LeonhardtStaffPlayer : ModPlayer
{
    private bool keyConsumed;

    internal void TryActivate()
    {
        if (Player.whoAmI != Main.myPlayer || keyConsumed || Player.HeldItem.ModItem is not LeonhardtStaff
            || Player.dead || Player.CCed || Player.noItems) return;
        keyConsumed = true;
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != nameof(LeonhardtStaff)
            || skills.StockCount <= 0 || skills.SkillActive || skills.Skill is < 0 or > 1) return;

        if (skills.Skill == 0)
        {
            skills.DelStockCount();
            skills.SkillActive = true;
            skills.SkillTimer = 0;
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = .42f, Pitch = .18f }, Player.Center);
            return;
        }

        Vector2 aim = Main.MouseWorld - Player.MountedCenter;
        if (aim.LengthSquared() > 720f * 720f)
            aim = aim.SafeNormalize(Vector2.UnitX) * 720f;
        Vector2 center = Player.MountedCenter + aim;
        if (!Collision.CanHitLine(Player.MountedCenter, 1, 1, center, 1, 1)) return;

        int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
        float multiplier = MathHelper.Lerp(1.5f, 2.3f, rank / 9f);
        int damage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * multiplier));
        skills.DelStockCount();
        skills.SkillTimer = 0;
        int index = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), center, Vector2.Zero,
            ModContent.ProjectileType<LeonhardtDetonation>(), damage, Player.HeldItem.knockBack,
            Player.whoAmI, 1f, 1f);
        if (Main.projectile.IndexInRange(index))
            Main.projectile[index].CritChance = Player.GetWeaponCrit(Player.HeldItem);
    }

    public override void PostUpdate()
    {
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        else TryActivate();
    }
}
