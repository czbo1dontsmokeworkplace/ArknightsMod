using System;
using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster.Gitano;
using ArknightsMod.Common.VisualEffects;
using ArknightsMod.Content.Projectiles.Caster.SpreadArts;
using ArknightsMod.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Gitano;

/// <summary>右手握球，按住聚晶，松手释放。准星由持有者同步。</summary>
public sealed class GitanoFocus : ModProjectile
{
    private const int BaseChargeTicks = 52;
    private static readonly float[] ChantSpeed = [.15f, .20f, .25f, .30f, .35f, .40f, .45f, .55f, .65f, .75f];
    private float aimAngle;
    private SpreadChargeVfx chargeVfx;
    private bool fired;
    private bool sawChannel;
    private int openingGrace;
    private Player Owner => Main.player[Projectile.owner];
    private Vector2 Direction => aimAngle.ToRotationVector2();
    private Vector2 OrbPosition => Owner.MountedCenter + Direction * 16f;
    private Vector2 Muzzle => OrbPosition + Direction * 19f;
    private int RequiredCharge
    {
        get
        {
            WeaponPlayer skills = Owner.GetModPlayer<WeaponPlayer>();
            if (!skills.SkillActive || skills.Skill != 0 || skills.CurrentSkill?.Key.Item != nameof(GitanoOrb))
                return BaseChargeTicks;
            int rank = Math.Clamp((skills.CurrentSkill.ForceReplaceLevel ?? skills.CurrentSkill.Level) - 1, 0, 9);
            return Math.Max(20, (int)MathF.Round(BaseChargeTicks / (1f + ChantSpeed[rank])));
        }
    }
    private float Charge => MathHelper.Clamp(Projectile.ai[0] / RequiredCharge, 0f, 1f);
    public override string Texture => "Terraria/Images/Item_" + ItemID.CrystalBall;

    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 16;
        Projectile.friendly = false;
        Projectile.tileCollide = false;
        Projectile.ignoreWater = true;
        Projectile.timeLeft = 2;
        Projectile.netImportant = true;
    }

    public override void OnSpawn(IEntitySource source) => aimAngle = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
    public override bool ShouldUpdatePosition() => false;

    public override void AI()
    {
        Player owner = Owner;
        if (!owner.active || owner.dead || owner.noItems || owner.CCed
            || owner.HeldItem.ModItem is not GitanoOrb
            || owner.GetModPlayer<GitanoOrbPlayer>().Exhausted)
        {
            Projectile.Kill();
            return;
        }
        Projectile.Center = owner.MountedCenter;
        if (Projectile.owner == Main.myPlayer)
        {
            aimAngle = (Main.MouseWorld - owner.MountedCenter)
                .SafeNormalize(new Vector2(owner.direction, 0f)).ToRotation();
            if (owner.channel) sawChannel = true;
            else if (sawChannel || ++openingGrace > 5)
            {
                if (sawChannel) Fire();
                owner.itemTime = owner.itemAnimation = 0;
                Projectile.Kill();
                return;
            }
            if ((int)Projectile.ai[0] % 6 == 0) Projectile.netUpdate = true;
            if (owner.channel && !ChannelMana.Sustain(owner, Projectile))
            { owner.itemTime = owner.itemAnimation = 0; Projectile.Kill(); return; }
        }
        owner.heldProj = Projectile.whoAmI;
        owner.itemTime = owner.itemAnimation = 2;
        owner.ChangeDir(Direction.X < 0f ? -1 : 1);
        owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, aimAngle - MathHelper.PiOver2);
        Projectile.timeLeft = 2;
        Projectile.ai[0] = Math.Min(BaseChargeTicks, Projectile.ai[0] + 1f);
        if (Main.dedServ) return;
        (chargeVfx ??= new()).Tick(Projectile, Muzzle, Charge, SpreadArt.Crystal, Direction);
        Lighting.AddLight(Muzzle, new Vector3(.32f, .11f, .46f) * Charge);
        if (Charge >= 1f && Projectile.localAI[0] == 0f)
        {
            Projectile.localAI[0] = 1f;
            ChargeCastFeedback.Ready(Muzzle, new Color(178, 116, 234), DustID.GemAmethyst, .38f);
        }
    }

    private void Fire()
    {
        if (fired || Projectile.owner != Main.myPlayer) return;
        fired = true;
        if (Projectile.ai[0] < RequiredCharge)
        {
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = .23f, Pitch = .18f }, Muzzle);
            return;
        }
        WeaponPlayer skills = Owner.GetModPlayer<WeaponPlayer>();
        bool fate = skills.SkillActive && skills.Skill == 1
            && skills.CurrentSkill?.Key.Item == nameof(GitanoOrb);
        int damage = Math.Max(1, Owner.GetWeaponDamage(Owner.HeldItem));
        int type = fate ? ModContent.ProjectileType<GitanoBurst>() : ModContent.ProjectileType<GitanoCrystal>();
        Vector2 origin = fate ? Owner.MountedCenter : Muzzle;
        Vector2 velocity = fate ? Vector2.Zero : Direction * (15f * 1.2f);
        int index = Projectile.NewProjectile(Projectile.GetSource_FromThis(), origin, velocity,
            type, damage, Projectile.knockBack, Projectile.owner, fate ? 1f : 0f);
        if (Main.projectile.IndexInRange(index))
            Main.projectile[index].CritChance = Owner.GetWeaponCrit(Owner.HeldItem);
        SoundEngine.PlaySound(SoundID.Item73 with { Volume = .46f, Pitch = fate ? -.25f : .2f }, Muzzle);
        if (!Main.dedServ) GitanoVisuals.FireParticles(Muzzle, Direction, fate);
    }

    public override void SendExtraAI(BinaryWriter writer) => writer.Write(aimAngle);
    public override void ReceiveExtraAI(BinaryReader reader) => aimAngle = reader.ReadSingle();

    public override bool PreDraw(ref Color lightColor)
    {
        Texture2D orb = TextureAssets.Item[ItemID.CrystalBall].Value;
        float rotation = aimAngle * .16f;
        Main.spriteBatch.Draw(orb, OrbPosition - Main.screenPosition, null, lightColor,
            rotation, orb.Size() * .5f, .82f, SpriteEffects.None, 0f);
        if (Projectile.ai[1] == 0f)
            GitanoVisuals.DrawCharge(Muzzle, Charge, chargeVfx?.Phase ?? 0f, Direction);
        ChargeCastFeedback.DrawBar(Owner, Charge, new Color(178, 116, 234));
        return false;
    }
}
