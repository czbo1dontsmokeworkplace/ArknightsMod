using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.FaustAndMephisto;

public abstract class DuoProjectile : ModProjectile
{
    // ai: Faust slot + 1 / encounter token / target or shot variant.
    public override string LocalizationCategory => "FaustAndMephisto.Projectiles";
    public override string Texture => "Terraria/Images/Projectile_1";
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 10;
        Projectile.hostile = true; Projectile.friendly = false; Projectile.tileCollide = false;
        Projectile.ignoreWater = true; Projectile.penetrate = -1; Projectile.timeLeft = 180;
    }
    // The server resolves the first swept collision. A targeted packet uses normal Player.Hurt
    // on that player's owning client, retaining defence, dodge and immunity without a second hit.
    public override bool? CanDamage() => false;
    public override bool ShouldUpdatePosition() => false;
    internal int FirstPlayer(Vector2 from, Vector2 to, out float contact)
    {
        int result = -1; contact = 2;
        for (int i = 0; i < Main.maxPlayers; i++)
            if (DuoEncounter.Alive(Main.player[i]) && DuoRules.SegmentHit(from, to, Main.player[i].Hitbox, Projectile.width * .5f, out float t) && t < contact)
            { result = i; contact = t; }
        return result;
    }
    protected bool ValidateOwner(out Faust f)
    {
        f = DuoEncounter.Owner(Projectile.ai[0], Projectile.ai[1]);
        if (f != null) return true;
        if (DuoEncounter.Authority) Projectile.Kill();
        return false;
    }
}

public class FaustBolt : DuoProjectile
{
    private bool playedSound;
    protected virtual Color BoltColor => Projectile.ai[2] > 0 ? new Color(209, 121, 255) : new Color(230, 128, 135);
    public override void AI()
    {
        if (!ValidateOwner(out Faust f)) return;
        if (!Main.dedServ && !playedSound)
        {
            playedSound = true;
            SoundEngine.PlaySound(SoundID.Item5 with { Volume = .65f, Pitch = Projectile.ai[2] > 0 ? -.3f : .1f }, Projectile.Center);
        }
        int target = (int)Projectile.ai[2] - 1;
        if (target >= 0 && target < Main.maxPlayers && DuoEncounter.Alive(Main.player[target]))
        {
            float heading = Projectile.velocity.ToRotation();
            float wanted = (Main.player[target].Center - Projectile.Center).ToRotation();
            // Weak steering only while approaching; no U-turn after passing the player.
            if (Math.Abs(MathHelper.WrapAngle(wanted - heading)) < .7f)
                Projectile.velocity = heading.AngleTowards(wanted, .006f).ToRotationVector2() * Projectile.velocity.Length();
        }
        Vector2 next = Projectile.Center + Projectile.velocity;
        if (DuoEncounter.Authority)
        {
            int player = FirstPlayer(Projectile.Center, next, out float t);
            if (player >= 0)
            {
                Projectile.Center = Vector2.Lerp(Projectile.Center, next, t);
                DuoNet.Apply(player, f, DuoNet.Action.Hit, Projectile.damage, Math.Sign(Projectile.velocity.X));
                Projectile.Kill(); return;
            }
        }
        Projectile.Center = next; Projectile.rotation = Projectile.velocity.ToRotation();
        if (!Main.dedServ) Lighting.AddLight(Projectile.Center, BoltColor.ToVector3() * .25f);
    }
    public override void OnKill(int timeLeft) => DuoVisuals.Puff(Projectile.Center, BoltColor, 5);
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 p = Projectile.Center - Main.screenPosition, direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
        DuoVisuals.Line(Main.spriteBatch, p - direction * 70, p, BoltColor * .2f, 7);
        DuoVisuals.Line(Main.spriteBatch, p - direction * 44, p, BoltColor, 2.5f);
        DuoVisuals.Line(Main.spriteBatch, p - direction * 14, p, Color.White * .85f, 1);
        return false;
    }
}
public sealed class BallistaBolt : FaustBolt
{
    protected override Color BoltColor => new(177, 163, 192);
}
public sealed class ReunionArts : FaustBolt
{
    protected override Color BoltColor => new(187, 128, 151);
    public override void SetDefaults() { base.SetDefaults(); Projectile.timeLeft = 210; Projectile.width = Projectile.height = 18; }
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 p = Projectile.Center - Main.screenPosition;
        DuoVisuals.Ring(Main.spriteBatch, p, 7, BoltColor);
        DuoVisuals.Line(Main.spriteBatch, p - Projectile.velocity * 3, p, BoltColor * .5f, 5);
        return false;
    }
}

public sealed class MephistoMedicine : DuoProjectile
{
    private int intendedType;
    public override string Texture => "Terraria/Images/Projectile_14";
    public override void SetDefaults() { base.SetDefaults(); Projectile.width = Projectile.height = 16; Projectile.timeLeft = 300; }
    public override void OnSpawn(IEntitySource source)
    {
        int index = (int)Projectile.ai[2] - 1;
        if (index >= 0 && index < Main.maxNPCs) intendedType = Main.npc[index].type;
    }
    public override void AI()
    {
        if (!ValidateOwner(out Faust f)) return;
        int index = (int)Projectile.ai[2] - 1;
        NPC target = index >= 0 && index < Main.maxNPCs ? Main.npc[index] : null;
        if (target == null || !target.active || target.friendly || target.dontTakeDamage || target.life <= 0 || target.type != intendedType ||
            (target.ModNPC is ReunionSupport && (target.ai[0] != Projectile.ai[0] || target.ai[1] != Projectile.ai[1])))
        { if (DuoEncounter.Authority) Projectile.Kill(); return; }
        Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 7;
        Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, .075f);
        Vector2 next = Projectile.Center + Projectile.velocity;
        if (DuoEncounter.Authority)
        {
            int player = FirstPlayer(Projectile.Center, next, out float playerTime);
            bool hitsTarget = DuoRules.SegmentHit(Projectile.Center, next, target.Hitbox, 8, out float targetTime);
            if (player >= 0 && (!hitsTarget || playerTime <= targetTime))
            {
                DuoNet.Apply(player, f, DuoNet.Action.Dust);
                Projectile.Center = Vector2.Lerp(Projectile.Center, next, playerTime); Projectile.Kill(); return;
            }
            if (hitsTarget)
            {
                int heal = Math.Min(DuoRules.Attack, target.lifeMax - target.life);
                if (heal > 0) { target.life += heal; target.HealEffect(heal, true); target.netUpdate = true; }
                Projectile.Kill(); return;
            }
        }
        Projectile.Center = next;
        if (!Main.dedServ && Main.rand.NextBool(3))
            Dust.NewDustPerfect(Projectile.Center, DustID.Cloud, -Projectile.velocity * .1f, 130, Color.LightGray, .7f).noGravity = true;
    }
    public override void SendExtraAI(System.IO.BinaryWriter writer) => writer.Write(intendedType);
    public override void ReceiveExtraAI(System.IO.BinaryReader reader) => intendedType = reader.ReadInt32();
    public override void OnKill(int timeLeft) => DuoVisuals.Puff(Projectile.Center, Color.LightGray);
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 point = Projectile.Center - Main.screenPosition;
        DuoVisuals.Ring(Main.spriteBatch, point, 7, Color.LightGray * .8f, 16);
        DuoVisuals.Line(Main.spriteBatch, point - new Vector2(4, 0), point + new Vector2(4, 0), Color.White, 2);
        DuoVisuals.Line(Main.spriteBatch, point - new Vector2(0, 4), point + new Vector2(0, 4), Color.White, 2);
        return false;
    }
}

public sealed class FaustExecution : DuoProjectile
{
    public override void SetStaticDefaults() => ProjectileID.Sets.DrawScreenCheckFluff[Type] = 8000;
    public override void SetDefaults() { base.SetDefaults(); Projectile.timeLeft = 16; }
    public override void AI()
    {
        if (!ValidateOwner(out _)) return;
        if (!Main.dedServ && Projectile.localAI[0]++ == 0)
        {
            SoundEngine.PlaySound(SoundID.Item92 with { Volume = .6f, Pitch = -.3f }, Projectile.Center + Projectile.velocity);
            DuoVisuals.Puff(Projectile.Center + Projectile.velocity, Color.IndianRed, 12);
        }
    }
    public override bool PreDraw(ref Color lightColor)
    {
        Vector2 from = Projectile.Center - Main.screenPosition;
        Color red = Color.IndianRed * (Projectile.timeLeft / 16f);
        DuoVisuals.Line(Main.spriteBatch, from, from + Projectile.velocity, red * .3f, 14);
        DuoVisuals.Line(Main.spriteBatch, from, from + Projectile.velocity, red, 3);
        return false;
    }
}
