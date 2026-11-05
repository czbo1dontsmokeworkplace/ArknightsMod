using System;
using ArknightsMod.Content.Items.Weapons.Charged;
using ArknightsMod.Content.Projectiles.Charged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class DreadnoughtPlayer : ModPlayer
{
    internal int IdleTicks { get; private set; }
    internal bool InAttack { get; private set; }
    private bool empoweredAttack;
    private float attackCrit;
    private bool attackJustEnded;
    private bool ownsMomentBuff;
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
        RefreshMoment();
        return charged;
    }
    internal void ObserveAttack(bool empowered, int crit)
    {
        IdleTicks = 0;
        InAttack = true;
        empoweredAttack = empowered;
        attackCrit = crit;
        RefreshMoment();
    }
    internal void EndAttack()
    {
        InAttack = empoweredAttack = false;
        IdleTicks = 0;
        attackJustEnded = true;
        ClearMoment();
    }
    public override void PostUpdate()
    {
        if (!Player.active || Player.dead || Player.noItems || Player.CCed) { Reset(); return; }
        bool held = Player.HeldItem.ModItem is ChargedOperatorWeapon { Melee: true };
        if (!held) { Reset(); return; }
        if (InAttack) {
            RefreshMoment();
            return;
        }
        if (attackJustEnded) { attackJustEnded = false; return; }
        if (Player.itemAnimation > 0) {
            IdleTicks = 0;
            ClearMoment();
            return;
        }
        bool wasReady = Ready;
        IdleTicks = Math.Min(DreadnoughtRules.ReadyTicks, IdleTicks + 1);
        if (Ready) {
            RefreshMoment();
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
        ClearMoment();
    }

    // Use the real vanilla buff. Projectile.Damage applies its +400% bonus and
    // consumes it on contact; restore the icon for the remainder of this swing.
    internal void RefreshMoment()
    {
        if (!HasMoment) return;
        ownsMomentBuff = true;
        Player.AddBuff(BuffID.ParryDamageBuff, 2);
        Player.parryDamageBuff = true;
        ApplyRedEyes();
    }

    private void ClearMoment()
    {
        if (!ownsMomentBuff) return;
        Player.ClearBuff(BuffID.ParryDamageBuff);
        Player.parryDamageBuff = false;
        ownsMomentBuff = false;
    }

    // Calamity's Shadow Potion enables the same vanilla eye trail with level 2.
    // Tint only the effect call, preserving the character's saved eye color.
    private void ApplyRedEyes()
    {
        if (HasMoment) Player.yoraiz0rEye = Math.Max(Player.yoraiz0rEye, 2);
    }
    public override void FrameEffects() => ApplyRedEyes();
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (HasMoment) drawInfo.colorEyes = Color.Red * (1f - drawInfo.shadow);
    }
    public override void Load()
    {
        if (!Main.dedServ) On_Player.Yoraiz0rEye += DrawRedEyes;
    }
    public override void Unload()
    {
        if (!Main.dedServ) On_Player.Yoraiz0rEye -= DrawRedEyes;
    }
    private static void DrawRedEyes(On_Player.orig_Yoraiz0rEye orig, Player player)
    {
        if (!player.GetModPlayer<DreadnoughtPlayer>().HasMoment) { orig(player); return; }
        Color eyeColor = player.eyeColor;
        int eyeShader = player.cYorai;
        try { player.eyeColor = Color.Red; player.cYorai = 0; orig(player); }
        finally { player.eyeColor = eyeColor; player.cYorai = eyeShader; }
    }
    public override void UpdateDead() => Reset();
    public override void OnEnterWorld() => Reset();
}
