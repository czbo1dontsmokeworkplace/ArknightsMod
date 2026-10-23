using ArknightsMod.Players;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Sniper.Jessica;

public sealed class JessicaDodgePlayer : ModPlayer, IProbabilityDodgeSource
{
    public string DodgeName => "杰西卡·掩护烟幕";
    public double DodgeChance => Player.HeldItem.ModItem is JessicaGun
        && Player.GetModPlayer<WeaponPlayer>().SkillActive
        && Player.GetModPlayer<WeaponPlayer>().Skill == 1 ? .75 : 0;
}
