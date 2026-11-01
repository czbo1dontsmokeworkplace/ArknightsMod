using ArknightsMod.Players;
using Terraria;

namespace ArknightsMod.Content.Projectiles.Defender;

// Values are Terraria adaptations of each operator's skills, not branch-wide placeholders.
internal readonly record struct DefenderSkillStats(float Attack = 1f, float Defense = 1f,
    float Speed = 1f, float Life = 1f, float Regen = 0f, float Reduction = 0f);

internal static class DefenderSkillRules
{
    internal static DefenderSkillStats Stats(Player player)
    {
        if (!DefenderRules.SkillActive(player)) return new(1f, 1f, 1f, 1f);
        return (player.HeldItem.ModItem.Name, DefenderRules.SkillIndex(player)) switch
        {
            ("BeagleWeapon", _) => new(Defense: 1.5f),
            ("CuoraWeapon", 0) => new(Defense: 1.8f),
            ("CuoraWeapon", 1) => new(Defense: 2.3f, Regen: .03f),
            ("BisonShield", 0) => new(Defense: 2f),
            ("BisonShield", 1) => new(Defense: 2.2f),
            ("HeavyrainBlade", 0) => new(1f, 1f, 1f, 1f),
            ("HeavyrainBlade", 1) => new(Defense: 1.55f, Life: 1.55f),
            ("HoshigumaShield", 0) => new(Attack: 1.4f, Defense: 1.8f),
            ("HoshigumaShield", 1) => new(Attack: 2.4f, Defense: 1.9f, Speed: 1.3f),
            ("NianWeapon", 0) => new(Attack: 1.45f, Defense: 1.7f),
            ("NianWeapon", 1) => new(Defense: 2.3f),
            ("NianWeapon", 2) => new(Attack: 2.2f, Defense: 1.8f),
            ("LutonHammer", 0) => new(Attack: 2.3f),
            ("LutonHammer", 1) => new(Defense: 2.3f),
            ("Vulcan_Weapon", 0) => new(Defense: 2.2f, Regen: .03f),
            ("Vulcan_Weapon", 1) => new(Attack: 2.5f, Speed: .85f),
            ("MudrockHammer", 0) => new(Defense: 2f),
            ("MudrockHammer", 1) => new(Attack: 2.7f),
            ("MudrockHammer", 2) => new(Attack: 2.4f, Defense: 1.8f, Speed: 1.3f),
            ("LiskarmPistol", 0) => new(Defense: 2f),
            ("LiskarmPistol", 1) => new(Attack: 2f, Speed: .8f),
            ("BlitzPistol", 1) => new(Speed: 2.5f),
            ("JessicaLiberatedGun", 0) => new(Attack: 1.7f, Defense: 1.7f),
            ("JessicaLiberatedGun", 1) => new(Attack: 1.75f, Speed: 1.8f),
            ("MechanistGun", 0) => new(Speed: .6f),
            ("MechanistGun", 1) => new(Attack: 2.5f),
            ("FaithMixer", 0) => new(Attack: 1.9f),
            ("FaithMixer", 1) => new(Attack: 2.5f, Defense: 2.3f),
            ("DN_Weapon", _) => new(Attack: 1.8f),
            ("AsbestosBlade", 0) => new(Reduction: .3f),
            ("AsbestosBlade", 1) => new(Attack: 1.9f, Defense: 1.6f, Speed: .8f),
            ("ShalemBlade", 0) => new(Speed: 1.65f, Life: 1.5f),
            ("ShalemBlade", 1) => new(Attack: 1.8f, Speed: 1.3f),
            ("CzernyBlade", 0) => new(Attack: 1.8f, Reduction: .4f),
            ("CzernyBlade", 1) => new(Attack: 1f + player.GetModPlayer<DefenderCombatPlayer>().ResonanceStacks * .15f, Life: 2f),
            ("HoshigumaBlazingBlade", 0) => new(Attack: 1.7f, Defense: 1.7f),
            ("HoshigumaBlazingBlade", 1) => new(Attack: 1.9f),
            ("FirewhistleLauncher", 0) => new(Attack: 1.85f),
            ("AshlockLauncher", 0) => new(Attack: 2f),
            ("AshlockLauncher", 1) => new(Attack: 1.55f, Speed: 2.5f),
            ("BubbleMaul", 0) => new(Defense: 1.8f),
            ("BubbleMaul", 1) => new(Defense: 2.2f),
            ("AuroraAxe", 0) => new(Defense: 3.1f),
            ("AuroraAxe", 1) => new(Attack: 1.75f, Speed: .85f),
            ("CementMaul", 0) => new(1f, 1f, 1f, 1f),
            ("CementMaul", 1) => new(Defense: 1f + player.GetModPlayer<DefenderCombatPlayer>().DefenseStacks * .08f),
            ("EunectesAxe", 0) => new(Attack: 1.3f, Defense: 1.25f),
            ("EunectesAxe", 1) => new(Attack: 2f, Speed: .85f),
            ("EunectesAxe", 2) => new(Attack: 2.8f, Defense: 2f, Regen: .03f),
            ("MatterhornBlade", 0) => new(Life: 1.7f, Regen: .015f),
            ("MatterhornBlade", 1) => new(Life: 1.5f, Defense: 1.3f, Reduction: .3f),
            ("PhilaeBlade", 0) => new(Life: 1.9f),
            ("PhilaeBlade", 1) => new(Life: 1.5f),
            ("BasslineBlade", 0) => new(Life: 2.3f),
            ("UnderflowBlade", 0) => new(Life: 2f, Regen: .02f),
            ("UnderflowBlade", 1) => new(Defense: 1f + player.GetModPlayer<DefenderCombatPlayer>().DefenseStacks * .2f),
            ("YuBlade", 0) => new(Life: 1.7f, Defense: 1.7f),
            ("YuBlade", 1) => new(Attack: 2.9f, Life: 1.6f),
            ("SanbyBlade", 0) => new(Attack: 1.5f, Defense: 1.5f),
            ("SanbyBlade", 1) => new(Attack: 1.7f, Defense: 1.7f),
            ("CroissantHammer", 0) => new(Defense: 1.8f),
            ("CroissantHammer", 1) => new(Attack: 2.2f),
            _ => new(1f, 1f, 1f, 1f)
        };
    }

    internal static bool Meditating(Player player) => DefenderRules.Skill(player, 2)
        && player.HeldItem.ModItem?.Name == "MudrockHammer"
        && DefenderRules.SkillTime(player) < 180 * WeaponPlayer.ActiveDurationMultiplier;
    internal static bool StopsAttacking(Player player) => Meditating(player) || (DefenderRules.SkillActive(player)
        && (player.HeldItem.ModItem.Name, DefenderRules.SkillIndex(player)) is
            ("NianWeapon", 1) or ("CuoraWeapon", 1) or ("BisonShield", 1)
            or ("HeavyrainBlade", 1) or ("LutonHammer", 1) or ("BubbleMaul", 1) or ("PhilaeBlade", 1));

    internal static bool UsesOneAttack(string name, int skill) => (name, skill) is
        ("LutonHammer", 0) or ("FaithMixer", 0) or ("FirewhistleLauncher", 0);
}
