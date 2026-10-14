using ArknightsMod.Content.Items.Weapons.Guard.Mousse;
using ArknightsMod.Content.Projectiles.Guard.ArtsFighter;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.Skill;
using Microsoft.Xna.Framework;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.Mousse;

public class MousseGlovePunch : ArtsFighterHoldout
{
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Guard/Mousse/MousseGlove";
    protected override int WeaponType => ModContent.ItemType<MousseGlove>();
    protected override int SlashType => ModContent.ProjectileType<MousseLightSlash>();
    protected override Color BladeColor => MousseVisuals.Pale;
    protected override float PrepareStrike()
    {
        WeaponPlayer skills = Owner.GetModPlayer<WeaponPlayer>();
        return skills.Skill == 0 && skills.SkillActive ? 1f : 0f;
    }
    protected override void AfterStrike()
    {
        WeaponPlayer skills = Owner.GetModPlayer<WeaponPlayer>();
        if (skills.Skill == 0 && skills.SkillActive) skills.SkillActive = false;
        else if (skills.Skill == 0 && skills.CurrentSkill?.ChargeType == SkillChargeType.Attack)
            skills.OffensiveRecovery();
    }
}
