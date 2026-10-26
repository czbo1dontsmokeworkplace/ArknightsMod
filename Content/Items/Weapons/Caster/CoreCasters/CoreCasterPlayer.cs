using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Caster.CoreCasters;

public sealed class CoreCasterPlayer : ModPlayer
{
    private int heldType, activeSkill = -1, drainCooldown;
    private bool keyConsumed;
    internal int DuettoUses { get; private set; }
    private void Attach(CoreCasterStaff staff)
    {
        if (heldType == staff.Type) return;
        heldType = staff.Type; DuettoUses = 0; activeSkill = -1; keyConsumed = false;
    }
    internal bool IsActive(CoreCasterStaff staff)
    {
        var skills = Player.GetModPlayer<WeaponPlayer>();
        return Player.HeldItem.ModItem == staff && skills.CurrentSkill?.Key.Item == staff.Name
            && skills.SkillActive && activeSkill == skills.Skill;
    }
    internal void Handle(CoreCasterStaff staff)
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead || Player.noItems || Player.CCed) return;
        Attach(staff);
        var skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != staff.Name) return;
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) keyConsumed = false;
        if (!skills.SkillActive && skills.StockCount > 0 &&
            (staff.Kind == CoreCasterKind.Absinthe && skills.Skill == 0 || staff.Kind == CoreCasterKind.Nightmare && skills.Skill == 1))
            Activate(staff);
        if (ArknightsKeybinds.SkillActivatePressed(Player)) TryActivate(staff);
    }
    internal void TryActivate(CoreCasterStaff staff)
    {
        if (Player.whoAmI != Main.myPlayer || Player.dead || Player.noItems || Player.CCed || keyConsumed) return;
        Attach(staff);
        keyConsumed = true;
        var skills = Player.GetModPlayer<WeaponPlayer>();
        if (staff.Kind == CoreCasterKind.Eyjafjalla && skills.Skill == 1) return; // Ignition is stored on-hit casting.
        Activate(staff);
    }
    private void Activate(CoreCasterStaff staff)
    {
        var skills = Player.GetModPlayer<WeaponPlayer>();
        if (skills.CurrentSkill?.Key.Item != staff.Name || skills.StockCount <= 0 || skills.SkillActive) return;
        bool nightmare = staff.Kind == CoreCasterKind.Nightmare && skills.Skill == 1;
        var targets = nightmare || staff.Kind == CoreCasterKind.Absinthe && skills.Skill == 1
            ? CoreCasterRules.Targets(Player.Center, 1000, Player.Center, staff.Kind == CoreCasterKind.Absinthe,
                staff.Kind == CoreCasterKind.Absinthe ? .5f : 1) : null;
        if (targets != null && targets.Count == 0) return;
        activeSkill = skills.Skill;
        if (nightmare)
        {
            int count = System.Math.Min(3, targets.Count), duration = (int)(420 * WeaponPlayer.ActiveDurationMultiplier);
            for (int i = 0; i < count; i++)
            {
                NPC target = targets[i];
                NightmareBrand.Apply(Player, target, Player.GetWeaponDamage(staff.Item), duration);
            }
        }
        if (staff.Kind == CoreCasterKind.Eyjafjalla && skills.Skill == 0) DuettoUses++;
        skills.DelStockCount(); skills.SkillTimer = 0; skills.SkillActive = true;
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .4f, Pitch = staff.Kind == CoreCasterKind.Nightmare ? -.45f : .15f }, Player.Center);
        CoreCasterVisuals.Activate(Player.Center, staff.Kind);
    }
    internal bool ReceiveDrain()
    {
        if (Player.dead || drainCooldown > 0 || Player.statLife >= Player.statLifeMax2) return false;
        Player.Heal(3); drainCooldown = 24;
        return true;
    }
    public override void PostUpdate()
    {
        if (drainCooldown > 0) drainCooldown--;
        if (Player.HeldItem.ModItem is CoreCasterStaff staff) Handle(staff);
        else { heldType = 0; activeSkill = -1; DuettoUses = 0; keyConsumed = false; }
    }
    public override void UpdateDead() { heldType = drainCooldown = DuettoUses = 0; activeSkill = -1; keyConsumed = false; }
}
