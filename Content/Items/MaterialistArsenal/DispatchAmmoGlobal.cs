using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace ArknightsMod.Content.Items.MaterialistArsenal;

// Instance-only conversion. Never change global vanilla projectile sets or the behavior of other players' bullets.
public sealed class DispatchAmmoGlobal : GlobalProjectile
{
    public override bool InstancePerEntity => true;
    private bool dispatchShot;
    internal void Bind(Projectile projectile)
    {
        dispatchShot = true;
        projectile.DamageType = DamageClass.Summon;
        projectile.hostile = false; projectile.friendly = true;
    }
    public override void OnSpawn(Projectile projectile, IEntitySource source)
    {
        if (source is EntitySource_Parent { Entity: Projectile parent } &&
            (parent.ModProjectile is DispatchDrone || parent.GetGlobalProjectile<DispatchAmmoGlobal>().dispatchShot)) Bind(projectile);
    }
    public override bool PreAI(Projectile projectile)
    {
        if (dispatchShot) Bind(projectile);
        return true;
    }
    public override void PostAI(Projectile projectile) { if (dispatchShot) Bind(projectile); }
    public override void SendExtraAI(Projectile projectile, BitWriter bits, BinaryWriter writer) => bits.WriteBit(dispatchShot);
    public override void ReceiveExtraAI(Projectile projectile, BitReader bits, BinaryReader reader)
    {
        dispatchShot = bits.ReadBit();
        if (dispatchShot) Bind(projectile);
    }
}
