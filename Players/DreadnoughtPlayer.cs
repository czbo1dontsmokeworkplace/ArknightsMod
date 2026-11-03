using System;
using ArknightsMod.Content.Buffs;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class DreadnoughtPlayer : ModPlayer
{
    internal int IdleTicks { get; private set; }
    internal bool InAttack { get; private set; }
    private bool empoweredAttack;
    private float attackCrit;
    private bool attackJustEnded;
    internal bool Ready => IdleTicks >= DreadnoughtRules.ReadyTicks;
    internal bool HasMoment => Ready || empoweredAttack;
    internal float Progress => empoweredAttack ? 1 : InAttack ? 0 : IdleTicks / (float)DreadnoughtRules.ReadyTicks;
    internal float CritChance(float baseline) => InAttack ? attackCrit : Math.Clamp(baseline, 0, 100)
        + (100 - Math.Clamp(baseline, 0, 100)) * Progress;

    // Capture crit first, then mark this attack as consuming the preparation.
    internal bool BeginAttack(int crit)
    {
        bool charged = Ready;
        attackCrit = crit;
        IdleTicks = 0;
        InAttack = true;
        empoweredAttack = charged;
        attackJustEnded = false;
        if (charged) Player.AddBuff(ModContent.BuffType<AstonishingMoment>(), 2);
        return charged;
    }
    internal void ObserveAttack(bool empowered, int crit)
    {
        IdleTicks = 0;
        InAttack = true;
        empoweredAttack = empowered;
        attackCrit = crit;
        if (empowered) Player.AddBuff(ModContent.BuffType<AstonishingMoment>(), 2);
    }
    internal void EndAttack()
    {
        InAttack = empoweredAttack = false;
        IdleTicks = 0;
        attackJustEnded = true;
        Player.ClearBuff(ModContent.BuffType<AstonishingMoment>());
    }
    public override void PostUpdate()
    {
        if (!Player.active || Player.dead) { Reset(); return; }
        bool held = Player.HeldItem.ModItem is ChargedOperatorWeapon { Melee: true };
        if (!held) { Reset(); return; }
        if (InAttack) {
            if (empoweredAttack) Player.AddBuff(ModContent.BuffType<AstonishingMoment>(), 2);
            return;
        }
        if (attackJustEnded) { attackJustEnded = false; return; }
        if (Player.itemAnimation > 0 || Player.noItems || Player.CCed) {
            IdleTicks = 0;
            Player.ClearBuff(ModContent.BuffType<AstonishingMoment>());
            return;
        }
        bool wasReady = Ready;
        IdleTicks = Math.Min(DreadnoughtRules.ReadyTicks, IdleTicks + 1);
        if (Ready) {
            Player.AddBuff(ModContent.BuffType<AstonishingMoment>(), 2);
            if (!wasReady && Player.whoAmI == Main.myPlayer) {
                var weapon = (ChargedOperatorWeapon)Player.HeldItem.ModItem;
                Projectile.NewProjectile(Player.GetSource_Misc("DreadnoughtReady"), Player.Top,
                    Microsoft.Xna.Framework.Vector2.Zero, ModContent.ProjectileType<DreadnoughtMomentFlash>(),
                    0, 0, Player.whoAmI, (int)weapon.Kind);
            }
        }
    }
    private void Reset()
    {
        IdleTicks = 0;
        InAttack = empoweredAttack = attackJustEnded = false;
        Player.ClearBuff(ModContent.BuffType<AstonishingMoment>());
    }
    public override void UpdateDead() => Reset();
    public override void OnEnterWorld() => Reset();
}
