using System;
using ArknightsMod.Content.Items.Weapons.Caster;
using ArknightsMod.Content.Projectiles.Caster.Goldenglow;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.MechAccord;

public sealed class MechAccordDrone : ModProjectile
{
    // Explicit placeholder: both operators borrow Goldenglow's existing drone sprite.
    public override string Texture => "ArknightsMod/Content/Items/Weapons/Caster/Goldenglow/GoldenglowBeacon";
    private Item sourceItem;
    private float cooldown = 16f;
    private int consecutiveHits;
    private int lockedIndex;
    internal bool HasLockedTarget => lockedIndex > 0 && lockedIndex <= Main.maxNPCs
        && Main.npc[lockedIndex - 1].CanBeChasedBy();

    internal static void RegisterHit(int owner, int npcIndex)
    {
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == owner && projectile.ModProjectile is MechAccordDrone drone &&
                drone.lockedIndex == npcIndex + 1)
                drone.consecutiveHits = Math.Min(14, drone.consecutiveHits + 1);
    }

    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailCacheLength[Type] = 18;
        ProjectileID.Sets.TrailingMode[Type] = 2;
    }

    public override void SetDefaults()
    {
        Projectile.width = 14;
        Projectile.height = 24;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 120;
        Projectile.netImportant = true;
    }
    public override bool? CanDamage() => false;
    public override void AI()
    {
        Player player = Main.player[Projectile.owner];
        bool owned = Projectile.owner == Main.myPlayer;
        if (!player.active || player.dead || player.noItems || player.CCed ||
            player.HeldItem.ModItem is not MechAccordStaffBase staff || !staff.IsRockrock ||
            (owned && sourceItem != null && sourceItem != player.HeldItem))
        { Projectile.Kill(); return; }
        sourceItem ??= player.HeldItem;
        Projectile.timeLeft = 120;
        if (!owned)
        {
            Lighting.AddLight(Projectile.Center, staff.IsRockrock ? new Vector3(.6f, .18f, .04f) : new Vector3(.08f, .4f, .48f));
            return;
        }
        MechAccordHeldStaff held = null;
        foreach (Projectile projectile in Main.ActiveProjectiles)
            if (projectile.owner == player.whoAmI && projectile.ModProjectile is MechAccordHeldStaff candidate)
            { held = candidate; break; }
        if (held == null) { Projectile.Kill(); return; }
        NPC target = held.LockedTarget;
        int targetIndex = target?.whoAmI + 1 ?? 0;
        if (targetIndex != lockedIndex) { lockedIndex = targetIndex; consecutiveHits = 0; Projectile.netUpdate = true; }
        Vector2 destination;
        float speed;
        if (target == null)
        {
            destination = player.Center + new Vector2(34f * player.direction, -57f);
            speed = 22f;
        }
        else
        {
            float radius = Math.Max(target.width, target.height) * .5f + 48f;
            float angle = (float)Main.GameUpdateCount * .017f + (staff.IsRockrock ? MathHelper.Pi : 0f);
            destination = target.Center + angle.ToRotationVector2() * radius;
            speed = Projectile.Distance(target.Center) > radius + 90f ? 34f : 6f;
        }
        Vector2 delta = destination - Projectile.Center;
        Projectile.velocity = delta.SafeNormalize(Vector2.Zero) * Math.Min(speed, delta.Length() * .25f);
        Projectile.rotation = Projectile.velocity.X * .012f;
        if (Main.GameUpdateCount % 6 == 0) Projectile.netUpdate = true;
        if (target != null && Projectile.Distance(target.Center) < Math.Max(target.width, target.height) + 120f)
        {
            cooldown--;
            if (cooldown <= 0)
            {
                WeaponPlayer skills = player.GetModPlayer<WeaponPlayer>();
                float cap = staff.DroneCap(skills);
                float multiplier = Math.Min(cap, .2f + consecutiveHits * .15f) * staff.DroneDamageBonus(skills);
                int damage = Math.Max(1, (int)(player.GetWeaponDamage(player.HeldItem) * multiplier));
                bool stun = !staff.IsRockrock && skills.SkillActive && skills.Skill == 1;
                RockrockRailPulse.Spawn(Projectile.GetSource_FromThis(), Projectile.Center, target,
                    player.whoAmI, damage);
                cooldown = Math.Max(5f, (held.Bursting?7:staff.PulseInterval) /
                    (MathHelper.Clamp(player.GetWeaponAttackSpeed(player.HeldItem), .5f, 2f) * staff.AttackSpeedBonus(skills)));
                if (!Main.dedServ && staff.IsRockrock && staff.DroneDamageBonus(skills) > 1f)
                    MechAccordVisuals.Overload(Projectile.Center);
            }
        }
        Lighting.AddLight(Projectile.Center, staff.IsRockrock ? new Vector3(.6f, .18f, .04f) : new Vector3(.08f, .4f, .48f));
    }

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
        bool rockrock = Projectile.ai[0] > 0;
        Color tint = rockrock ? new Color(255, 190, 145) : new Color(160, 245, 255);
        Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
            Color.Lerp(lightColor, tint, .52f), Projectile.rotation, texture.Size() * .5f,
            Projectile.scale, SpriteEffects.None);
        MechAccordVfx.DrawLens(Projectile.Center, rockrock, 16f, (float)Main.GameUpdateCount);
        return false;
    }
}
