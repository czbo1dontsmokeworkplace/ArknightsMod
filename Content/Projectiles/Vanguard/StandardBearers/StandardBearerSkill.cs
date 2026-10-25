using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Vanguard.StandardBearers;
using ArknightsMod.Content.NPCs.Friendly;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Vanguard.StandardBearers;

// A synchronized, harmless controller. The server alone creates currency and heals;
// remote clients never need to read another player's unsynchronized WeaponPlayer.
public sealed class StandardBearerSkill : ModProjectile
{
    public override string Texture => "Terraria/Images/MagicPixel";
    public StandardBearerKind Kind => (StandardBearerKind)(int)Projectile.ai[0];
    public int Skill => (int)Projectile.ai[1];
    private int duration, emitted, boundSlot;
    private Item boundItem;
    public bool Valid(Player owner) => Projectile.active && duration > 0 && owner.active && !owner.dead
        && !owner.CCed && !owner.noItems && owner.selectedItem == boundSlot
        && owner.HeldItem.ModItem is StandardBearerWeapon weapon && weapon.Kind == Kind
        && (owner.whoAmI != Main.myPlayer || ((boundItem == null || ReferenceEquals(boundItem, owner.HeldItem))
            && owner.GetModPlayer<WeaponPlayer>().SkillActive && owner.GetModPlayer<WeaponPlayer>().Skill == Skill));
    public void Configure(Player owner, int ticks)
    {
        duration = Math.Clamp(ticks, 1, 1800); boundSlot = owner.selectedItem; boundItem = owner.HeldItem;
        owner.GetModPlayer<StandardBearerPlayer>().Active = this;
        Projectile.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter writer) { writer.Write((short)duration); writer.Write((byte)boundSlot); }
    public override void ReceiveExtraAI(BinaryReader reader)
    { duration = Math.Clamp((int)reader.ReadInt16(), 1, 1800); boundSlot = Math.Clamp((int)reader.ReadByte(), 0, 58); }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 2; Projectile.timeLeft = 1900;
        Projectile.tileCollide = false; Projectile.ignoreWater = true; Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override bool PreDraw(ref Color lightColor) => false;
    public override void AI()
    {
        if (duration == 0) return; // wait for ExtraAI on remote peers
        Player owner = Main.player[Projectile.owner];
        if (!Valid(owner) || (uint)(int)Kind > 3 || Skill < 0 || Skill > (Kind == StandardBearerKind.Saileach ? 2 : 1))
        { Projectile.Kill(); return; }
        if (owner.whoAmI == Main.myPlayer)
        {
            WeaponPlayer mp = owner.GetModPlayer<WeaponPlayer>();
            if (!mp.SkillActive || mp.Skill != Skill || mp.CurrentSkill?.Key.Item != owner.HeldItem.ModItem.Name)
            { Projectile.Kill(); return; }
        }
        owner.GetModPlayer<StandardBearerPlayer>().Active = this;
        Projectile.Center = owner.Center;
        Projectile.timeLeft = 2;
        if (!global::ArknightsMod.Systems.Gameplay.MujicaFeverSystem.PauseSkill(owner)) Projectile.ai[2]++;
        int elapsed = (int)Projectile.ai[2];
        int realTicks = (int)++Projectile.localAI[0];
        if (Main.netMode != NetmodeID.MultiplayerClient)
        {
            int total = StandardBearerRules.CostTotal(Kind, Skill);
            int owed = Kind == StandardBearerKind.Saileach && Skill == 2 ? total
                : StandardBearerRules.Earned(total, elapsed, duration);
            if (owed > emitted)
            {
                DeploymentCost.SpawnAmount(Projectile.GetSource_FromThis(), owner.Center, owed - emitted);
                emitted = owed;
            }
            if (Skill == 1 && Kind != StandardBearerKind.Elysium && realTicks % 60 == 0) HealLowestAlly(owner);
        }
        // Damage modifiers must also exist on the attacking client's NPC instance.
        if ((Kind == StandardBearerKind.Elysium && Skill == 1)
            || (Kind == StandardBearerKind.Saileach && Skill == 2)) ApplyEnemySupport(owner);
        if (elapsed >= duration) Projectile.Kill();
    }
    private void HealLowestAlly(Player owner)
    {
        Player target = null;
        foreach (Player ally in Main.ActivePlayers)
            if (StandardBearerPlayer.Allied(owner, ally) && ally.Distance(owner.Center) <= StandardBearerRules.AuraRange
                && ally.statLife < ally.statLifeMax2 && (target == null
                    || ally.statLife / (float)ally.statLifeMax2 < target.statLife / (float)target.statLifeMax2)) target = ally;
        if (target == null) return;
        // Terraria health scale: small recovery rather than importing Arknights' HP scale.
        int amount = Math.Min(target.statLifeMax2 - target.statLife, Kind == StandardBearerKind.Myrtle ? 3 : 5);
        target.Heal(amount);
        if (Main.netMode == NetmodeID.Server) NetMessage.SendData(MessageID.PlayerLifeMana, number: target.whoAmI);
    }
    private void ApplyEnemySupport(Player owner)
    {
        int count = 0;
        foreach (NPC npc in Main.ActiveNPCs)
        {
            if (!npc.CanBeChasedBy() || npc.Distance(owner.Center) > StandardBearerRules.AuraRange) continue;
            if (Kind == StandardBearerKind.Elysium && count++ >= 4) break;
            npc.GetGlobalNPC<StandardBearerEnemySupport>().Mark(Kind == StandardBearerKind.Elysium ? .35f : 0,
                Kind == StandardBearerKind.Saileach ? .3f : 0);
        }
    }
    public override void OnKill(int timeLeft)
    {
        Player owner = Main.player[Projectile.owner];
        if (owner.GetModPlayer<StandardBearerPlayer>().Active == this)
            owner.GetModPlayer<StandardBearerPlayer>().Active = null;
        if (owner.whoAmI == Main.myPlayer && owner.HeldItem.ModItem is StandardBearerWeapon weapon && weapon.Kind == Kind)
        {
            WeaponPlayer mp = owner.GetModPlayer<WeaponPlayer>();
            if (mp.Skill == Skill) mp.SkillActive = false;
        }
    }
}

public sealed class StandardBearerEnemySupport : GlobalNPC
{
    public override bool InstancePerEntity => true;
    private int shredTicks, vulnerableTicks;
    private float shred, vulnerable;
    public void Mark(float defenseShred, float damageTaken)
    {
        if (defenseShred > 0) { shredTicks = 2; shred = Math.Max(shred, defenseShred); }
        if (damageTaken > 0) { vulnerableTicks = 2; vulnerable = Math.Max(vulnerable, damageTaken); }
    }
    public override void PostAI(NPC npc)
    {
        if ((shredTicks > 0 || vulnerableTicks > 0) && !npc.boss && npc.knockBackResist > 0) npc.velocity *= .85f;
        if (shredTicks > 0 && --shredTicks == 0) shred = 0;
        if (vulnerableTicks > 0 && --vulnerableTicks == 0) vulnerable = 0;
    }
    public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
    {
        if (shredTicks > 0) modifiers.DefenseEffectiveness *= 1 - shred;
        if (vulnerableTicks > 0) modifiers.FinalDamage *= 1 + vulnerable;
    }
}
