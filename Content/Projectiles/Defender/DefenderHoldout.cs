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
    private readonly HashSet<int> routeEffectHits = new();
    private int attackSequence, lastSkill = -1;
    internal void SyncSkill() => Projectile.netUpdate = true;
    private float aim, duration = 30f, bladeRotation;
    private Vector2 hand;
    private bool arts, impact, shieldBash;
    private byte hornVisual;
    private readonly Vector2[] trailHands = new Vector2[20];
    private readonly Vector2[] trailTips = new Vector2[20];
    private int trailCount;
    private Player Owner => Main.player[Projectile.owner];
    internal bool Attacking => Projectile.ai[1] == 2f && Projectile.ai[2] > 0;
    internal bool Guarding => Projectile.ai[1] == 1f;
    internal bool MainWeaponInHands => (Attacking && !shieldBash)
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
        Projectile.netImportant = true;
        Projectile.timeLeft = 2;
    }
    internal void BeginAttack(int damage, float knockback)
    {
        Player p = Owner;
        if (Attacking || ShieldGuardRules.WantsRaised(p) || p.GetModPlayer<DefenderCombatPlayer>().StopsAttacking) return;
        shieldBash = p.HeldItem.ModItem?.Name == "DN_Weapon" && DefenderRules.Skill(p, 1);
        aim = (Main.MouseWorld - p.MountedCenter).ToRotation();
        p.ChangeDir(MathF.Cos(aim) >= 0 ? 1 : -1);
        duration = Math.Max(10, p.HeldItem.useAnimation / Math.Max(.1f, p.GetTotalAttackSpeed(p.HeldItem.DamageType) * DefenderSkillRules.Stats(p).Speed));
        Projectile.ai[1] = 2;
        Projectile.ai[2] = duration;
        Projectile.damage = damage;
        Projectile.knockBack = knockback;
        Projectile.CritChance = p.GetWeaponCrit(p.HeldItem);
        Projectile.DamageType = DefenderRules.AttackClass(p);
        arts = Projectile.DamageType == DamageClass.Magic;
        hitGroups.Clear();
        routeEffectHits.Clear();
        attackSequence++;
        trailCount = 0;
        Array.Clear(Projectile.localNPCImmunity);
        impact = false;
        if (DefenderRules.IsSentinel(p.HeldItem))
        {
            Vector2 direction = aim.ToRotationVector2();
            DefenderRouteEffects.FireSentinel(p, direction, damage, knockback);
            SoundEngine.PlaySound(SoundID.Item93 with { Volume = .45f }, p.Center);
        }
        else SoundEngine.PlaySound(SoundID.Item1, p.Center);
        DefenderRouteEffects.FireShalem(p, aim.ToRotationVector2(), damage);
        Projectile.netUpdate = true;
    }
    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write(aim); writer.Write(duration); writer.Write(arts); writer.Write(attackSequence);
        writer.Write(DefenderRules.SkillActive(Owner) ? DefenderRules.SkillIndex(Owner) : -1);
        writer.Write(DefenderRules.SkillTime(Owner)); writer.Write(shieldBash);
        writer.Write(Owner.GetModPlayer<DefenderCombatPlayer>().DefenseStacks);
        writer.Write(Owner.GetModPlayer<DefenderCombatPlayer>().ResonanceStacks);
        writer.Write(Projectile.owner == Main.myPlayer ? Owner.GetModPlayer<HornLauncherPlayer>().VisualState : hornVisual);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        aim = reader.ReadSingle(); duration = reader.ReadSingle(); arts = reader.ReadBoolean();
        int sequence = reader.ReadInt32();
        if (sequence != attackSequence) { routeEffectHits.Clear(); hitGroups.Clear(); trailCount = 0; attackSequence = sequence; }
        var state = Owner.GetModPlayer<DefenderCombatPlayer>();
        state.RemoteSkill = reader.ReadInt32(); state.RemoteTime = reader.ReadInt32(); shieldBash = reader.ReadBoolean();
        state.DefenseStacks = Math.Clamp(reader.ReadInt32(), 0, 20);
        state.ResonanceStacks = Math.Clamp(reader.ReadInt32(), 0, 10);
        hornVisual = reader.ReadByte();
        state.RemoteItem = (int)Projectile.ai[0]; state.RemoteUntil = Main.GameUpdateCount + 90;
    }
    public override bool ShouldUpdatePosition() => false;
    public override void AI()
    {
        Player p = Owner;
        if (!p.active || p.dead || p.CCed || p.noItems || p.HeldItem.type != (int)Projectile.ai[0] || !DefenderRules.IsWeapon(p.HeldItem))
        { Projectile.Kill(); return; }
        Projectile.timeLeft = 2;
        if (Projectile.owner == Main.myPlayer && Attacking && ShieldGuardRules.WantsRaised(p))
        {
            Projectile.ai[1] = 1f; Projectile.ai[2] = 0f; Projectile.damage = 0;
            trailCount = 0; Projectile.netUpdate = true;
        }
        if (Projectile.owner == Main.myPlayer)
        {
            int skill = DefenderRules.SkillActive(p) ? DefenderRules.SkillIndex(p) : -1;
            if (skill != lastSkill || (skill >= 0 && Main.GameUpdateCount % 30 == 0))
            { lastSkill = skill; Projectile.netUpdate = true; }
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
            if (DefenderRules.IsSentinel(p.HeldItem)) bladeRotation = aim;
            else bladeRotation = aim + MathHelper.Lerp(-1.9f, 1.8f, MathHelper.SmoothStep(0, 1, progress)) * p.direction;
            arm = bladeRotation - MathHelper.PiOver2;
            Projectile.ai[2]--;
            if (Projectile.ai[2] <= 0)
            {
                Projectile.ai[1] = 0; Projectile.netUpdate = true;
                if (Projectile.owner == Main.myPlayer) p.GetModPlayer<DefenderCombatPlayer>().AttackFinished();
            }
        }
        else if (DefenderRules.IsFortress(p.HeldItem) && MainWeaponInHands)
        { bladeRotation = aim; arm = aim - MathHelper.PiOver2; }
        // An idle back-mounted weapon must not override the player's free arm.
        if (MainWeaponInHands)
            p.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, arm);
        hand = p.GetBackHandPosition(Player.CompositeArmStretchAmount.Full, arm);
        if ((p.HeldItem.ModItem?.Name == "NoirShield" || shieldBash) && Attacking)
        {
            float extension = MathF.Sin((1f - Projectile.ai[2] / duration) * MathHelper.Pi);
            Projectile.Center = p.MountedCenter + aim.ToRotationVector2() * (16f + extension * 32f);
            Projectile.rotation = aim - MathHelper.PiOver2 + MathHelper.PiOver2 * p.direction;
            p.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aim - MathHelper.PiOver2);
            hand = p.MountedCenter;
            bladeRotation = aim;
        }
        if (Attacking && DefenderRules.MainArt(p.HeldItem) != null
            && !DefenderRules.IsFortress(p.HeldItem) && !DefenderRules.IsSentinel(p.HeldItem))
        {
            for (int i = trailHands.Length - 1; i > 0; i--)
            { trailHands[i] = trailHands[i - 1]; trailTips[i] = trailTips[i - 1]; }
            trailHands[0] = hand - bladeRotation.ToRotationVector2() * 12f;
            trailTips[0] = hand + bladeRotation.ToRotationVector2() * (shieldBash ? 64f : DefenderRules.WeaponLength(p.HeldItem) - 12f);
            trailCount = Math.Min(trailCount + 1, trailHands.Length);
            DefenderSkillVisuals.Swing(p, trailTips[0], (int)Projectile.ai[2]);
        }
        else trailCount = 0;
        Projectile.GetGlobalProjectile<ArtsProjectileMarker>().IsArtsDamage = arts;
        Projectile.DamageType = arts ? DamageClass.Magic : DamageClass.Melee;
        shield.mp.ParryThemeColor = DefenderRules.Color(p.HeldItem);
        if (DefenderRules.IsFortress(p.HeldItem))
        {
            byte state = p.HeldItem.ModItem is HornGrenadeLauncher
                ? (Projectile.owner == Main.myPlayer ? p.GetModPlayer<HornLauncherPlayer>().VisualState : hornVisual)
                : DefenderRules.SkillActive(p) ? (p.HeldItem.ModItem?.Name == "FirewhistleLauncher" ? (byte)6 : (byte)2) : (byte)0;
            hornVisual = state;
            Horn.HornVisuals.SkillBody(p, state, ++Projectile.localAI[0]);
        }
    }
    public override bool? CanDamage() => Attacking && !Guarding && !DefenderRules.IsSentinel(Owner.HeldItem)
        && !DefenderRules.IsFortress(Owner.HeldItem) && Projectile.ai[2] < duration * .85f && Projectile.ai[2] > duration * .1f;
    public override bool? CanHitNPC(NPC target) => target.friendly || hitGroups.Contains(target.realLife >= 0 ? target.realLife : target.whoAmI) ? false : null;
    public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
    {
        float collision = 0;
        float reach = (Owner.HeldItem.ModItem?.Name == "NoirShield" || shieldBash) ? 64f : DefenderRules.WeaponLength(Owner.HeldItem) - 12f;
        return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), hand,
            hand + bladeRotation.ToRotationVector2() * reach, 22f, ref collision);
    }
    public override void CutTiles()
    {
        if (CanDamage() != true || DefenderRules.IsFortress(Owner.HeldItem) || DefenderRules.IsSentinel(Owner.HeldItem)) return;
        DelegateMethods.tilecut_0 = Terraria.Enums.TileCuttingContext.AttackProjectile;
        Utils.PlotTileLine(hand, hand + bladeRotation.ToRotationVector2() * (DefenderRules.WeaponLength(Owner.HeldItem) - 12f),
            22f, DelegateMethods.CutTiles);
    }
    public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Owner.HeldItem.ModItem?.Name == "AuroraAxe" && DefenderRules.Skill(Owner, 1)
            && target.GetGlobalNPC<DefenderColdNPC>().ChillHits >= 2) modifiers.SourceDamage *= 2f;
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        hitGroups.Add(target.realLife >= 0 ? target.realLife : target.whoAmI);
        DefenderRouteCombatNet.Hit(Projectile, target, damageDone, attackSequence);
        if (Projectile.owner != Main.myPlayer) return;
        string name = Owner.HeldItem.ModItem.Name;
        DefenderSkillVisuals.Burst(target.Center, DefenderRules.Color(Owner.HeldItem), DefenderRules.SkillActive(Owner) ? 5 : 2);

        if (name == "CroissantHammer" && DefenderRules.Skill(Owner, 1))
            DefenderSkillVisuals.Field(target.Center, 90f, DefenderAreaKind.Electric, DefenderRules.Color(Owner.HeldItem), 5);
        if (name == "Vulcan_Weapon" && DefenderRules.Skill(Owner, 1)) Owner.GetModPlayer<DefenderCombatPlayer>().HealSelf(.04f, 30);
        if (name == "LutonHammer") Owner.GetModPlayer<DefenderCombatPlayer>().HealSelf(DefenderRules.Skill(Owner, 1) ? .04f : .015f, 60);
        if (name == "MudrockHammer") Owner.GetModPlayer<DefenderCombatPlayer>().HealSelf(DefenderRules.Skill(Owner, 1) ? .06f : .02f, 60);
        if (!impact && ((name == "CroissantHammer" && DefenderRules.Skill(Owner, 1))
            || (name == "MudrockHammer" && (DefenderRules.Skill(Owner, 1) || DefenderRules.Skill(Owner, 2)))
            || (name is "AsbestosBlade" or "HoshigumaShield" or "ShalemBlade" or "HoshigumaBlazingBlade" or "YuBlade" && DefenderRules.Skill(Owner, 1))))
        {
            impact = true;
            DefenderPulse.Spawn(Owner, target.Center, Projectile.damage, name == "MudrockHammer" ? 140 : 105, arts, target.whoAmI);
        }
    }
    internal void ApplyRouteEffects(NPC target, int damageDone, int sequence)
    {
        if (sequence != attackSequence || !routeEffectHits.Add(target.realLife >= 0 ? target.realLife : target.whoAmI)) return;
        DefenderRouteEffects.Contact(Owner, target, Math.Clamp(damageDone, 0, Math.Max(1, Projectile.damage * 4)));
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
            _ => DefenderRules.Color(Owner.HeldItem)
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
        if (DefenderRules.IsFortress(p.HeldItem)) Horn.HornVisuals.SkillBodyGlow(p, hornVisual, Projectile.localAI[0]);
        if (MainWeaponInHands && !(shieldBash && Attacking) && DefenderRules.MainArt(p.HeldItem) is string path)
        {
            Texture2D texture = ModContent.Request<Texture2D>(path).Value;
            bool gun = DefenderRules.IsFortress(p.HeldItem) || DefenderRules.IsSentinel(p.HeldItem);
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
            Texture2D texture = ModContent.Request<Texture2D>(shieldBash && Attacking
                ? "ArknightsMod/Content/Projectiles/Defender/Durnar/DN_Shield_Attack" : shieldPath).Value;
            shield.DrawShield(Projectile, p, texture, Guarding);
            shield.mp.DrawEffect(texture);
        }
        sb.End();
        sb.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp,
            DepthStencilState.None, RasterizerState.CullNone, null, Main.GameViewMatrix.TransformationMatrix);
        return false;
    }
}
