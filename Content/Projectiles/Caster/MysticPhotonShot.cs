using System.IO;
using ArknightsMod.Content.Items.Weapons.Caster;
using ArknightsMod.Content.Projectiles.Caster.Ebenholz;
using ArknightsMod.Content.Projectiles.Caster.Harmonie;
using ArknightsMod.Content.Projectiles.Caster.Indigo;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Projectiles.Caster;

public sealed class MysticPhotonShot : GlobalProjectile
{
    internal bool NormalFinisher;
    private bool detonated;
    public override bool InstancePerEntity => true;
    public override bool AppliesToEntity(Projectile entity, bool lateInstantiation)
        => entity.ModProjectile is IndigoDroplet or HarmonieNote or EbenholzCube;
    internal static bool Eligible(Projectile shot)
        => shot.ai[2] <= 0 && (shot.ai[0] == MysticChargeRules.StoredMode(2)
            || shot.ai[0] == 0 && shot.GetGlobalProjectile<MysticPhotonShot>().NormalFinisher);
    public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        => bitWriter.WriteBit(NormalFinisher);
    public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        => NormalFinisher = bitReader.ReadBit();
    internal static void Detonate(Projectile shot, int excludedTarget)
    {
        if (!Eligible(shot) || shot.owner != Main.myPlayer) return;
        var state = shot.GetGlobalProjectile<MysticPhotonShot>();
        if (state.detonated) return;
        state.detonated = true;
        int kind = shot.ModProjectile is IndigoDroplet ? 0 : shot.ModProjectile is HarmonieNote ? 1 : 2;
        Projectile blast = Projectile.NewProjectileDirect(shot.GetSource_Death(), shot.Center,
            shot.velocity.SafeNormalize(Vector2.UnitX), ModContent.ProjectileType<MysticPhotonBlast>(),
            shot.damage, shot.knockBack, shot.owner, kind, excludedTarget + 1);
        blast.CritChance = shot.CritChance;
    }
}
