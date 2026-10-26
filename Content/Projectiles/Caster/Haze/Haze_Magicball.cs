using Microsoft.Xna.Framework;
using ArknightsMod.Content.Projectiles.Caster.CoreCasters;
using Terraria;
using System.IO;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Caster.Haze;

public class Haze_Magicball : ModProjectile, ICoreCasterGuidance
{
    public override string Texture => "ArknightsMod/Content/Projectiles/Caster/Haze/Haze_Magicball";
    private float effectDistance;
    private CoreCasterFlight flight;
    public bool GuidanceReady => flight?.CanGuide(CoreCasterKind.Haze) == true;
    public override void SendExtraAI(BinaryWriter writer) => CoreCasterGuidance.Write(writer, flight);
    public override void ReceiveExtraAI(BinaryReader reader) => flight = CoreCasterGuidance.Read(reader);
    public override void SetStaticDefaults()
    {
        ProjectileID.Sets.TrailingMode[Type] = 2;
        ProjectileID.Sets.TrailCacheLength[Type] = 80;
    }
    public override void SetDefaults()
    {
        Projectile.width = Projectile.height = 12;
        Projectile.DamageType = DamageClass.Magic;
        Projectile.aiStyle = -1; Projectile.timeLeft = 120;
        Projectile.friendly = true; Projectile.extraUpdates = 1;
    }
    public override void OnSpawn(Terraria.DataStructures.IEntitySource source) => CoreCasterFlight.Configure(Projectile, CoreCasterKind.Haze);
		public override void AI()
    {
        flight ??= new CoreCasterFlight(Projectile.Center, Projectile.velocity, Projectile.MaxUpdates, Projectile.identity);
        flight.Step(CoreCasterKind.Haze, Projectile.ai[2] == 2 ? 1 : 0, Projectile.Center, ref Projectile.velocity, Projectile.MaxUpdates,
            GuidanceReady ? CoreCasterGuidance.Target(Projectile) : null);
        Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
        effectDistance += Projectile.velocity.Length();
        if (effectDistance >= 30)
        {
            effectDistance %= 30;
            CoreCasterVisuals.Trail(Projectile, CoreCasterKind.Haze, (Projectile.ai[2] == 2 ? 1 : 0));
        }
    }
    public override void OnKill(int timeLeft) { CoreCasterVisuals.Retire(Projectile, CoreCasterKind.Haze, Projectile.ai[2] == 2 ? 1 : 0); CoreCasterVisuals.Impact(Projectile.Center, Projectile.velocity,
        CoreCasterKind.Haze, (Projectile.ai[2] == 2 ? 1 : 0) > 0); }
    public override bool PreDraw(ref Color lightColor)
    {
        CoreCasterVisuals.Bolt(Projectile, CoreCasterKind.Haze, (Projectile.ai[2] == 2 ? 1 : 0));
        return false;
    }
}
