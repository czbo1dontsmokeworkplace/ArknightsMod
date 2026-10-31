using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Guard.BladeRoutes;

internal static class BladeRouteArt
{
    // Optional reader lets the offline GPU review call these exact draw routines.
    internal static Func<string, Texture2D> TextureReader;
    private static Texture2D Read(string path) => TextureReader?.Invoke(path) ?? ModContent.Request<Texture2D>(path,ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
    private static Texture2D Vanilla(int type,string key)
    {
        if(TextureReader!=null)return TextureReader(key);
        Main.instance.LoadProjectile(type);
        return TextureAssets.Projectile[type].Value;
    }
    private static Texture2D Mask(string name) => Read("ArknightsMod/Content/Textures/OperatorVfx/" + name);
    private static Color Glow(Color color, float strength) => new Color(color.R, color.G, color.B, 0) * strength;
    internal static void Icon(SpriteBatch batch, int kind, Vector2 position, Color color, float size, float rotation = 0)
    {
        Texture2D texture = Read(BladeRouteRules.WeaponTexture(kind));
        batch.Draw(texture, position, null, color, rotation, texture.Size() * .5f, size / Math.Max(texture.Width, texture.Height), SpriteEffects.None, 0);
    }
    internal static void Blade(Vector2 center, float angle, float length, int kind, Color color)
    {
        Texture2D texture = Read(BladeRouteRules.WeaponTexture(kind));
        Vector2 grip = new(6, texture.Height - 6);
        Vector2 axis = new(texture.Width - 9, 9 - texture.Height);
        Main.spriteBatch.Draw(texture, center - Main.screenPosition, null, color, angle - axis.ToRotation(), grip, length / axis.Length(), SpriteEffects.None, 0);
    }
    internal static void Aura(Vector2 center, float angle, float scale, float progress, int kind, float direction,Color lightColor)
    {
        Texture2D texture = Vanilla(ProjectileID.TrueExcalibur,"VanillaAura");
        Rectangle frame = texture.Frame(1, 4);
        // Same 0 -> .6 -> 1 lifetime envelope and .3/.5/.6 material gains
        // as vanilla's Excalibur draw, with only the operator palette changed.
        float fade = Math.Clamp(progress/.6f,0,1)*Math.Clamp((1-progress)/.4f,0,1);
        float lighting=Math.Clamp((lightColor.ToVector3().Length()/MathF.Sqrt(3)-.2f)/.8f,0,1);
        Color theme = BladeRouteRules.Theme(kind);
        Vector2 at = center - Main.screenPosition;
        SpriteEffects flip = direction < 0 ? SpriteEffects.FlipVertically : SpriteEffects.None;
        Color back=Color.Lerp(theme,Color.Black,.4f),front=Color.Lerp(theme,Color.White,.55f);
        Main.spriteBatch.Draw(texture,at,frame,back*(lighting*fade),angle-direction*MathHelper.PiOver4*(1-progress),frame.Size()*.5f,scale*1.1f,flip,0);
        Main.spriteBatch.Draw(texture,at,frame,theme*(lighting*fade*.3f),angle,frame.Size()*.5f,scale*1.1f,flip,0);
        Main.spriteBatch.Draw(texture,at,frame,front*(lighting*fade*.5f),angle,frame.Size()*.5f,scale*1.1f*.975f,flip,0);
        Main.spriteBatch.Draw(texture,at,texture.Frame(1,4,0,3),Color.White*(fade*.6f),angle+direction*.01f,frame.Size()*.5f,scale*1.1f,flip,0);
        Main.spriteBatch.Draw(texture,at,texture.Frame(1,4,0,3),Color.White*(fade*.5f),angle-direction*.05f,frame.Size()*.5f,scale*1.1f*.8f,flip,0);
        Main.spriteBatch.Draw(texture,at,texture.Frame(1,4,0,3),Color.White*(fade*.4f),angle-direction*.1f,frame.Size()*.5f,scale*1.1f*.6f,flip,0);
    }
    private static void Sprite(string name, Vector2 at, Vector2 size, Color color, float angle = 0)
    {
        Texture2D texture = Mask(name);
        Main.spriteBatch.Draw(texture, at - Main.screenPosition, null, color, angle, texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0);
    }
    internal static void Slash(Vector2 center, float angle, float progress, int kind)
    {
        Color theme = BladeRouteRules.Theme(kind);
        float length = 175 * MathF.Sqrt(Math.Min(1, progress * 5));
        float fade = MathF.Pow(1 - progress, 1.6f);
        Sprite("OrbitSmear", center, new Vector2(length * 1.1f, 27 * fade), Glow(theme, .65f * fade), angle);
        Sprite("LinearSpark", center, new Vector2(length, 13 * fade + 1), Glow(theme, fade), angle);
        Sprite("LinearSpark", center, new Vector2(length * .85f, 4 * fade + 1), Glow(Color.Lerp(theme, Color.White, .7f), fade), angle);
        Sprite("StarCore", center, new Vector2(25, 17) * fade, Glow(theme, fade), angle);
    }
    private static void Trail(Projectile q, float width, Color color)
    {
        Texture2D texture = Mask("TrailFilament");
        Vector2 previous = q.Center;
        for (int i = 0; i < q.oldPos.Length; i++)
        {
            if (q.oldPos[i] == Vector2.Zero) break;
            Vector2 point = q.oldPos[i] + q.Size * .5f;
            Vector2 delta = previous - point;
            float taper = 1 - i / (float)q.oldPos.Length;
            if (delta.LengthSquared() > .01f && delta.LengthSquared() < 120 * 120)
            {
                Vector2 size = new(delta.Length() + 4, width * taper);
                Main.spriteBatch.Draw(texture, (previous + point) * .5f - Main.screenPosition, null, Glow(color, taper * .65f), delta.ToRotation(), texture.Size() * .5f, size / texture.Size(), SpriteEffects.None, 0);
            }
            previous = point;
        }
    }
    internal static void Wave(Projectile q)
    {
        Color theme = BladeRouteRules.Theme((int)q.ai[0]);
        float fade = Math.Min(1, q.timeLeft / 12f);
        Trail(q, 28 * q.scale, theme * fade);
        // Terra Blade wave silhouette with a tinted envelope and narrow bright edge.
        Texture2D texture = TextureReader?.Invoke("VanillaWave") ?? TextureAssets.Projectile[ProjectileID.TerraBlade2Shot].Value;
        // These are four material layers, not animation frames (vanilla also
        // uses Frame(1, 4) directly). Drawing the whole texture makes four waves.
        Rectangle frame = texture.Frame(1, 4);
        Vector2 scale = new(74 / (float)frame.Width, 102 / (float)frame.Height);
        Main.spriteBatch.Draw(texture, q.Center - Main.screenPosition, frame, Glow(theme, .85f * fade), q.rotation, frame.Size() * .5f, scale * q.scale, SpriteEffects.None, 0);
        Main.spriteBatch.Draw(texture, q.Center - Main.screenPosition, texture.Frame(1, 4, 0, 3), Glow(Color.Lerp(theme, Color.White, .5f), .65f * fade), q.rotation, frame.Size() * .5f, scale * q.scale, SpriteEffects.None, 0);
        Sprite("OrbitSmear", q.Center, new Vector2(35, 95) * q.scale, Glow(theme, .65f * fade), q.rotation);
    }
    internal static void Bolt(Projectile q)
    {
        Color theme = BladeRouteRules.Theme((int)q.ai[0]);
        float fade = Math.Min(1, q.timeLeft / 12f);
        Trail(q, 16, theme * fade);
        Sprite("ForwardFlame", q.Center - q.velocity * .5f, new Vector2(27, 15), Glow(theme, .7f * fade), q.rotation);
        Sprite("StarCore", q.Center, new Vector2(16, 11), Glow(theme, fade), q.rotation);
        Sprite("LinearSpark", q.Center, new Vector2(17, 3), Glow(Color.Lerp(theme, Color.White, .7f), fade), q.rotation);
    }
    internal static void Mote(Vector2 at, int kind)
    {
        if (Main.dedServ) return;
        Dust dust = Dust.NewDustPerfect(at, DustID.FireworksRGB, Main.rand.NextVector2Circular(1.7f, 1.7f), 110, BladeRouteRules.Theme(kind), .65f);
        dust.noGravity = true;
    }
    internal static void Impact(Vector2 at, int kind)
    {
        if (Main.dedServ) return;
        for (int i = 0; i < 8; i++)
        {
            Dust dust = Dust.NewDustPerfect(at, DustID.FireworksRGB, Main.rand.NextVector2CircularEdge(5, 5) * Main.rand.NextFloat(.4f, 1.2f), 80, BladeRouteRules.Theme(kind), .9f);
            dust.noGravity = true;
        }
    }
}
