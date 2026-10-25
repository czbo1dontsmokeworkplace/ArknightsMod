using System;
using ArknightsMod.Players;
using ArknightsMod.Content.Items.Weapons.Defender;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

public sealed class DefenderCombatPlayer : ModPlayer
{
    private int healingCooldown, retaliationCooldown;
    private bool consumed;
    internal bool StopsAttacking => Player.HeldItem.ModItem?.Name == "NianWeapon" && DefenderRules.Skill(Player, 1);
    internal void TryActivate()
    {
        WeaponPlayer skills = Player.GetModPlayer<WeaponPlayer>();
        if (Player.whoAmI != Main.myPlayer || consumed || !DefenderRules.IsWeapon(Player.HeldItem)
            || skills.CurrentSkill?.Key.Item != Player.HeldItem.ModItem.Name || skills.StockCount <= 0 || skills.SkillActive) return;
        consumed = true;
        skills.DelStockCount();
        skills.SkillTimer = 0;
        skills.SkillActive = true;
        if (Player.HeldItem.ModItem?.Name == "CardiWeapon") { healingCooldown = 0; HealSelf(.4f, 0); }
        SoundEngine.PlaySound(SoundID.Item29 with { Volume = .5f }, Player.Center);
    }
    internal void HealSelf(float fraction, int cooldown)
    {
        if (Player.whoAmI != Main.myPlayer || healingCooldown > 0) return;
        int amount = Math.Min(Player.statLifeMax2 - Player.statLife, (int)(Player.statLifeMax2 * fraction));
        if (amount <= 0) return;
        Player.Heal(amount);
        healingCooldown = cooldown;
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerLifeMana, -1, -1, null, Player.whoAmI);
    }
    public override void PostUpdate()
    {
        if (healingCooldown > 0) healingCooldown--;
        if (retaliationCooldown > 0) retaliationCooldown--;
        if (!ArknightsKeybinds.SkillActivatePressed(Player)) consumed = false;
        if (!DefenderRules.IsWeapon(Player.HeldItem) || Player.dead || Player.CCed || Player.noItems) return;
        if (Player.whoAmI == Main.myPlayer)
        {
            DefenderHoldout.Ensure(Player);
            if (ArknightsKeybinds.SkillActivatePressed(Player) && Player.HeldItem.ModItem is not Items.Weapons.Defender.Horn.HornGrenadeLauncher) TryActivate();
        }
    }
    public override void UpdateEquips()
    {
        if (!DefenderRules.SkillActive(Player)) return;
        int skill = Player.GetModPlayer<WeaponPlayer>().Skill;
        string name = Player.HeldItem.ModItem.Name;
        float defense = name switch
        {
            "BeagleWeapon" => 1.25f,
            "CuoraWeapon" => skill == 0 ? 1.8f : 2.3f,
            "Vulcan_Weapon" => skill == 0 ? 1.5f : 1f,
            "NianWeapon" => skill == 0 ? 1.7f : skill == 1 ? 2.3f : 1.8f,
            "LiskarmPistol" => skill == 0 ? 1.8f : 1f,
            "CroissantHammer" => skill == 0 ? 1.8f : 1f,
            "MudrockHammer" => skill == 0 ? 1.8f : skill == 2 ? 1.6f : 1f,
            "EunectesAxe" => skill == 2 ? 2f : 1f,
            _ => 1f
        };
        if (Player.HeldItem.ModItem is RoutedDefenderWeapon) {
            defense = skill == 0 ? 1.8f : 1.3f;
            if (skill == 0) Player.lifeRegen += 4;
        }
        Player.statDefense *= defense;
        if ((name == "Vulcan_Weapon" && skill == 0) || (name == "CuoraWeapon" && skill == 1)
            || (name == "EunectesAxe" && skill == 2))
            Player.lifeRegen += Math.Max(2, Player.statLifeMax2 / 50);
    }
    public override void PostUpdateEquips()
    {
        // Iron Defense protects allies, but shields never leave the owner's hand.
        foreach (Player ally in Main.ActivePlayers)
            if (ally.whoAmI != Player.whoAmI && !ally.dead && ally.HeldItem.ModItem?.Name == "NianWeapon"
                && DefenderRules.Skill(ally, 2) && ally.DistanceSQ(Player.Center) <= 480f * 480f
                && ((!ally.hostile && !Player.hostile) || (ally.team != 0 && ally.team == Player.team)))
            { Player.statDefense *= 1.8f; break; }
    }
    private void Retaliate(Vector2 source)
    {
        if (Player.whoAmI != Main.myPlayer || retaliationCooldown > 0 || !StopsAttacking) return;
        retaliationCooldown = 30;
        DefenderPulse.Spawn(Player, Player.Center, (int)(Player.GetWeaponDamage(Player.HeldItem) * .9f), 160, true);
    }
    public override void OnHitByNPC(NPC npc, Player.HurtInfo hurtInfo) => Retaliate(npc.Center);
    public override void OnHitByProjectile(Projectile proj, Player.HurtInfo hurtInfo) => Retaliate(proj.Center);
}
