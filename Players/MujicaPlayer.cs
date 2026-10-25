using ArknightsMod.Systems.Gameplay;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class MujicaPlayer : ModPlayer, IGuaranteedAvoidanceSource
{
    public float ArtsPenetration { get; private set; }
    private int activationWait;
    internal bool WaitingForActivation => activationWait > 0;
    internal void AwaitActivation() => activationWait = 180;
    internal void ConfirmActivation() => activationWait = 0;
    public GuaranteedAvoidanceState? GuaranteedAvoidance => !MujicaFeverSystem.Protected(Player) ? null
        : new("Ave Mujica · Fever", true, $"不死保护剩余 {MujicaFeverSystem.Meter.Remaining / 60f:0.0} 秒");
    public override void ResetEffects() => ArtsPenetration = 0;
    public override void PostUpdateMiscEffects() {
        if (!MujicaFeverSystem.IsMember(Player)) return;
        Player.GetArmorPenetration(DamageClass.Generic) += 3 * MujicaFeverSystem.NoteLayers;
        ArtsPenetration = .02f * MujicaFeverSystem.NoteLayers;
    }
    public override void PostUpdate() { if (activationWait > 0) activationWait--; }
    public override void UpdateDead() => activationWait = 0;
    public override void SyncPlayer(int toWho, int fromWho, bool newPlayer) => MujicaFeverSystem.Sync(toWho);
    public override void OnHitNPCWithItem(Item item, NPC target, NPC.HitInfo hit, int damageDone) => MujicaFeverSystem.ReportHit(Player, target, damageDone);
    public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone) => MujicaFeverSystem.ReportHit(Player, target, damageDone);
    public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genDust, ref PlayerDeathReason damageSource) {
        if (!MujicaFeverSystem.Protected(Player)) return true;
        Player.statLife = 1;
        playSound = genDust = false;
        return false;
    }
}
