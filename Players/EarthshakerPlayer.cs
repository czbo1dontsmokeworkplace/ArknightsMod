using System;
using ArknightsMod.Content;
using ArknightsMod.Content.Items.Weapons.Guard.Earthshaker;
using ArknightsMod.Content.Projectiles.Guard.Earthshaker;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Players;

public sealed class EarthshakerPlayer : ModPlayer
{
    public bool RightHeld { get; private set; }
    internal bool LeftHeld { get; private set; }
    private Item toolItem,preparedItem;
    // A native right-click swing keeps its tool configuration through release.
    internal bool ToolUse => toolItem==Player.HeldItem&&toolItem!=null;
    public bool Busy => EarthshakerHeldHammer.Find(Player)!=null;
    public bool Charging => EarthshakerHeldHammer.Find(Player)?.IsCharging==true;
    public float ChargeProgress => EarthshakerHeldHammer.Find(Player)?.ChargeProgress??0;
    public EarthshakerKind Kind => Player.HeldItem.ModItem is EarthshakerWeapon w?w.Kind:EarthshakerKind.Odda;
    private Item skillItem;
    private int mode,skillTime,duration,level=1,chargeCooldown,skillHits,pepeUses;
    private bool previousRight,keyWasDown;
    private float shake;private int shakeTime;
    public int ActiveMode => skillTime>0&&skillItem==Player.HeldItem&&Holding&&Player.GetModPlayer<WeaponPlayer>().Skill==mode-1?mode:0;
    private bool Holding => !Player.dead&&Player.HeldItem.ModItem is EarthshakerWeapon;
    private float L(float a,float b)=>MathHelper.Lerp(a,b,(level-1)/9f);
    public int ChargeTicks => ActiveMode==2&&Kind==EarthshakerKind.Pepe?Math.Max(12,30-pepeUses*6):ActiveMode==1&&Kind==EarthshakerKind.Zima?20:30;
    public override void SetControls()
    {
        bool allowed=Holding&&!Player.noItems&&!Player.CCed&&!Player.mouseInterface&&!Main.blockMouse
            &&Content.Items.Weapons.Charged.ChargedOperatorWeapon.WorldInputAllowed(Player);
        RightHeld=allowed&&Player.controlUseTile;
        LeftHeld=allowed&&Player.controlUseItem&&!RightHeld;
        if(!Holding){toolItem=null;previousRight=false;return;}
        if(toolItem!=Player.HeldItem)toolItem=null;
        // Right click is translated once, before vanilla right-click features run.
        // Do not also enter Terraria's alternate-shooting state machine.
        Player.altFunctionUse=0;
        if(RightHeld){
            toolItem=Player.HeldItem;
            if(EarthshakerHeldHammer.Find(Player) is { } held){
                held.Projectile.Kill();Player.heldProj=-1;Player.channel=false;
                Player.itemTime=Player.itemAnimation=0;Player.releaseUseItem=true;
            }
            Player.controlUseTile=false;Player.controlUseItem=true;Player.channel=false;
            if(!previousRight){
                Player.poundRelease=true;
                if(Player.itemAnimation==0)Player.releaseUseItem=true;
            }
        }
        else if(ToolUse){
            Player.channel=false;
            if(Player.itemAnimation==0)toolItem=null;
            else Player.controlUseItem=false;
        }
        previousRight=RightHeld;
    }
    public override bool PreItemCheck()
    {
        if(Player.HeldItem.ModItem is EarthshakerWeapon weapon){
            preparedItem=Player.HeldItem;
            bool tool=ToolUse||!LeftHeld&&!Busy;
            weapon.Configure(tool);
            // Suppress mining only inside a left-attack ItemCheck. Restore the
            // actual hammer stat afterwards for inventory/UI and idle release.
            if(!tool)preparedItem.hammer=0;
        }
        return true;
    }
    public override void PostItemCheck()
    {
        if(preparedItem?.ModItem is EarthshakerWeapon weapon)preparedItem.hammer=EarthshakerRules.HammerPower(weapon.Kind);
        preparedItem=null;
    }
    internal void BeginCharge(int damage,float kb)
    {
        if(Busy||!Holding||!LeftHeld||ToolUse||Player.whoAmI!=Main.myPlayer)return;
        float attackPower=1,attackRange=1;int impactStatus=0,baseDamage;float knockback;
        level = Math.Clamp(Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.ForceReplaceLevel ?? Player.GetModPlayer<WeaponPlayer>().CurrentSkill?.Level ?? 1, 1, 10);
        attackPower = attackRange = 1; impactStatus = 0;
        var shared = Player.GetModPlayer<WeaponPlayer>();
        if (ActiveMode == 0 && shared.Skill == 0 && Kind != EarthshakerKind.Zima && shared.StockCount > 0) {
            shared.DelStockCount(); attackPower = Kind == EarthshakerKind.Odda ? L(1.4f, 2.4f) : L(1.6f, 2.9f); attackRange = 1.35f;
        }
        if (ActiveMode > 0) {
            if (Kind == EarthshakerKind.Odda) { attackPower *= L(1.1f, 2); impactStatus = 1; }
            if (Kind == EarthshakerKind.Zima) {
                attackPower *= ActiveMode switch { 1 => L(1.25f, 1.5f), 2 => L(1.45f, 1.9f), _ => L(1.5f, 2.2f) + skillHits * .2f };
                attackRange *= ActiveMode == 3 ? 1.35f : 1.1f; impactStatus = 2;
            }
            if (Kind == EarthshakerKind.Pepe) {
                attackPower *= ActiveMode == 2 ? L(1.5f, 1.9f) : L(2.2f, 3.4f) + Math.Min(4, skillHits) * L(.1f, .25f);
                attackRange *= ActiveMode == 3 ? 1 + Math.Min(4, skillHits) * .18f : 1.15f; impactStatus = ActiveMode == 3 ? 3 : 0;
            }
        }
        baseDamage = Math.Max(1, (int)(damage * attackPower)); knockback = kb;
        int id=Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem),Player.MountedCenter,Vector2.Zero,
            ModContent.ProjectileType<EarthshakerHeldHammer>(),baseDamage,knockback,Player.whoAmI,(int)Kind,Player.selectedItem);
        if(id<Main.maxProjectiles&&Main.projectile[id].ModProjectile is EarthshakerHeldHammer held)
            held.Configure(attackRange,impactStatus,Kind==EarthshakerKind.Zima&&ActiveMode==3?5:1);
    }
    internal void Released(){skillHits++;}
    public override void PostUpdate()
    {
        if(Player.whoAmI!=Main.myPlayer)return;
        if(chargeCooldown>0)chargeCooldown--;
        if(shakeTime>0){shakeTime--;shake*=.87f;}
        if(skillTime>0){if(ActiveMode==0||Player.CCed||Player.noItems)EndSkill();
            else{skillTime--;var shared=Player.GetModPlayer<WeaponPlayer>();shared.SkillActive=true;shared.SkillTimer=duration-skillTime;if(skillTime==0)EndSkill();}}
        bool key=ArknightsKeybinds.SkillActivatePressed(Player);if(key&&!keyWasDown)TryActivate();keyWasDown=key;
    }
    internal void CreditAttack()
    {
        if (!Holding || Player.whoAmI != Main.myPlayer || chargeCooldown > 0) return;
        chargeCooldown = 18;
        if (Player.GetModPlayer<WeaponPlayer>() is var shared && shared.CurrentSkill?.ChargeType == Systems.Gameplay.Skill.SkillChargeType.Attack) shared.OffensiveRecovery();
    }
    internal bool TryActivate()
    {
        var shared = Player.GetModPlayer<WeaponPlayer>();
        if (ArknightsKeybinds.SkillActivatePressed(Player) && keyWasDown) return false;
        if (!Holding || Player.whoAmI != Main.myPlayer || Busy || Player.noItems || Player.CCed || ActiveMode > 0
            || shared.CurrentSkill == null || shared.StockCount <= 0 || shared.SkillActive || shared.Skill == 0 && Kind != EarthshakerKind.Zima) return false;
        skillItem = Player.HeldItem; mode = shared.Skill + 1; level = Math.Clamp(shared.CurrentSkill.ForceReplaceLevel ?? shared.CurrentSkill.Level, 1, 10); skillHits = 0;
        duration = skillTime = Math.Max(1, (int)(shared.CurrentSkill.CurrentLevelData.ActiveTime * 60 * WeaponPlayer.ActiveDurationMultiplier));
        if (Kind == EarthshakerKind.Pepe && mode == 2) pepeUses = Math.Min(2, pepeUses + 1);
        shared.DelStockCount(); shared.SkillActive = true; shared.SkillTimer = 0;
        EarthshakerVisuals.Raise(Player.Center, Kind); return true;
    }
    public override void PostUpdateEquips()
    {
        if (ActiveMode == 2 && Kind == EarthshakerKind.Odda) Player.statDefense *= L(1.1f, 2);
        if (ActiveMode == 2 && Kind == EarthshakerKind.Zima) Player.statDefense *= L(1.3f, 1.6f);
    }
    private void EndSkill()
    {
        if (skillItem == Player.HeldItem) { var shared = Player.GetModPlayer<WeaponPlayer>(); shared.SkillActive = false; shared.SkillTimer = 0; }
        mode = skillTime = duration = skillHits = 0; skillItem = null;
    }
    internal void Shake(float force) { shake = Math.Max(shake, force); shakeTime = 20; }
    public override void ModifyScreenPosition()
    {
        if (shakeTime > 0 && Player.whoAmI == Main.myPlayer) Main.screenPosition += new Vector2(MathF.Sin(shakeTime * 2.4f) * .35f, MathF.Cos(shakeTime * 2.7f)) * shake;
    }
    public override void UpdateDead(){EarthshakerHeldHammer.Find(Player)?.Projectile.Kill();EndSkill();pepeUses=0;shakeTime=0;previousRight=RightHeld=LeftHeld=false;toolItem=null;}
}
