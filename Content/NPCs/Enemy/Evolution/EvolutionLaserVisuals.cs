using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Evolution;

internal static class EvolutionLaserVisuals
{
    internal const int FadeTicks = 48;
    internal static float CollisionWidth(EvolutionBeamStyle style, bool corridor) =>
        EvolutionPianoRules.IsStyle(style) ? 18 : style == EvolutionBeamStyle.Pulse ? 12 : corridor ? 24 : 12;
    internal static bool WorldMaterialReady;
    internal static Effect ActiveMaterial;
    internal static void Draw(Vector2 start, Vector2 end, int age, float envelope, float collisionWidth, Color color, Color core)
    {
        if (envelope <= .0001f) return;
        Vector2 delta = end - start;
        float length = delta.Length();
        if (length < 1) return;
        float angle = delta.ToRotation();
        float width = collisionWidth * (3.6f + MathF.Sin(age * .21f) * .12f) * MathF.Sqrt(envelope);
        if (ActiveMaterial != null)
        {
            Effect effect = ActiveMaterial;
            effect.Parameters["uTime"].SetValue(age / 60f);
            effect.Parameters["uOpacity"].SetValue(envelope);
            effect.Parameters["uLength"].SetValue(length);
            effect.Parameters["uColor"].SetValue(color.ToVector3());
            effect.Parameters["uCore"].SetValue(core.ToVector3());
            effect.CurrentTechnique.Passes[0].Apply();
            Texture2D noise = EvolutionVisuals.Asset("EnergyNoise");
            Main.spriteBatch.Draw(noise, (start + end) * .5f - Main.screenPosition, null, Color.White,
                angle, noise.Size() * .5f, new Vector2(length, width) / noise.Size(), SpriteEffects.None, 0);
            return;
        }
        // 无材质时也保持连续光束，不再使用长条火焰插画反复拼接。
        EvolutionVisuals.Glow((start+end)*.5f, color * (envelope*.5f), new Vector2(length, width), angle);
        EvolutionVisuals.Glow((start+end)*.5f, core * (envelope*.8f), new Vector2(length, width*.25f), angle);
        for (int i=0;i<3;i++)
        {
            Vector2 normal = delta.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            float offset = MathF.Sin(age*.17f+i*2.1f)*collisionWidth*.15f;
            EvolutionVisuals.Line(start+normal*offset,end+normal*offset,core*(envelope*.25f),1.4f,false);
        }
    }
    internal static void DrawMuzzle(Vector2 origin, Vector2 direction, float envelope, float width, int age, Color color)
    {
        if (envelope <= .001f) return;
        EvolutionVisuals.Glow(origin, color * (.85f*envelope), new Vector2(width*2.5f,width*1.5f),direction.ToRotation());
        Texture2D ring = EvolutionVisuals.Asset("CinematicRing");
        Main.spriteBatch.Draw(ring,origin-Main.screenPosition,null,(color with {A=0})*(envelope*.6f),
            direction.ToRotation(),ring.Size()*.5f,new Vector2(width*.55f,width*1.6f)/ring.Size(),SpriteEffects.None,0);
    }
}

public sealed class EvolutionLaserRenderer : ModSystem
{
    private Effect material;
    private bool failed;
    public override void Unload()
    {
        Effect old=material;material=null;failed=false;EvolutionLaserVisuals.WorldMaterialReady=false;EvolutionLaserVisuals.ActiveMaterial=null;
        if(old!=null) Main.QueueMainThreadAction(old.Dispose);
    }
    public override void PostDrawTiles()
    {
        if(Main.dedServ||Main.gameMenu)return;
        bool any=false;
        foreach(Projectile projectile in Main.ActiveProjectiles)
            if(projectile.ModProjectile is EvolutionHazard h && h.UsesContinuousLaser && h.Parent is Evolution boss && boss.Serial==h.Serial)
            {any=true;break;}
        if(!any)return;
        GraphicsDevice device=Main.instance.GraphicsDevice;
        if(material==null&&!failed)
        {
            try{material=new Effect(device,Mod.GetFileBytes("Assets/Effects/EvolutionContinuousRay.fxc"));}
            catch(Exception error){failed=true;Mod.Logger.Warn("Evolution continuous ray material unavailable: "+error.Message);}
        }
        EvolutionLaserVisuals.WorldMaterialReady=material!=null;
        if(material==null)return;
        Texture oldNoise=device.Textures[1];SamplerState oldSampler=device.SamplerStates[1];
        Main.spriteBatch.Begin(SpriteSortMode.Immediate,BlendState.AlphaBlend,SamplerState.LinearClamp,
            DepthStencilState.None,RasterizerState.CullNone,material,Main.GameViewMatrix.TransformationMatrix);
        try
        {
            device.Textures[1]=EvolutionVisuals.Asset("EnergyNoise");device.SamplerStates[1]=SamplerState.LinearWrap;
            EvolutionLaserVisuals.ActiveMaterial=material;
            foreach(Projectile projectile in Main.ActiveProjectiles)
                if(projectile.ModProjectile is EvolutionHazard h && h.UsesContinuousLaser && h.Parent is Evolution boss && boss.Serial==h.Serial)
                    h.DrawContinuousBodies();
        }
        finally
        {
            EvolutionLaserVisuals.ActiveMaterial=null;Main.spriteBatch.End();device.Textures[1]=oldNoise;device.SamplerStates[1]=oldSampler;
        }
    }
}

public sealed partial class EvolutionHazard
{
    internal bool UsesContinuousLaser => Kind is EvolutionShot.Radiation or EvolutionShot.CrashFan ||
        !IsAxis && (Kind is EvolutionShot.Beam or EvolutionShot.Spike);
    internal int ContinuousActiveTicks => BeamStyle is EvolutionBeamStyle.Pulse or EvolutionBeamStyle.PianoStaccato ? 6 :
        BeamStyle==EvolutionBeamStyle.Piano ? EvolutionPianoRules.Active : Parent?.CorridorActive==true?32:10;
    internal float ContinuousCollisionWidth => EvolutionLaserVisuals.CollisionWidth(BeamStyle, Parent?.CorridorActive == true);
    internal int ContinuousFadeTicks => EvolutionPianoRules.IsStyle(BeamStyle)?EvolutionPianoRules.Fade:EvolutionLaserVisuals.FadeTicks;
    internal float ContinuousEnvelope => EvolutionRules.BeamEnvelope(Age-FireAge,ContinuousActiveTicks,ContinuousFadeTicks,
        Parent?.Phase==5);
    private void UpdateContinuousLaserDust()
    {
        if(Main.dedServ||!UsesContinuousLaser||Age<FireAge||Age%6!=0)return;
        float envelope=Kind==EvolutionShot.Radiation?
            EvolutionRules.BeamEnvelope(Age-FireAge,8,EvolutionNestRules.LaserFadeTicks,true):ContinuousEnvelope;
        if(envelope<.05f)return;
        int count=Kind==EvolutionShot.Radiation?5:1;
        Color color=Parent?.ThemeColor??EvolutionVisuals.Blood;
        for(int i=0;i<count;i++)
        {
            Vector2 direction=Kind==EvolutionShot.Radiation?EvolutionNestRules.Direction((int)Parameter,i).ToRotationVector2():Projectile.velocity.SafeNormalize(Vector2.UnitY);
            float reach=Kind==EvolutionShot.Radiation?EvolutionNestRules.RadiationReach:Parameter;
            Vector2 point=Projectile.Center+direction*Main.rand.NextFloat(.2f,.95f)*reach;
            Dust dust=Dust.NewDustPerfect(point,DustID.FireworkFountain_Red,
                direction*Main.rand.NextFloat(2,5)+direction.RotatedBy(MathHelper.PiOver2)*Main.rand.NextFloat(-1,1),0,color,.8f*envelope);
            dust.noGravity=true;
        }
    }
    internal void DrawContinuousBodies()
    {
        if(Kind==EvolutionShot.Radiation)
        {
            int fired=Age-FireAge;
            float envelope=EvolutionRules.BeamEnvelope(fired,EvolutionNestRules.LaserActiveTicks,EvolutionNestRules.LaserFadeTicks,true);
            for(int ray=0;ray<5;ray++)
            {
                Vector2 direction=EvolutionNestRules.Direction((int)Parameter,ray).ToRotationVector2();
                EvolutionLaserVisuals.Draw(Projectile.Center,Projectile.Center+direction*EvolutionNestRules.RadiationReach,
                    fired,envelope,EvolutionNestRules.LaserWidth,EvolutionVisuals.Sand,EvolutionVisuals.SandCore);
            }
            return;
        }
        if(Kind==EvolutionShot.CrashFan)
        {
            int fired=Age-Delay;
            float envelope=EvolutionRules.BeamEnvelope(fired,EvolutionBroodfallRules.FanActive,EvolutionBroodfallRules.FanFade);
            for(int ray=0;ray<EvolutionBroodfallRules.FanRayCount;ray++)
            {
                Vector2 direction=CrashFanDirection(ray);
                DrawRayBody(RayPath(Projectile.Center+direction*75,direction,EvolutionBroodfallRules.FanReach-75),
                    fired,envelope,EvolutionBroodfallRules.FanWidth,EvolutionVisuals.Blood,EvolutionVisuals.Core);
            }
            return;
        }
        Vector2 d=Projectile.velocity.SafeNormalize(Vector2.UnitY);
        DrawRayBody(RayPath(Projectile.Center,d,Parameter>0?Parameter:1900*EvolutionRules.LaserReachMultiplier),
            Age-FireAge,ContinuousEnvelope,ContinuousCollisionWidth,Parent?.ThemeColor??EvolutionVisuals.Blood,EvolutionVisuals.Core);
    }
}
