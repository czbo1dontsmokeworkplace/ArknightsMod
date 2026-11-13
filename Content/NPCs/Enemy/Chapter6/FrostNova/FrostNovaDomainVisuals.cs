using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace ArknightsMod.Content.NPCs.Enemy.Chapter6.FrostNova;

// The same world-pixel radius drives gameplay and rendering, including camera zoom.
// Drawn before projectiles so ice fog obstructs the arena while attack tells remain readable.
public sealed class FrostNovaDomainVisuals : ModSystem
{
    private static Effect material;
    private static readonly VertexPositionTexture[] quad = new VertexPositionTexture[4];
    public override void Unload() => material = null;
    internal static void Draw(Vector2 center, float radius, float exposure, float opacity, float age = -1)
    {
        if (Main.dedServ || opacity <= 0 || Main.spriteBatch?.GraphicsDevice == null || Main.GameViewMatrix == null) return;
        // First draw can happen before an asynchronous request finishes, including
        // immediately after a mod reload. Never cache or dereference a pending Value.
        if (material == null || material.IsDisposed)
            material = ModContent.Request<Effect>("ArknightsMod/Assets/Effects/FrostNovaDomain", ReLogic.Content.AssetRequestMode.ImmediateLoad).Value;
        if (material == null || material.IsDisposed) return;
        Matrix inverse = Matrix.Invert(Main.GameViewMatrix.TransformationMatrix);
        Vector2 World(float x, float y) => Main.screenPosition + Vector2.Transform(new Vector2(x, y), inverse);
        quad[0] = new(new Vector3(-1, 1, 0), World(0, 0));
        quad[1] = new(new Vector3(1, 1, 0), World(Main.screenWidth, 0));
        quad[2] = new(new Vector3(-1, -1, 0), World(0, Main.screenHeight));
        quad[3] = new(new Vector3(1, -1, 0), World(Main.screenWidth, Main.screenHeight));
        material.Parameters["Anchor"].SetValue(center);
        material.Parameters["Radius"].SetValue(radius);
        material.Parameters["Clock"].SetValue(Main.GlobalTimeWrappedHourly);
        material.Parameters["Exposure"].SetValue(exposure);
        material.Parameters["Opacity"].SetValue(opacity);
        material.Parameters["Age"].SetValue(age);
        material.CurrentTechnique = material.Techniques[age >= 0 ? "LandingPass" : "DomainPass"];
        GraphicsDevice device = Main.spriteBatch.GraphicsDevice;
        BlendState blend = device.BlendState;
        DepthStencilState depth = device.DepthStencilState;
        RasterizerState rasterizer = device.RasterizerState;
        try {
            device.BlendState = BlendState.AlphaBlend;
            device.DepthStencilState = DepthStencilState.None;
            device.RasterizerState = RasterizerState.CullNone;
            material.CurrentTechnique.Passes[0].Apply();
            device.DrawUserPrimitives(PrimitiveType.TriangleStrip, quad, 0, 2);
        }
        finally {
            Main.pixelShader.CurrentTechnique.Passes[0].Apply();
            device.BlendState = blend; device.DepthStencilState = depth; device.RasterizerState = rasterizer;
        }
    }
    public override void PostDrawTiles()
    {
        if (Main.dedServ || Main.gameMenu || Main.LocalPlayer.dead) return;
        FrostNova boss = FrostNovaDomainRules.Nearest(Main.LocalPlayer);
        if (boss != null) Draw(boss.HaloCenter, FrostNovaDomainRules.Radius,
            Main.LocalPlayer.GetModPlayer<FrostNovaDomainPlayer>().Obscurity, MathHelper.Clamp(boss.DomainOpacity, 0, 1));
        foreach (NPC npc in Main.ActiveNPCs)
            if (npc.ModNPC is FrostNova frost && frost.LandingAge >= 0 && frost.LandingAge < 60)
                Draw(frost.RevivalAnchor + new Vector2(0, 29), 720, 0, 1, frost.LandingAge);
    }
}
