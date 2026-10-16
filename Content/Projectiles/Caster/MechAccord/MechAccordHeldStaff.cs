using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons;
using ArknightsMod.Content.Items.Weapons.Caster;
using ArknightsMod.Content.Projectiles.Caster.Goldenglow;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

public sealed class MechAccordHeldStaff : ModProjectile
{
    private Vector2 aim;
    private float pulseCooldown;
    private Item sourceItem;
    private Vector2 dragStart;
    private bool dragging;
    private int attackTicks;
    private int rockrockVolley;
    internal bool Bursting => attackTicks % 240 >= 180;
    internal NPC LockedTarget { get; private set; }
    internal Vector2 Aim => aim;
    // Fallback texture; PreDraw selects the matching vanilla staff for the held weapon.
    public override string Texture => "Terraria/Images/Item_" + ItemID.InfernoFork;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 32;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.penetrate = -1;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    public override void SendExtraAI(BinaryWriter writer) { writer.Write(aim.X); writer.Write(aim.Y); }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Vector2 value = new(reader.ReadSingle(), reader.ReadSingle());
        if (float.IsFinite(value.X) && float.IsFinite(value.Y)) aim = value;
    }

    private Vector2 StaffTip(Player player, MechAccordStaffBase staff)
    {
        if (Main.dedServ)
            return Projectile.Center + new Vector2(-6.6f * player.direction, -37.2f * player.gravDir);
        Texture2D texture = TextureAssets.Item[staff.IsRockrock ? ItemID.SpectreStaff : ItemID.InfernoFork].Value;
        float rotation = -MathHelper.PiOver4 * player.direction * player.gravDir;
        return VerticalStaffBase.HeldStaffPoint(player, texture, Projectile.Center,
            staff.VerticalTip, rotation, staff.VerticalOrigin, 1f);
    }

    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        bool owned = Projectile.owner == Main.myPlayer;
        if (!player.active || player.dead || player.noItems || player.CCed ||
            player.HeldItem.ModItem is not MechAccordStaffBase staff ||
            player.selectedItem != (int)Projectile.ai[1] ||
            (owned && (sourceItem != null && sourceItem != player.HeldItem)))
        { Projectile.Kill(); return; }
        sourceItem ??= player.HeldItem;
        Projectile.timeLeft = 2;
        Projectile.spriteDirection = player.direction;
        Projectile.Center = VerticalStaffBase.HeldStaffCenter(player, staff);
        player.heldProj = Projectile.whoAmI;
        player.itemTime = player.itemAnimation = 2;
        Vector2 tip = StaffTip(player, staff);
        if (!staff.IsRockrock)
        {
            if (owned)
            {
                Vector2 offset = Main.MouseWorld - player.MountedCenter;
                aim = player.MountedCenter + offset.SafeNormalize(Vector2.Zero) * Math.Min(staff.Reach, offset.Length());
                aim = Vector2.Clamp(aim, new Vector2(16), new Vector2((Main.maxTilesX-2)*16,(Main.maxTilesY-2)*16));
                if (!dragging) { dragging=true;dragStart=aim; }
                ClickDrone drone = ClickDrone.Ensure(player);
                if (!player.channel)
                {
                    WeaponPlayer skill = player.GetModPlayer<WeaponPlayer>();
                    drone?.Begin(ClickSelection.Targets(ClickSelection.Area(dragStart,aim),player,staff.Reach),
                        player.GetWeaponDamage(player.HeldItem),skill.SkillActive&&skill.Skill==1,
                        MathHelper.Clamp(player.GetWeaponAttackSpeed(player.HeldItem),.5f,2f)*staff.AttackSpeedBonus(skill));
                    Projectile.Kill();return;
                }
                player.ChangeDir(aim.X>=player.Center.X?1:-1);
                if ((int)Projectile.ai[0]%6==0) Projectile.netUpdate=true;
            }
            Projectile.ai[0]++;return;
        }
        if(owned&&!player.channel){Projectile.Kill();return;}
        if (owned)
        {
            Vector2 offset = Main.MouseWorld - player.MountedCenter;
            if (offset.LengthSquared() > staff.Reach * staff.Reach)
                offset = offset.SafeNormalize(Vector2.UnitX) * staff.Reach;
            aim = player.MountedCenter + offset;
            player.ChangeDir(aim.X >= player.Center.X ? 1 : -1);
            if ((int)Projectile.ai[0] % 6 == 0) Projectile.netUpdate = true;
            // Item use paid the first installment; keep the hold's later payments in the normal mana pipeline.
            if (Projectile.ai[0] > 0 && (int)Projectile.ai[0] % 14 == 0 &&
                !player.CheckMana(player.HeldItem, -1, true))
            { player.channel = false; Projectile.Kill(); return; }
            player.manaRegenDelay = player.maxRegenDelay;
            EnsureDrone(player, staff);
        }
        WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
        LockedTarget = GoldenglowHeldStaff.FindTargetNPC(aim, player.Center, staff.Reach, staff.SearchRadius);
        attackTicks++;
        bool burstStart=attackTicks%240==180;
        if(burstStart&&!Main.dedServ) MechAccordVisuals.Emit(tip,true);
        if(Bursting&&LockedTarget!=null&&!Main.dedServ&&attackTicks%4==0)
        {
            float angle=attackTicks*.14f;
            for(int i=0;i<5;i++)
            {
                Vector2 radial=(angle+MathHelper.TwoPi*i/5).ToRotationVector2();
                global::ArknightsMod.Common.VisualEffects.CasterSparkles.Spawn(aim+radial*32,
                    new Vector2(-radial.Y,radial.X)*.65f,MechAccordVfx.Amber,.4f,14,angle);
            }
        }
        if (owned)
        {
            pulseCooldown--;
            if (pulseCooldown <= 0 || burstStart)
            {
                int damage = player.GetWeaponDamage(player.HeldItem);
                RockrockStarBolt.SpawnFan(Projectile.GetSource_FromThis(), tip, LockedTarget,
                    player.whoAmI, (int)(damage*(Bursting?1.25f:1f)), player.GetWeaponKnockback(player.HeldItem),
                    Bursting, rockrockVolley++);
                pulseCooldown = Math.Max(4f, (Bursting?6:staff.PulseInterval) /
                    (MathHelper.Clamp(player.GetWeaponAttackSpeed(player.HeldItem), .5f, 2f) * staff.AttackSpeedBonus(skills)));
            }
        }
        else if (pulseCooldown > 0) pulseCooldown--;
        if (!Main.dedServ)
        {
            Lighting.AddLight(tip, (staff.IsRockrock ? new Vector3(.75f, .26f, .08f) :
                new Vector3(.12f, .48f, .55f)) * .52f);
            if ((int)Projectile.ai[0] % 8 == 0)
                MechAccordVisuals.Charge(tip, staff.IsRockrock);
        }
        Projectile.ai[0]++;
    }

    private static void EnsureDrone(Player player, MechAccordStaffBase staff)
    {
        int type = ModContent.ProjectileType<MechAccordDrone>();
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == player.whoAmI && projectile.type == type) return;
        Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), player.Center,
            Vector2.Zero, type, 0, 0f, player.whoAmI, staff.IsRockrock ? 1f : 0f);
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Player player = Main.player[Projectile.owner];
        if (player.HeldItem.ModItem is not MechAccordStaffBase staff) return false;
        int heldStaffItem = staff.IsRockrock ? ItemID.SpectreStaff : ItemID.InfernoFork;
        Texture2D texture = TextureAssets.Item[heldStaffItem].Value;
        float rotation = -MathHelper.PiOver4 * player.direction * player.gravDir;
        VerticalStaffBase.DrawHeldStaff(player, texture, Projectile.Center, lightColor,
            Vector2.Zero, rotation, new Vector2(.28f, .75f));
        Vector2 tip = StaffTip(player, staff);
        MechAccordVfx.DrawLens(tip, staff.IsRockrock, 24f, (float)Projectile.ai[0]);
        if(!staff.IsRockrock&&Projectile.owner==Main.myPlayer&&dragging)
            ClickSelection.Draw(ClickSelection.Area(dragStart,aim));
        if(staff.IsRockrock&&Bursting)
            MechAccordVfx.DrawBurst(aim,(attackTicks%60)/60f,true,96f);
        return false;
    }
}
