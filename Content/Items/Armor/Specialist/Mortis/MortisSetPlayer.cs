using System;
using ArknightsMod.Content.Items.Armor.NeoArmorReforge;
using ArknightsMod.Systems.Gameplay;
using ArknightsMod.Systems.Gameplay.Elemental;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Armor.Specialist.Mortis;

public sealed class MortisSetPlayer : ModPlayer
{
    public const int SubstituteDuration = 20 * 60;
    public int SubstituteTime { get; internal set; }
    public bool IsSubstitute => SubstituteTime > 0;
    internal bool IsAuthority => Main.netMode == NetmodeID.SinglePlayer || Player.whoAmI == Main.myPlayer;
    private int pendingNecrosis;
    private double conversionRemainder;
    private double pendingRemainder;
    private ulong enteredTick;
    public static bool HasHelmet(Player p) => p.active && !p.dead
        && p.armor[0].type == NeoArmorReforgeSetLoader.GetSetType<MortisHead>();
    public static bool HasSet(Player p) => HasHelmet(p)
        && p.armor[1].type == NeoArmorReforgeSetLoader.GetSetType<MortisBody>()
        && p.armor[2].type == NeoArmorReforgeSetLoader.GetSetType<MortisLegs>();
    public static bool BlocksActions(Player p) => p.GetModPlayer<MortisSetPlayer>().IsSubstitute;

    public override void PostUpdateEquips() {
        if (MujicaFeverSystem.IsMember(Player)) {
            foreach (Player source in Main.ActivePlayers) {
                if (!HasSet(source)) continue;
                Player.statLifeMax2 = (int)(Player.statLifeMax2 * 1.1f);
                break;
            }
        }
        if (IsSubstitute) Player.statDefense *= 1.2f;
    }

    public override void ModifyHurt(ref Player.HurtModifiers modifiers) {
        if (!HasHelmet(Player) || Player.GetModPlayer<NecrosisPlayer>().ApplyingBurstDamage) return;
        modifiers.ModifyHurtInfo += (ref Player.HurtInfo info) => {
            if (info.Damage <= 0) return;
            // Defer accumulation until OnHurt: a dodged hit must not inflict elemental damage.
            double converted = info.Damage * .3d + conversionRemainder;
            pendingNecrosis = (int)converted;
            pendingRemainder = converted - pendingNecrosis;
            info.Damage = Math.Max(1, info.Damage - pendingNecrosis);
        };
    }

    public override void OnHurt(Player.HurtInfo info) {
        if (!IsAuthority || info.Damage <= 0) return;
        if (HasSet(Player)) MujicaFeverSystem.ReportMortisDamage(Player);
        int converted = pendingNecrosis;
        if (HasHelmet(Player) && !Player.GetModPlayer<NecrosisPlayer>().ApplyingBurstDamage)
            conversionRemainder = pendingRemainder;
        pendingNecrosis = 0;
        if (converted > 0) ElementalSystem.ApplyDamage(ElementalType.Necrosis, converted, Player.whoAmI);
    }

    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound,
        ref bool genDust, ref PlayerDeathReason damageSource) {
        if (!HasHelmet(Player) || IsSubstitute || MujicaFeverSystem.Protected(Player)) return true;
        SubstituteTime = SubstituteDuration;
        enteredTick = Main.GameUpdateCount;
        RestoreFullLife();
        playSound = genDust = false;
        SyncState();
        return false;
    }

    private void RestoreFullLife() {
        int restored = Player.statLifeMax2 - Math.Max(0, Player.statLife);
        Player.statLife = Player.statLifeMax2;
        if (restored > 0) Player.HealEffect(restored);
    }

    public override void PreUpdate() { pendingNecrosis = 0; }
    public override void SetControls() {
        if (!IsSubstitute) return;
        Player.controlUseItem = Player.controlUseTile = false;
    }
    public override bool CanUseItem(Item item) => !IsSubstitute || item.damage <= 0;
    public override bool? CanHitNPCWithItem(Item item, NPC target) => IsSubstitute ? false : null;
    public override bool? CanHitNPCWithProj(Projectile proj, NPC target) => IsSubstitute ? false : null;
    public override bool CanHitPvp(Item item, Player target) => !IsSubstitute;
    public override bool CanHitPvpWithProj(Projectile proj, Player target) => !IsSubstitute;

    public override void PostUpdate() {
        if (!IsSubstitute || Player.dead) return;
        Player.AddBuff(ModContent.BuffType<Content.Buffs.ArmorSets.MortisSubstituteBuff>(), SubstituteTime);
        if (Main.GameUpdateCount != enteredTick && (IsAuthority || SubstituteTime > 1)) SubstituteTime--;
        if (!IsAuthority) return;
        if (SubstituteTime <= 0) FinishSubstitute();
        else if (SubstituteTime % 60 == 0) SyncState();
    }
    internal void ShortenSubstitute() {
        if (!IsSubstitute || !IsAuthority) return;
        SubstituteTime = Math.Max(0, SubstituteTime - 60);
        if (SubstituteTime == 0) FinishSubstitute();
        else SyncState();
    }
    private void FinishSubstitute() {
        if (Player.dead || Player.statLife <= 0) return;
        SubstituteTime = 0;
        conversionRemainder = 0;
        ElementalSystem.ClearPlayerDamage(Player.whoAmI);
        RestoreFullLife();
        SyncState();
    }
    public override void UpdateDead() {
        if (SubstituteTime > 0) { SubstituteTime = 0; SyncState(); }
        conversionRemainder = 0;
        pendingNecrosis = 0;
    }
    public override void DrawEffects(PlayerDrawSet drawInfo, ref float r, ref float g, ref float b,
        ref float a, ref bool fullBright) {
        if (!IsSubstitute) return;
        r *= .6f; g *= .7f; b *= .85f;
    }
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) =>
        MujicaFeverSystem.SyncMortis(Player, toWho);
    private void SyncState() => MujicaFeverSystem.SyncMortis(Player);
}
