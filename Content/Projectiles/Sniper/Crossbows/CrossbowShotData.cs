using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Projectiles.Sniper.Crossbows;

// Skill effects also apply to unconverted ammunition without changing its AI or ai[] slots.
public sealed class CrossbowShotData : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    public int Kind = -1;
    public int Skill;
    private Vector2[] visualHistory;
    public override void PostAI(Projectile projectile)
    {
        if (Kind < 0 || Main.dedServ || projectile.ModProjectile is CrossbowBolt) return;
        visualHistory ??= new Vector2[28];
        for (int i = visualHistory.Length - 1; i > 0; i--) visualHistory[i] = visualHistory[i - 1];
        visualHistory[0] = projectile.Center;
        CrossbowFlightVfx.Wake(projectile, (CrossbowKind)Kind);
    }
    public override bool PreDraw(Projectile projectile, ref Color lightColor)
    {
        if (Kind >= 0 && visualHistory != null && projectile.ModProjectile is not CrossbowBolt)
            CrossbowFlightVfx.Draw(projectile, (CrossbowKind)Kind, visualHistory, false);
        return true;
    }
    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter writer)
    {
        bitWriter.WriteBit(Kind >= 0);
        if (Kind >= 0) { writer.Write((byte)Kind); writer.Write((byte)Skill); }
    }
    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader reader)
    {
        if (bitReader.ReadBit()) { Kind = reader.ReadByte(); Skill = reader.ReadByte(); }
    }
    public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
    {
        if (Kind != (int)CrossbowKind.Pozemka) return;
        if (Skill == 1 && Main.rand.NextFloat() < .4f) modifiers.FinalDamage *= 2.25f;
        if (Skill == 3)
            modifiers.FinalDamage *= Vector2.Distance(Main.player[projectile.owner].Center, target.Center) < 400f ? 2.55f : 2f;
    }
    public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
    {
        if (Kind == (int)CrossbowKind.Pozemka && projectile.owner == Main.myPlayer)
        {
            global::ArknightsMod.Content.Projectiles.Sniper.Pozemka.PozemkaStarBolt.SpawnPair(
                projectile.GetSource_OnHit(target), projectile, target);
        }
        if (Kind == (int)CrossbowKind.KroosAlter && Main.rand.NextFloat() < .4f)
            target.AddBuff(BuffID.Confused, 6);
        if (Kind == (int)CrossbowKind.Schwarz)
        {
            float chance = Skill switch { 1 => .8f, 2 => .5f, 3 => 1f, _ => .2f };
            if (Main.rand.NextFloat() < chance) target.AddBuff(BuffID.BrokenArmor, 300);
        }
    }
}
