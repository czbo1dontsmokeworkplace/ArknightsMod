using System;
using ArknightsMod.Content.Items.Weapons.Defender;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Defender;

public sealed class EunectesDashPlayer : ModPlayer
{
    private int cooldown;
    private bool lastRight;
    internal EunectesDash Dash
    {
        get
        {
            foreach (Projectile p in Main.ActiveProjectiles)
                if (p.owner == Player.whoAmI && p.ModProjectile is EunectesDash dash && p.ai[0] == 0 && (int)p.ai[1] == Player.HeldItem.type) return dash;
            return null;
        }
    }
    internal bool Dashing => DefenderRules.IsDuelist(Player.HeldItem) && !Player.dead && Dash != null;
    public override void PostUpdate()
    {
        if (cooldown > 0) cooldown--;
        if (Player.whoAmI != Main.myPlayer) return;
        bool right = Main.mouseRight && !Player.mouseInterface;
        if (right && !lastRight && cooldown == 0 && DefenderRules.IsDuelist(Player.HeldItem)
            && !Player.dead && !Player.CCed && !Player.noItems && !DefenderHoldout.IsBusy(Player))
        {
            Vector2 aim = (Main.MouseWorld - Player.MountedCenter).SafeNormalize(new Vector2(Player.direction, 0));
            int index = Projectile.NewProjectile(Player.GetSource_ItemUse(Player.HeldItem), Player.Center, aim,
                ModContent.ProjectileType<EunectesDash>(), Player.GetWeaponDamage(Player.HeldItem) * 4,
                12f, Player.whoAmI, 0, Player.HeldItem.type);
            if (Main.projectile.IndexInRange(index)) Main.projectile[index].CritChance = Player.GetWeaponCrit(Player.HeldItem);
            Player.velocity = aim * 24f;
            cooldown = 90;
            SoundEngine.PlaySound(SoundID.Item1 with { Pitch = -.5f }, Player.Center);
        }
        lastRight = right;
    }
    public override void PreUpdateMovement()
    {
        if (!Dashing) return;
        Player.velocity = Dash.Projectile.velocity.SafeNormalize(new Vector2(Player.direction, 0)) * 24f;
        Player.fallStart = (int)(Player.position.Y / 16f);
    }
    // Applies only during this Duelist's collision dash, never to the shared guard.
    public override bool ImmuneTo(PlayerDeathReason damageSource, int cooldownCounter, bool dodgeable) => Dashing;
    public override void UpdateDead() { cooldown = 0; lastRight = false; }
}

public sealed class EunectesDash : ModProjectile
{
    private readonly System.Collections.Generic.HashSet<int> routeHits = new();
    private Player Owner => Main.player[Projectile.owner];
    public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Bullet;
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 70;
        Projectile.friendly = true;
        Projectile.DamageType = DamageClass.Melee;
        Projectile.penetrate = -1;
        Projectile.tileCollide = false;
        Projectile.ownerHitCheck = true;
        Projectile.timeLeft = 18;
        Projectile.usesLocalNPCImmunity = true;
        Projectile.localNPCHitCooldown = -1;
    }
    public override bool ShouldUpdatePosition() => false;
    public override bool? CanDamage() => Projectile.ai[0] == 0f;
    public override void AI()
    {
        if (!Owner.active || Owner.dead || Owner.CCed || Owner.noItems || !DefenderRules.IsDuelist(Owner.HeldItem) || Owner.HeldItem.type != (int)Projectile.ai[1])
        { Projectile.Kill(); return; }
        Vector2 direction = Projectile.velocity.SafeNormalize(new Vector2(Owner.direction, 0));
        Projectile.Center = Owner.MountedCenter + direction * 25f;
        if (Projectile.ai[0] != 0) return;
        Owner.ChangeDir(direction.X >= 0 ? 1 : -1);
        if (Projectile.owner == Main.myPlayer)
        {
            Vector2 expected = direction * 24f;
            Vector2 available = Collision.TileCollision(Owner.position, expected, Owner.width, Owner.height, Owner.controlDown, Owner.controlDown, (int)Owner.gravDir);
            if (Vector2.DistanceSquared(expected, available) > .01f) Bounce(direction);
        }
        if (!Main.dedServ)
        {
            Dust d = Dust.NewDustPerfect(Owner.Center + Main.rand.NextVector2Circular(15, 20), DustID.JungleSpore, -direction * 3f, 130, default, .8f);
            d.noGravity = true;
        }
    }
    public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
    {
        DefenderRouteCombatNet.Hit(Projectile, target, damageDone, 0);
        if (Projectile.owner == Main.myPlayer) Owner.GetModPlayer<DefenderCombatPlayer>().AttackFinished();
        Bounce(Projectile.velocity.SafeNormalize(Vector2.UnitX));
    }
    internal void ApplyRouteEffects(NPC target, int damageDone, int sequence)
    {
        if (sequence != 0 || !routeHits.Add(target.realLife >= 0 ? target.realLife : target.whoAmI)) return;
        DefenderRouteEffects.Contact(Owner, target, System.Math.Clamp(damageDone, 0, System.Math.Max(1, Projectile.damage * 4)));
    }
    private void Bounce(Vector2 normal)
    {
        if (Projectile.ai[0] != 0) return;
        Projectile.ai[0] = 1;
        Projectile.timeLeft = 6;
        Projectile.netUpdate = true;
        if (Projectile.owner == Main.myPlayer)
        {
            Owner.velocity = Vector2.Reflect(normal * 24f, normal) * .6f;
            if (Main.netMode == NetmodeID.MultiplayerClient)
                NetMessage.SendData(MessageID.PlayerControls, -1, -1, null, Owner.whoAmI);
        }
        Collision.HitTiles(Owner.position, -normal * 8f, Owner.width, Owner.height);
        SoundEngine.PlaySound(SoundID.Item14 with { Volume = .5f }, Owner.Center);
    }
    public override void OnKill(int timeLeft)
    {
        if (Projectile.ai[0] == 0 && Projectile.owner == Main.myPlayer) Owner.velocity *= .3f;
    }
    public override bool PreDraw(ref Microsoft.Xna.Framework.Color lightColor) => false;
}
