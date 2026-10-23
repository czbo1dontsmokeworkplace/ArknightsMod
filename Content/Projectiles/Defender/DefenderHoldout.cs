using System;
using System.IO;
using System.Collections.Generic;
using ArknightsMod.Content.SwingHelper;
using ArknightsMod.Content.Items.Weapons.Defender.Horn;
using ArknightsMod.Systems.Gameplay.Damage;
using ArknightsMod.Common.VisualEffects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Vertex = ArknightsMod.Content.SwingHelper.Vertex;

namespace ArknightsMod.Content.Projectiles.Defender;

// ai[0]: held item type, ai[1]: idle / guard / attack, ai[2]: attack time remaining.
public sealed class DefenderHoldout : ModProjectile
{
    private readonly ShieldHelper shield = new();
    private readonly HashSet<int> hitGroups = new();
    private float aim, duration = 30f, bladeRotation;
    private Vector2 hand;
    private bool arts, impact;
    private readonly Vector2[] trailHands = new Vector2[20];
    private readonly Vector2[] trailTips = new Vector2[20];
    private int trailCount;
    private Player Owner => Main.player[Projectile.owner];
    internal bool Attacking => Projectile.ai[1] == 2f && Projectile.ai[2] > 0;
    internal bool Guarding => Projectile.ai[1] == 1f;
    internal bool MainWeaponInHands => Attacking
        || (DefenderRules.IsFortress(Owner.HeldItem)
            && (Owner.itemAnimation > 0 || Owner.GetModPlayer<HornLauncherPlayer>().Dumping))
        || Owner.GetModPlayer<EunectesDashPlayer>().Dashing;
    public override string Texture => "ArknightsMod/Content/Projectiles/Defender/Caridgan/Cardi_Shield";
    internal static DefenderHoldout Find(Player player)
    {
        foreach (Projectile p in Main.ActiveProjectiles)
            if (p.owner == player.whoAmI && p.ModProjectile is DefenderHoldout held && (int)p.ai[0] == player.HeldItem.type)
                return held;
        return null;
    }
    internal static bool IsBusy(Player player) => Find(player)?.Attacking == true;
    internal static DefenderHoldout Ensure(Player player)
    {
        var held = Find(player);
        if (held != null || player.whoAmI != Main.myPlayer || !DefenderRules.IsWeapon(player.HeldItem)) return held;
        int i = Projectile.NewProjectile(player.GetSource_ItemUse(player.HeldItem), player.MountedCenter, Vector2.Zero,
            ModContent.ProjectileType<DefenderHoldout>(), 0, 0f, player.whoAmI, player.HeldItem.type);
        return Main.projectile.IndexInRange(i) ? Main.projectile[i].ModProjectile as DefenderHoldout : null;
    }
    public override void SetDefaults()
    {
        shield.SetDefaults(Projectile, -1);
        Projectile.width = Projectile.height = 20;
        Projectile.hide = true;
        Projectile.timeLeft = 2;
    }
    internal void BeginAttack(int damage, float knockback)
    {
        Player p = Owner;
        if (Attacking || ShieldGuardRules.IsRaised(p) || p.GetModPlayer<DefenderCombatPlayer>().StopsAttacking) return;
        aim = (Main.MouseWorld - p.MountedCenter).ToRotation();
        p.ChangeDir(MathF.Cos(aim) >= 0 ? 1 : -1);
        duration = Math.Max(10, p.HeldItem.useAnimation / Math.Max(.1f, p.GetTotalAttackSpeed(p.HeldItem.DamageType)));
        Projectile.ai[1] = 2;
        Projectile.ai[2] = duration;
        Projectile.damage = damage;
        Projectile.knockBack = knockback;
        Projectile.CritChance = p.GetWeaponCrit(p.HeldItem);
        Projectile.DamageType = DefenderRules.AttackClass(p);
        arts = Projectile.DamageType == DamageClass.Magic;
        hitGroups.Clear();
        trailCount = 0;
        Array.Clear(Projectile.localNPCImmunity);
        impact = false;
        if (p.HeldItem.ModItem?.Name == "LiskarmPistol")
        {
            Vector2 direction = aim.ToRotationVector2();
            Projectile.NewProjectile(p.GetSource_ItemUse(p.HeldItem), p.MountedCenter + direction * 22f,
                direction * 20f, ModContent.ProjectileType<DefenderElectron>(), damage, knockback,
                p.whoAmI, DefenderRules.Skill(p, 1) ? 1f : 0f);
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = .45f }, p.Center);
        }
        else SoundEngine.PlaySound(SoundID.Item1, p.Center);
        Projectile.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    { writer.Write(aim); writer.Write(duration); writer.Write(arts); }
    public override void ReceiveExtraAI(BinaryReader reader)
    { aim = reader.ReadSingle(); duration = reader.ReadSingle(); arts = reader.ReadBoolean(); }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player p = Owner;
        if (!p.active || p.dead || p.CCed || p.noItems || p.HeldItem.type != (int)Projectile.ai[0] || !DefenderRules.IsWeapon(p.HeldItem))
        { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        if (Projectile.owner == Main.myPlayer)
        {
            float nextAim = (Main.MouseWorld - p.MountedCenter).ToRotation();
            if (!Attacking && MathF.Abs(MathHelper.WrapAngle(nextAim - aim)) > .04f)
            { aim = nextAim; Projectile.netUpdate = true; }
            if (!Attacking)
            {
                float next = ShieldGuardRules.WantsRaised(p) ? 1f : 0f;
                if (Projectile.ai[1] != next) { Projectile.ai[1] = next; Projectile.netUpdate = true; }
            }
        }
        if (Guarding || Attacking || (DefenderRules.IsFortress(p.HeldItem) && p.itemAnimation > 0))
            p.ChangeDir(MathF.Cos(aim) >= 0 ? 1 : -1);
        // These are the same front-arm movement and guarding methods used by Cardigan/Durnar.
        if (Guarding) shield.UpdateDefenderPose(Projectile, p, aim);
        else shield.UpdateMovePose(Projectile, p);
        float arm = 0f;
        bladeRotation = MathHelper.PiOver2 + arm;
        if (Attacking)
        {
            float progress = 1f - Projectile.ai[2] / Math.Max(1f, duration);
            if (p.HeldItem.ModItem?.Name == "LiskarmPistol") bladeRotation = aim;
            else bladeRotation = aim + MathHelper.Lerp(-1.9f, 1.8f, MathHelper.SmoothStep(0, 1, progress)) * p.direction;
            arm = bladeRotation - MathHelper.PiOver2;
            Projectile.ai[2]--;
            if (Projectile.ai[2] <= 0) { Projectile.ai[1] = 0; Projectile.netUpdate = true; }
        }
        else if (DefenderRules.IsFortress(p.HeldItem) && MainWeaponInHands)
        { bladeRotation = aim; arm = aim - MathHelper.PiOver2; }
        // An idle back-mounted weapon must not override the player's free arm.
        if (MainWeaponInHands)
            p.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, arm);
        hand = p.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, arm);
        if (Attacking && DefenderRules.MainArt(p.HeldItem) != null
            && !DefenderRules.IsFortress(p.HeldItem) && p.HeldItem.ModItem?.Name != "LiskarmPistol")
        {
            for (int i = trailHands.Length - 1; i > 0; i--)
            { trailHands[i] = trailHands[i - 1]; trailTips[i] = trailTips[i - 1]; }
            trailHands[0] = hand - bladeRotation.ToRotationVector2() * 12f;
            trailTips[0] = hand + bladeRotation.ToRotationVector2() * (DefenderRules.WeaponLength(p.HeldItem) - 12f);
            trailCount = Math.Min(trailCount + 1, trailHands.Length);
        }
        else trailCount = 0;
        if (p.HeldItem.ModItem?.Name == "NoirShield" && Attacking)
        {
            float extension = MathF.Sin((1f - Projectile.ai[2] / duration) * MathHelper.Pi);
            Projectile.Center = p.MountedCenter + aim.ToRotationVector2() * (16f + extension * 32f);
            Projectile.rotation = aim - MathHelper.PiOver2 + MathHelper.PiOver2 * p.direction;
            p.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
            hand = p.MountedCenter;
            bladeRotation = aim;
        }
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = arts;
        Projectile.DamageType = arts ? DamageClass.Magic : DamageClass.Melee;
        shield.mp.ParryThemeColor = DefenderRules.Color(p.HeldItem);
        if (p.HeldItem.ModItem is HornGrenadeLauncher)
            Horn.HornVisuals.SkillBody(p, p.GetModPlayer<HornLauncherPlayer>().VisualState, ++Projectile.localAI[0]);
    }
    public override bool? CanDamage() => Attacking && !Guarding && Owner.HeldItem.ModItem?.Name != "LiskarmPistol"
        && !DefenderRules.IsFortress(Owner.HeldItem) && Projectile.ai[2] < duration * .85f && Projectile.ai[2] > duration * .1f;
    public override bool? CanHitNPC(NPC target) => hitGroups.Contains(target.realLife >= 0 ? target.realLife : target.whoAmI) ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collision = 0;
        float reach = Owner.HeldItem.ModItem?.Name == "NoirShield" ? 64f : DefenderRules.WeaponLength(Owner.HeldItem) - 12f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), hand,
            hand + bladeRotation.ToRotationVector2() * reach, 22f, ref collision);
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitGroups.Add(target.realLife >= 0 ? target.realLife : target.whoAmI);
        if (Projectile.owner != Main.myPlayer) return;
        string name = Owner.HeldItem.ModItem.Name;
        if (name == "Vulcan_Weapon" && DefenderRules.Skill(Owner, 1)) Owner.GetModPlayer<DefenderCombatPlayer>().HealSelf(.04f, 30);
        if (name == "MudrockHammer") Owner.GetModPlayer<DefenderCombatPlayer>().HealSelf(DefenderRules.Skill(Owner, 1) ? .06f : .02f, 60);
        if (!impact && ((name == "CroissantHammer" && DefenderRules.Skill(Owner, 1))
            || (name == "MudrockHammer" && (DefenderRules.Skill(Owner, 1) || DefenderRules.Skill(Owner, 2)))))
        {
            impact = true;
            DefenderPulse.Spawn(Owner, target.Center, Projectile.damage, name == "MudrockHammer" ? 140 : 105, false, target.whoAmI);
        }
    }
    public override void DrawBehind(int index, List<int> behindNPCsAndTiles, List<int> behindNPCs,
        List<int> behindProjectiles, List<int> overPlayers, List<int> overWiresUI) => overPlayers.Add(index);
    private void DrawSwingTrail(SpriteBatch sb)
    {
        if (!Attacking || trailCount < 2) return;
        // Preserve the legacy Durnar purple ribbon and Cardigan/Cuora weapon trails
        // on the actual common-template hand/tip history, without changing hit geometry.
        Color color = Owner.HeldItem.ModItem?.Name switch
        {
            "DN_Weapon" => Color.Purple,
            "NianWeapon" => new Color(240, 60, 50),
            "BeagleWeapon" => Color.Gray,
            "CardiWeapon" or "CuoraWeapon" => new Color(74, 74, 74),
            _ => Color.White
        };
        var vertices = new Vertex[trailCount * 2];
        for (int i = 0; i < trailCount; i++)
        {
            float progress = i / (float)trailHands.Length;
            Color fade = color * (1f - progress);
            vertices[i * 2] = new Vertex(trailHands[i] - Main.screenPosition, new Vector3(progress, 0, 1), fade);
            vertices[i * 2 + 1] = new Vertex(trailTips[i] - Main.screenPosition, new Vector3(progress, 1, 1), fade);
        }
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.AnisotropicClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        Main.graphics.GraphicsDevice.Textures[0] = ModContent.Request<Texture2D>(
            "ArknightsMod/Content/Projectiles/Defender/Durnar/Extra_209").Value;
        Main.graphics.GraphicsDevice.DrawUserPrimitives(PrimitiveType.TriangleStrip, vertices, 0, vertices.Length - 2);
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Player p = Owner;
        SpriteBatch sb = Main.spriteBatch;
        sb.End();
        sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.AnisotropicClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        DrawSwingTrail(sb);
        if (MainWeaponInHands && DefenderRules.MainArt(p.HeldItem) is string path)
        {
            Texture2D texture = ModContent.Request<Texture2D>(path).Value;
            bool gun = DefenderRules.IsFortress(p.HeldItem) || p.HeldItem.ModItem?.Name == "LiskarmPistol";
            if (!gun) shield.DrawMainWeapon(p, texture, hand, bladeRotation, lightColor, DefenderRules.WeaponLength(p.HeldItem));
            else
            {
                float rotation = bladeRotation;
                if (DefenderRules.IsFortress(p.HeldItem)) rotation += p.direction == 1 ? -MathHelper.ToRadians(25) : MathHelper.ToRadians(25);
                Main.EntitySpriteDraw(texture, hand + bladeRotation.ToRotationVector2() * 10f - Main.screenPosition,
                    null, lightColor, rotation, texture.Size() * .5f, 1f, p.direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None);
            }
        }
        if (DefenderRules.ShieldArt(p.HeldItem) is string shieldPath)
        {
            Texture2D texture = ModContent.Request<Texture2D>(shieldPath).Value;
            shield.DrawShield(Projectile, p, texture, Guarding);
            shield.mp.DrawEffect(texture);
        }
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        return false;
    }
}
