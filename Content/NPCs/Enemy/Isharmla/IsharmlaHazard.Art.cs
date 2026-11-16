using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;

namespace ArknightsMod.Content.NPCs.Enemy.Isharmla;

public sealed partial class IsharmlaHazard
{
    // Damaging bodies use shipped Terraria sprites, the three generated pixel assets, or complete shader materials.
    // No line, chevron, polygon or hand-built pixel projectile fallback.
    public string GeneratedTextureName => Kind switch { TideShot.Harpoon=>"TideHarpoon",TideShot.TideSeed=>"TideSeed",TideShot.TideRay=>"TideRay",_=>null };
    public int CombatTexture => Kind switch
    {
        TideShot.Harpoon or TideShot.TideSeed or TideShot.TideRay => -1,
        TideShot.Canticle or TideShot.Note or TideShot.CharmNote or TideShot.EchoSinger => ProjectileID.QuarterNote,
        TideShot.FoamArc or TideShot.Pearl or TideShot.Bubble => ProjectileID.Bubble,
        TideShot.Current or TideShot.Typhoon => ProjectileID.Typhoon,
        TideShot.Seeker or TideShot.Shark => ProjectileID.MiniSharkron,
        TideShot.StormSpark or TideShot.Bomb or TideShot.TideHeart => ProjectileID.Electrosphere,
        TideShot.Rain or TideShot.WakeMark => ProjectileID.WaterBolt,
        TideShot.Mine => ProjectileID.Bubble,
        TideShot.MistField or TideShot.RainCloud => ProjectileID.ToxicCloud,
        TideShot.Tornado => ProjectileID.Sharknado,
        _ => ProjectileID.WaterBolt
    };
    public void DrawCombatArt()
    {
        float warning = IsharmlaRules.Smooth(Age / (float)Math.Max(1, Delay));
        float fade = Age < Delay ? .3f + warning * .45f : Math.Min(1, Math.Max(0, (Delay + Duration - Age) / 18f));
        Color tint = Style % 2 == 0 ? new Color(115, 235, 255) : new Color(175, 155, 255);
        if (Ray)
        {
            Vector2 axis = Direction.SafeNormalize(Vector2.UnitY);
            float envelope = Age < Delay ? .12f + warning * .16f : fade;
            float width = Age < Delay ? Math.Max(8, Width * .25f) : Kind==TideShot.Lattice?Width*1.75f:Width;
            IsharmlaVisuals.DrawField(Projectile.Center + axis * Length * .5f, new Vector2(Length, width),
                axis.ToRotation(), tint * envelope, Age>=Delay?(Kind==TideShot.Breath?"Disintegration":Kind==TideShot.Lattice?"Lattice":"Water"):"Water");
            IsharmlaVisuals.DrawField(Projectile.Center, new Vector2(Width * 2.5f), Age * .02f, tint * fade, "Halo");
            return;
        }
        if (Area || Kind == TideShot.TideHeart)
        {
            float diameter = Area ? Width * 2 : Age < Delay ? 50 + warning * 90 : 150;
            IsharmlaVisuals.DrawField(Projectile.Center, new Vector2(diameter), Age * .01f, tint * fade, "Lens");
            IsharmlaVisuals.DrawField(Projectile.Center, new Vector2(diameter * 1.15f), -Age * .015f, tint * fade, "Halo");
        }
        if(Kind is TideShot.Harpoon or TideShot.TideSeed or TideShot.TideRay) {
            string name=GeneratedTextureName;
            Texture2D sprite=IsharmlaVisuals.Art(name);
            float spriteRotation=Kind==TideShot.TideSeed?Age*.027f:Direction.ToRotation();
            float scale=Kind==TideShot.Harpoon?.80f:Kind==TideShot.TideSeed?.72f:.84f;
            if(Kind==TideShot.TideSeed && Age-Delay is >=40 and <76)scale*=1+MathF.Sin(Age*.3f)*.07f;
            for(int i=3;i>0;i--)Main.spriteBatch.Draw(sprite,Projectile.Center-Direction.SafeNormalize(Vector2.UnitX)*i*7-Main.screenPosition,null,tint*(fade*.09f),spriteRotation,sprite.Size()/2,scale*(1-i*.08f),SpriteEffects.None,0);
            Main.spriteBatch.Draw(sprite,Projectile.Center-Main.screenPosition,null,Color.White*fade,spriteRotation,sprite.Size()/2,scale,SpriteEffects.None,0);
            return;
        }
        int art = CombatTexture;
        if (!TextureAssets.Projectile[art].IsLoaded) Main.instance.LoadProjectile(art);
        Texture2D texture = TextureAssets.Projectile[art].Value;
        if (Kind == TideShot.Tornado) { DrawTornado(texture); return; }
        int frames = NativeFrames(art), height = texture.Height / frames;
        Rectangle frame = new(0, Age / 5 % frames * height, texture.Width, height);
        Vector2 size = Kind switch
        {
            TideShot.Current => new(42, 68), TideShot.Seeker or TideShot.Shark => new(48, 28),
            TideShot.Canticle or TideShot.Note or TideShot.CharmNote => new(24, 30),
            TideShot.WakeMark => new(32), TideShot.EchoSinger => new(60, 76),
            TideShot.TideHeart => new(70), TideShot.RainCloud => new(Width, 80),
            TideShot.Wave => new(76, 150), _ => new(30)
        };
        Vector2 direction = Direction.SafeNormalize(Vector2.UnitY);
        float rotation = Kind == TideShot.Current || Kind == TideShot.Typhoon ? Age * .13f : Direction.ToRotation();
        var batch = Main.spriteBatch;
        for (int i = 3; i > 0; i--)
            batch.Draw(texture, Projectile.Center - direction * i * 9 - Main.screenPosition, frame, tint * (fade * .1f),
                rotation, frame.Size() / 2, size / frame.Size() * (1 - i * .13f), SpriteEffects.None, 0);
        // Four offset copies outline the source silhouette, rather than manufacturing a shape from lines.
        foreach (Vector2 offset in new[] { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY })
            batch.Draw(texture, Projectile.Center + offset * 1.5f - Main.screenPosition, frame, tint * (.5f * fade),
                rotation, frame.Size() / 2, size / frame.Size(), SpriteEffects.None, 0);
        batch.Draw(texture, Projectile.Center - Main.screenPosition, frame, Color.White * fade,
            rotation, frame.Size() / 2, size / frame.Size(), SpriteEffects.None, 0);
    }
}
