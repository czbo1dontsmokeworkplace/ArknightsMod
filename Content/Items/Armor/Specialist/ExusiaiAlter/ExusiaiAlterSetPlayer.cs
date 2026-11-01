using System;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Content.Items.Weapons.Medic;
using ArknightsMod.Content.Projectiles.Specialist.ExusiaiAlter;
using ArknightsMod.Players;
using ArknightsMod.Systems.Gameplay.OperatorTags;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.ExusiaiAlter;

internal class ExusiaiAlterSetPlayer : ArknightsArmorPlayer
{
    public bool ExusiaiAlterHelmetActive;
    public bool ExusiaiAlterSetActive;
    private int lifeDrainTimer;

    internal static bool HasHelmet(Player player) => player.active && !player.dead
        && player.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<ExusiaiAlterHead>();
    internal static bool HasSet(Player player) => HasHelmet(player)
        && player.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<ExusiaiAlterBody>()
        && player.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<ExusiaiAlterLegs>();

    public override void ResetEffects() => ExusiaiAlterHelmetActive = ExusiaiAlterSetActive = false;
    public override void PostUpdateEquips() {
        ExusiaiAlterHelmetActive = HasHelmet(Player);
        ExusiaiAlterSetActive = HasSet(Player);
    }

    public override void PostUpdateMiscEffects() {
        if (!Player.active || Player.dead || !OperatorAmmoConsumeHelper.HasAmmoSkill(Player)) return;
        foreach (Player source in Main.ActivePlayers) {
            if (!HasHelmet(source) || !MedicHealingNet.Allied(source, Player)) continue;
            bool laterano = OperatorTagHelper.PlayerHasFaction(Player, OperatorFaction.Laterano);
            Player.GetDamage(DamageClass.Generic) *= laterano ? 1.18f : 1.09f;
            break; // Multiple identical auras do not stack.
        }
    }

    public override void PostUpdate() {
        if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer) return;
        if (!HasSet(Player)) { lifeDrainTimer = 0; return; }
        if (++lifeDrainTimer < 30) return;
        lifeDrainTimer = 0;
        Player.statLife--;
        if (Player.statLife <= 0) {
            Player.KillMe(PlayerDeathReason.ByCustomReason(Language.GetTextValue(
                "Mods.ArknightsMod.ArmorSets.ExusiaiAlter.LifeDrainDeath", Player.name)), 1, 0);
        }
        SyncLife();
    }
    public override void UpdateDead() => lifeDrainTimer = 0;

    public void OnAllyAmmoConsumed(Player ally) {
        if (Main.netMode == NetmodeID.Server || Player.whoAmI != Main.myPlayer
            || !HasSet(Player) || ally.whoAmI == Player.whoAmI || !MedicHealingNet.Allied(ally, Player)) return;

        int before = Player.statLife;
        Player.GetModPlayer<IncomingHealingPlayer>().HealSilent(Math.Max(1, (int)(Player.statLifeMax2 * .06f)));
        int restored = Player.statLife - before;
        if (restored > 0) {
            Player.HealEffect(restored);
            SyncLife();
        }

        if (!Main.rand.NextBool(4)) return;
        NPC target = FindNearestEnemy(ally, 600f);
        if (target == null) return;
        // GetWeaponDamage includes the held weapon's damage class, prefixes, skills and armor bonuses once.
        int attack = Player.HeldItem.IsAir ? 0 : Player.GetWeaponDamage(Player.HeldItem);
        int damage = Math.Max(1, (int)(attack * 1.5f));
        Projectile.NewProjectile(Player.GetSource_FromThis(), target.Center, Vector2.Zero,
            ModContent.ProjectileType<ExusiaiAlterBomb>(), damage, 0, Player.whoAmI);
    }

    private void SyncLife() {
        if (Main.netMode == NetmodeID.MultiplayerClient)
            NetMessage.SendData(MessageID.PlayerLifeMana, number: Player.whoAmI);
    }

    private static NPC FindNearestEnemy(Player ally, float range) {
        NPC nearest = null;
        float distance = range * range;
        foreach (NPC npc in Main.ActiveNPCs) {
            if (npc.friendly || !npc.CanBeChasedBy()) continue;
            float next = Vector2.DistanceSquared(npc.Center, ally.Center);
            if (next >= distance) continue;
            nearest = npc;
            distance = next;
        }
        return nearest;
    }
}
