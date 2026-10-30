using System;
using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Items.Weapons.Guard.Chen;

public partial class sw_Proj_2
{
    private int normalBlades, skill3Strikes;

    private void Skill_3_AI()
    {
        bool local = Projectile.owner == Main.myPlayer;
        player.heldProj = Projectile.whoAmI;
        Projectile.Center = player.MountedCenter + new Vector2(5).RotatedBy(Projectile.rotation - MathHelper.PiOver2);
        player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.rotation + MathHelper.Pi);
        player.itemAnimation = player.itemTime = 3;
        if (Projectile.localAI[0] == 0) {
            if (Projectile.ai[0] == 0) {
                AttackPos = player.MountedCenter;
                if (local) Projectile.NewProjectile(player.GetSource_FromAI(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<Dragon_Proj>(), 10, 1, player.whoAmI);
            }
            if (local) UpdateSkill3Target(player.MountedCenter);
            Projectile.ai[0]++;
            Projectile.timeLeft = 3;
            Swing_DrawScale = Vector2.One;
            player.ChangeDir(AttackPos.X < player.MountedCenter.X ? -1 : 1);
            Projectile.rotation = Projectile.rotation.AngleLerp(10 * player.direction,
                Math.Clamp(Projectile.ai[0] / ChenSwordRules.Skill3StartupTicks * .14f, 0, 1));
            if (!local || Projectile.ai[0] < ChenSwordRules.Skill3StartupTicks) return;
            Projectile.localAI[0] = 1;
            Projectile.ai[1] = 0;
            Projectile.timeLeft = ChenSwordRules.Skill3Duration;
            Projectile.netUpdate = true;
        }
        player.velocity = Vector2.Zero;
        player.immuneAlpha = 255;
        Swing_DrawScale = Vector2.Zero;
        var protection = player.GetModPlayer<Skill_2_Player>();
        protection.SetScreenPos(AttackPos);
        protection.CanBeDamaged = false;
        Projectile.ai[1]++;
        if (local && Projectile.ai[1] % ChenSwordRules.Skill3Interval == 0 && skill3Strikes < ChenSwordRules.Skill3Strikes) {
            UpdateSkill3Target(AttackPos);
            bool final = ++skill3Strikes == ChenSwordRules.Skill3Strikes;
            ReleaseSkill3Strike(final);
            Projectile.netUpdate = true;
        }
    }

    private void UpdateSkill3Target(Vector2 origin)
    {
        Skill_3_Target = ChenSwordRules.FindTarget(player, origin);
        if (Skill_3_Target != null) AttackPos = Skill_3_Target.Center;
    }

    private void ReleaseSkill3Strike(bool final)
    {
        float angle = final ? 0 : Main.rand.NextFloat(-.9f, .9f);
        int direction = final ? (player.MountedCenter.X < AttackPos.X ? 1 : -1) : (Main.rand.NextBool() ? 1 : -1);
        Vector2 start = AttackPos + new Vector2((final ? 240 : 220) * direction, 0).RotatedBy(angle);
        Vector2 velocity = (AttackPos - start).SafeNormalize(Vector2.UnitX) * (final ? 70 : 60);
        int damage = (int)Math.Round(player.GetTotalDamage(DamageClass.Melee).ApplyTo(player.HeldItem.damage) * 3.2f);
        Projectile.NewProjectile(Projectile.GetSource_FromThis(), start, velocity,
            ModContent.ProjectileType<Skill_3_Attack_Proj>(), damage, 1, player.whoAmI);
        if (!_playedSkill3Sound && !Main.dedServ) {
            SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/ChenSwordSkill3") { MaxInstances = 2 }, player.position);
            _playedSkill3Sound = true;
        }
        if (Main.dedServ) return;
        for (int i = 0; i < (final ? 60 : 30); i++) {
            Dust dust = Dust.NewDustPerfect(AttackPos, Main.rand.NextBool(3) ? 219 : 222);
            dust.noGravity = true;
            dust.velocity = velocity.RotatedByRandom(.4) * Main.rand.NextFloat(.1f, .3f);
            dust.fadeIn = .4f; dust.scale = final ? .6f : .5f;
        }
        Dust ring = Dust.NewDustPerfect(AttackPos, ModContent.DustType<Skill_3_Dust_4>());
        ring.fadeIn = final ? .8f : Main.rand.NextFloat(.15f, .3f);
        Dust blade = Dust.NewDustPerfect(AttackPos, ModContent.DustType<Skill_3_Dust_2>());
        blade.velocity = angle.ToRotationVector2();
        if (final) foreach (int side in new[] { -1, 1 }) {
            Dust arrow = Dust.NewDustPerfect(AttackPos, ModContent.DustType<Skill_3_Dust_3>());
            arrow.velocity = new Vector2(15 * side, 0); arrow.color = Color.Orange;
        }
    }

    public override void SendExtraAI(BinaryWriter writer)
    {
        writer.Write((byte)Sword_Style); writer.Write(AttackPos.X); writer.Write(AttackPos.Y);
        writer.Write(Skill_3_Target?.whoAmI ?? -1); writer.Write((int)Projectile.localAI[0]); writer.Write(skill3Strikes);
    }
    public override void ReceiveExtraAI(BinaryReader reader)
    {
        Sword_Style = (ChenSword_Style)Math.Clamp(reader.ReadByte(), (byte)0, (byte)3);
        AttackPos = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        int target = reader.ReadInt32(); Skill_3_Target = (uint)target < Main.maxNPCs ? Main.npc[target] : null;
        Projectile.localAI[0] = reader.ReadInt32(); skill3Strikes = Math.Clamp(reader.ReadInt32(), 0, ChenSwordRules.Skill3Strikes);
    }
    public override void OnKill(int timeLeft)
    {
        if (Sword_Style == ChenSword_Style.Skill_3) {
            player.GetModPlayer<Skill_2_Player>().CanBeDamaged = true;
            player.immuneAlpha = 0;
            ChenSwordRules.EndSkill(player, 2);
        }
    }
}
