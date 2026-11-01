using System;
using ArknightsMod.Content.Items.Weapons.Medic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace ArknightsMod.Content.Projectiles.Medic.Arts;

internal static class MedicIdentityVisuals
{
    internal static Effect Material;
    private static readonly VertexPositionColorTexture[] Quad=new VertexPositionColorTexture[4];
    internal static void DrawGlyph(int id,Vector2 center,Vector2 size,float rotation,float time,Color body,Color core,float opacity)
    {
        if(Main.dedServ)return;
        if(!MedicMaterialVfx.UsesLineArt(id) && id!=22) throw new InvalidOperationException("Discarded medical glyph: " + id);
        Material??=ModContent.Request<Effect>("ArknightsMod/Assets/Effects/MedicIdentity",AssetRequestMode.ImmediateLoad).Value;
        Vector2 x=rotation.ToRotationVector2()*size.X*.5f,y=x.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2)*size.Y*.5f;
        Vector2 c=center-Main.screenPosition;
        Quad[0]=new(new Vector3(c-x-y,0),Color.White,new(0,0));Quad[1]=new(new Vector3(c+x-y,0),Color.White,new(1,0));
        Quad[2]=new(new Vector3(c-x+y,0),Color.White,new(0,1));Quad[3]=new(new Vector3(c+x+y,0),Color.White,new(1,1));
        var device=Main.spriteBatch.GraphicsDevice;
        Main.spriteBatch.End();
        try
        {
            device.BlendState=BlendState.Additive;device.DepthStencilState=DepthStencilState.None;device.RasterizerState=RasterizerState.CullNone;
            Material.Parameters["Transform"].SetValue(Main.GameViewMatrix.TransformationMatrix*Matrix.CreateOrthographicOffCenter(0,device.Viewport.Width,device.Viewport.Height,0,0,1));
            Material.CurrentTechnique=Material.Techniques[id==22?"PerfumePetals":"Identity"+id];Material.Parameters["Clock"].SetValue(time/60f);
            Material.Parameters["Opacity"].SetValue(opacity);Material.Parameters["BodyTint"].SetValue(body.ToVector3());Material.Parameters["CoreTint"].SetValue(core.ToVector3());
            Material.CurrentTechnique.Passes[0].Apply();device.DrawUserPrimitives(PrimitiveType.TriangleStrip,Quad,0,2);
        }
        finally
        {
            RestoreSprites();
        }
    }
    internal static void RestoreSprites()
    {
        Main.spriteBatch.Begin(SpriteSortMode.Immediate,BlendState.AlphaBlend,Main.DefaultSamplerState,
            DepthStencilState.None,RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
        Main.spriteBatch.Draw(TextureAssets.MagicPixel.Value,Vector2.Zero,new Rectangle(0,0,1,1),Color.Transparent);
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,Main.DefaultSamplerState,
            DepthStencilState.None,RasterizerState.CullCounterClockwise,null,Main.GameViewMatrix.TransformationMatrix);
    }
    internal static void MaterialSprites(Action draw)
    {
        Main.spriteBatch.End();
        Main.spriteBatch.Begin(SpriteSortMode.Deferred,BlendState.AlphaBlend,SamplerState.LinearClamp,
            DepthStencilState.None,RasterizerState.CullNone,null,Main.GameViewMatrix.TransformationMatrix);
        try { draw(); }
        finally { Main.spriteBatch.End(); RestoreSprites(); }
    }
    internal static void DrawBody(int id,Vector2 center,Vector2 size,float rotation,float time,Color body,Color core,float opacity)
    {
        if(MedicMaterialVfx.UsesLineArt(id))DrawGlyph(id,center,size,rotation,time,body,core,opacity);
        else MaterialSprites(()=>MedicMaterialVfx.Body(id,center,size,rotation,time,body,core,opacity));
    }
    internal static Vector2 Size(int id) => id switch {
        0=>new(43,24),1=>new(40,38),2=>new(72,32),3=>new(52,48),4=>new(48,48),5=>new(100,38),
        6=>new(48,40),7=>new(52,46),8=>new(44,54),9=>new(48,60),10=>new(60,60),11=>new(60,60),
        12=>new(62,50),13=>new(80,54),14=>new(60,42),15=>new(66,40),16=>new(68,50),17=>new(52,38),
        18=>new(48,56),19=>new(58,48),20=>new(72,64),21=>new(54,54),22=>new(52,52),23=>new(50,58),
        24=>new(95,65),_=>new(92,40)};
    internal static void Draw(MedicArt art)
    {
        int id=art.ProfileId;var q=art.Projectile;var p=art.Profile;var f=MedicIdentity.Get(id);
        float fade=Math.Min(MathHelper.Clamp(art.Age/6,0,1),MathHelper.Clamp(q.timeLeft/24f,0,1));
        if(p.Branch == MedicBranch.Chain) { MedicLightningVfx.Draw(art, fade); return; }
        if(MedicSpriteFlightVisuals.Handles(id))
        {
            MedicSpriteFlightVisuals.Draw(art,fade);
            return;
        }
        // Ampoules, rotors, lanterns, rain and bubbles stay upright; blades follow their flight.
        float rotation=id is 1 or 3 or 4 or 8 or 9 or 10 or 12 or 13 or 18 or 20 or 22 or 23 ? 0:q.rotation;
        DrawWake(q,id,p.Body,p.Core,f,art.Age,fade);
        float pulse=1+MathF.Sin(art.Age*(.07f+id%4*.018f))*.06f;
        if(id==5)
            MedicArtsVfx.Glow(q.Center,Size(id)*new Vector2(.95f,.6f),new Color(32,35,65),fade*.6f,rotation);
        else
            MedicArtsVfx.Glow(q.Center,Size(id)*(.85f*pulse),p.Body,fade*(id is 3 or 12 or 19?.12f:.22f),rotation);
        DrawBody(id,q.Center,Size(id),rotation,art.Age,p.Body,p.Core,fade*(.45f+.55f*art.Armed));
    }
    internal static void DrawWake(Projectile q,int id,Color body,Color core,MedicFlight f,float age,float fade)
    {
        if(!MedicMaterialVfx.UsesLineArt(id))
        {
            MedicPointedTrail.Draw(q,MedicMaterialVfx.Get(id),body,core,age,fade);
            return;
        }
        Vector2 previous=q.Center;float length=0;
        for(int i=0;i<q.oldPos.Length;i++)
        {
            if(q.oldPos[i]==Vector2.Zero)break;
            Vector2 at=q.oldPos[i]+q.Size*.5f;float d=Vector2.Distance(at,previous);
            if(d>96||length>f.Trail)break;
            length+=d;float alpha=MathF.Pow(Math.Max(0,1-length/f.Trail),1.5f)*fade;
            // Individual wakes: discrete packets, twin rails, fluid pearls, paper folds and curls.
            if(id is 0 or 2 or 5 or 12 or 16 or 19)
            {
                if(id!=12 || i%3!=0)MedicArtsVfx.Thread(previous,at,body,f.Width,alpha*.65f);
                if(id is 2 or 19) {Vector2 side=q.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2)*4;MedicArtsVfx.Thread(previous+side,at+side,core,.6f,alpha*.65f);}
            }
            else if(id is 1 or 8 or 10 or 18 or 20)
            {if(i%3==0)MedicArtsVfx.Glow(at,new Vector2(f.Width*2.5f),body,alpha*.45f);}
            else
            {
                Vector2 side=q.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2)*MathF.Sin(age*.12f-i*.45f)*(id==15?8:4);
                MedicArtsVfx.Thread(previous+side,at+side,body,f.Width,alpha*.6f);
            }
            previous=at;
        }
    }
    internal static void Feedback(Vector2 center,int id,float size,float time,float opacity)
    {
        var p=MedicCatalog.Get(id);
        DrawBody(id,center,Size(id)*size,0,time,p.Body,p.Core,opacity);
    }
    internal static void Field(MedicHealingField field) => MedicFieldVfx.Draw(field);
}
