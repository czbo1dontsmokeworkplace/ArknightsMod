using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using ReLogic.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

public sealed class CognitiveOverwrite : ModBuff
{
    public override string Texture => "Terraria/Images/Buff_" + BuffID.Obstructed;
    public override void SetStaticDefaults() { Main.debuff[Type] = true; Main.buffNoSave[Type] = true; Main.buffNoTimeDisplay[Type] = true; }
}

public sealed class HarbingerInterference : ModSystem
{
    private const string FilterKey = "ArknightsMod:HarbingerCapture";
    internal static AACT Current { get; private set; }
    internal static bool Corrupting => !Main.gameMenu && Current?.NPC.active == true;
    internal static float Hack => Corrupting ? Current.HackStrength : 0;
    private readonly List<IDisposable> hooks = new();
    private static readonly Dictionary<string, string> textCache = new();
    private static int textTick = -1;
    private static bool dialog;
    private static Vector2 layerOffset;
    private static bool mouseAdjusted;
    private RenderTarget2D copy;
    private SpriteBatch compositor;
    private bool compositorFailed;

    private delegate void FontOrig(DynamicSpriteFont font, string text, SpriteBatch batch, Vector2 pos, Color color, float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects effects, float depth);
    private delegate bool LayerOrig(GameInterfaceLayer layer);
    private delegate void BeginOrig(SpriteBatch batch, SpriteSortMode sort, BlendState blend, SamplerState sampler, DepthStencilState depth, RasterizerState raster, Effect effect, Matrix transform);
    public override void Load()
    {
        if (Main.dedServ) return;
        Filters.Scene[FilterKey] = new Filter(new ScreenShaderData("FilterMiniTower").UseColor(.02f, .01f, .02f).UseOpacity(.001f), EffectPriority.High);
        Filters.Scene[FilterKey].Load();
        var method = typeof(DynamicSpriteFont).GetMethod("InternalDraw", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new MissingMethodException("DynamicSpriteFont.InternalDraw");
        hooks.Add(new Hook(method, (FontOrig orig, DynamicSpriteFont font, string text, SpriteBatch batch, Vector2 pos, Color color,
            float rotation, Vector2 origin, ref Vector2 scale, SpriteEffects effects, float depth) => {
                if (!Corrupting || string.IsNullOrWhiteSpace(text)) { orig(font, text, batch, pos, color, rotation, origin, ref scale, effects, depth); return; }
                string corrupted = TransformText(text);
                Vector2 localScale = scale;
                float width = font.MeasureString(corrupted).X;
                if (width > 0) localScale.X *= font.MeasureString(text).X / width;
                int tick = (int)(Main.GameUpdateCount / 3);
                float jitter = tick % 19 < 3 ? (tick % 5 - 2) * (1 + Hack * 2) : 0;
                pos.X += jitter;
                // Black outline passes retain their role. The foreground gets the reference's RGB split.
                if (color.R + color.G + color.B > 25) {
                    orig(font, corrupted, batch, pos - new Vector2(1 + Hack * 2, 0), new Color(255, 35, 90, color.A) * .35f, rotation, origin, ref localScale, effects, depth);
                    orig(font, corrupted, batch, pos + new Vector2(1 + Hack * 2, 0), new Color(20, 220, 255, color.A) * .35f, rotation, origin, ref localScale, effects, depth);
                    color = Color.Lerp(color, new Color(95, 255, 175, color.A), .35f);
                }
                orig(font, corrupted, batch, pos, color, rotation, origin, ref localScale, effects, depth);
            }));
        // FNA fonts are also used by combat text and mods that bypass DynamicSpriteFont.
        foreach (MethodInfo m in typeof(SpriteBatch).GetMethods()) {
            ParameterInfo[] p = m.GetParameters();
            if (m.Name != "DrawString" || p.Length != 9 || p[6].ParameterType != typeof(Vector2)) continue;
            bool builder = p[1].ParameterType == typeof(StringBuilder);
            hooks.Add(new ILHook(m, il => {
                var c = new ILCursor(il);
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_2);
                if (builder) c.EmitDelegate<Func<StringBuilder, StringBuilder>>(s => Corrupting && s != null ? new StringBuilder(TransformText(s.ToString())) : s);
                else c.EmitDelegate<Func<string, string>>(TransformText);
                c.Emit(Mono.Cecil.Cil.OpCodes.Starg_S, (byte)2);
            }));
        }
        hooks.Add(new Hook(typeof(GameInterfaceLayer).GetMethod(nameof(GameInterfaceLayer.Draw)), (LayerOrig orig, GameInterfaceLayer layer) => {
            Vector2 previous = layerOffset;
            int x = Main.mouseX, y = Main.mouseY;
            bool wasAdjusted = mouseAdjusted;
            layerOffset = UIShift(layer.Name); mouseAdjusted = false;
            try { return orig(layer); }
            finally { Main.mouseX = x; Main.mouseY = y; layerOffset = previous; mouseAdjusted = wasAdjusted; }
        }));
        hooks.Add(new Hook(typeof(SpriteBatch).GetMethod("Begin", new[] { typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState), typeof(Effect), typeof(Matrix) }),
            (BeginOrig orig, SpriteBatch batch, SpriteSortMode sort, BlendState blend, SamplerState sampler, DepthStencilState depth, RasterizerState raster, Effect effect, Matrix transform) => {
                if (layerOffset != Vector2.Zero && batch == Main.spriteBatch) {
                    Matrix matrix = transform;
                    if (!mouseAdjusted) {
                        // Apply the inverse of the same display translation to UI hit testing.
                        Main.mouseX -= (int)(layerOffset.X / Math.Max(.1f, matrix.M11));
                        Main.mouseY -= (int)(layerOffset.Y / Math.Max(.1f, matrix.M22));
                        mouseAdjusted = true;
                    }
                    transform = matrix * Matrix.CreateTranslation(layerOffset.X, layerOffset.Y, 0);
                }
                orig(batch, sort, blend, sampler, depth, raster, effect, transform);
            }));
        On_FilterManager.EndCapture += EndCapture;
    }
    internal static string TransformText(string source)
    {
        if (!Corrupting || string.IsNullOrEmpty(source)) return source;
        int tick = (int)(Main.GameUpdateCount / 4);
        if (tick != textTick) { textTick = tick; textCache.Clear(); }
        if (!textCache.TryGetValue(source, out string value)) {
            value = HarbingerRules.Corrupt(source, tick, source.GetHashCode());
            if (textCache.Count < 2048) textCache[source] = value;
        }
        return value;
    }
    private static Vector2 UIShift(string name)
    {
        if (Hack <= 0 || !(name.Contains("Inventory") || name.Contains("Map") || name.Contains("Resource Bars"))) return Vector2.Zero;
        int tick = (int)(Main.GameUpdateCount / 5);
        int seed = name.Contains("Map") ? 11 : name.Contains("Inventory") ? 7 : 3;
        if ((tick + seed) % 9 > 3) return Vector2.Zero;
        return new Vector2(((tick * seed) % 7 - 3) * 9, ((tick + seed) % 5 - 2) * 5) * Hack;
    }
    public override void PostUpdateEverything()
    {
        AACT previous = Current;
        Current = null;
        if (Main.gameMenu) return;
        foreach (NPC n in Main.ActiveNPCs) if (n.ModNPC is AACT boss) { Current = boss; break; }
        if (!Main.dedServ) {
            if (Current == null && previous?.Phase == 4) ShowError();
            Filter filter = Filters.Scene[FilterKey];
            if (Hack > 0 && !filter.IsActive()) Filters.Scene.Activate(FilterKey, Current.NPC.Center);
            if (Hack <= 0 && filter.IsActive()) filter.Deactivate();
        }
        int type = ModContent.BuffType<CognitiveOverwrite>();
        foreach (Player p in Main.ActivePlayers) {
            if (Current != null) {
                // A boss-presence effect: immunity and cleansing cannot leave a missing interval.
                p.buffImmune[type] = false;
                if (!p.dead) p.AddBuff(type, 2, quiet: true);
            }
            else if (p.HasBuff(type)) p.ClearBuff(type);
        }
    }
    public override void ModifyScreenPosition()
    {
        if (Hack <= 0) return;
        int tick = (int)(Main.GameUpdateCount / 4);
        Main.screenPosition += new Vector2((tick * 13 % 11 - 5) * 2, (tick * 5 % 7 - 3) * 1.5f) * Hack;
    }
    private void EndCapture(On_FilterManager.orig_EndCapture orig, FilterManager self, RenderTarget2D final, RenderTarget2D first, RenderTarget2D second, Color clear)
    {
        if (Hack <= 0 || compositorFailed) { orig(self, final, first, second, clear); return; }
        GraphicsDevice gd = Main.instance.GraphicsDevice;
        Viewport viewport = gd.Viewport;
        Rectangle scissor = gd.ScissorRectangle;
        BlendState blend = gd.BlendState; SamplerState sampler = gd.SamplerStates[0];
        RasterizerState raster = gd.RasterizerState; DepthStencilState depth = gd.DepthStencilState;
        bool captured = false;
        try {
            int w = final?.Width ?? gd.PresentationParameters.BackBufferWidth;
            int h = final?.Height ?? gd.PresentationParameters.BackBufferHeight;
            if (copy == null || copy.IsDisposed || copy.Width != w || copy.Height != h) {
                copy?.Dispose(); copy = new RenderTarget2D(gd, w, h, false, final?.Format ?? SurfaceFormat.Color, DepthFormat.None);
            }
            compositor ??= new SpriteBatch(gd);
            // Normal gameplay uses final == null (the backbuffer). Capture the
            // complete filter chain into our own target before composing to it.
            orig(self, copy, first, second, clear); captured = true;
            gd.SetRenderTarget(final);
            gd.Clear(clear);
            compositor.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            int tick = (int)(Main.GameUpdateCount / 3);
            int height = Math.Max(12, h / 28);
            for (int y = 0, row = 0; y < h; y += height, row++) {
                int offset = row % 4 == tick % 4 ? (int)((row * 37 + tick * 11) % 61 - 30) : 0;
                offset = (int)(offset * Hack);
                compositor.Draw(copy, new Rectangle(offset, y, w, Math.Min(height, h - y)),
                    new Rectangle(0, y, w, Math.Min(height, h - y)), Color.White);
            }
            compositor.End();
            compositor.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            compositor.Draw(copy, new Vector2(5 * Hack, 0), new Color(65, 0, 12) * Hack);
            compositor.Draw(copy, new Vector2(-5 * Hack, 0), new Color(0, 32, 45) * Hack);
            compositor.End();
        }
        catch (Exception ex) {
            try { compositor?.End(); } catch { }
            compositorFailed = true; Mod.Logger.Error("Harbinger screen compositor disabled after a graphics failure", ex);
            if (!captured) orig(self, final, first, second, clear);
            else {
                gd.SetRenderTarget(final);
                compositor.Begin(); compositor.Draw(copy, Vector2.Zero, Color.White); compositor.End();
            }
        }
        finally {
            gd.SetRenderTarget(final); gd.Viewport = viewport; gd.ScissorRectangle = scissor;
            gd.BlendState = blend; gd.SamplerStates[0] = sampler; gd.RasterizerState = raster; gd.DepthStencilState = depth;
        }
    }
    public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers) => layers.Add(new LegacyGameInterfaceLayer(
        "ArknightsMod: Harbinger interference and error", DrawOverlay, InterfaceScaleType.UI));
    private static bool DrawOverlay()
    {
        if (Main.gameMenu) return true;
        SpriteBatch b = Main.spriteBatch;
        // The UI layer has already converted these dimensions to UI coordinates.
        float w = Main.screenWidth, h = Main.screenHeight;
        float hack = Hack;
        if (Current != null) {
            float intensity = Current.Phase == 3 ? .14f + hack * .26f : .035f;
            int step = Current.Phase == 3 ? 5 : 9;
            for (int y = 0; y < h; y += step) b.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, y, (int)w, 1), new Color(20, 0, 15) * intensity);
            float scan = (Main.GameUpdateCount * (Current.Phase == 3 ? 11 : 2)) % Math.Max(1, h);
            b.Draw(TextureAssets.MagicPixel.Value, new Rectangle(0, (int)scan, (int)w, Current.Phase == 3 ? 7 : 2), HarbingerVisuals.Cyan * (.06f + hack * .2f));
            if (hack > 0) for (int i = 0; i < 9; i++) {
                int tick = (int)(Main.GameUpdateCount / 4);
                int x = (tick * 97 + i * 193) % Math.Max(1, (int)w), y = (tick * 53 + i * 101) % Math.Max(1, (int)h);
                b.Draw(TextureAssets.MagicPixel.Value, new Rectangle(x, y, 40 + i * 9, 3 + i % 3 * 5), (i % 2 == 0 ? HarbingerVisuals.Red : HarbingerVisuals.Cyan) * (hack * .13f));
            }
        }
        if (dialog) DrawError(b, w, h);
        return true;
    }
    internal static void ShowError()
    {
        if (Main.dedServ || Main.gameMenu || dialog) return;
        dialog = true;
        SoundEngine.PlaySound(new SoundStyle("ArknightsMod/Sounds/HarbingerWindowsError") with { Volume = .8f, MaxInstances = 1 });
    }
    private static void DrawError(SpriteBatch b, float screenW, float screenH)
    {
        float width = Math.Min(490, screenW - 24), height = 210;
        Rectangle panel = new((int)((screenW - width) / 2), (int)((screenH - height) / 2), (int)width, (int)height);
        void Fill(Rectangle r, Color c) => b.Draw(TextureAssets.MagicPixel.Value, r, c);
        Fill(new Rectangle(panel.X + 6, panel.Y + 6, panel.Width, panel.Height), Color.Black * .45f);
        Fill(panel, new Color(192, 192, 192));
        Fill(new Rectangle(panel.X + 3, panel.Y + 3, panel.Width - 6, 27), new Color(0, 0, 128));
        HarbingerVisuals.Line(b, new(panel.Left, panel.Bottom), new(panel.Left, panel.Top), Color.White, 2);
        HarbingerVisuals.Line(b, new(panel.Left, panel.Top), new(panel.Right, panel.Top), Color.White, 2);
        HarbingerVisuals.Line(b, new(panel.Right, panel.Top), new(panel.Right, panel.Bottom), new Color(64, 64, 64), 2);
        HarbingerVisuals.Line(b, new(panel.Right, panel.Bottom), new(panel.Left, panel.Bottom), new Color(64, 64, 64), 2);
        int tick = (int)(Main.GameUpdateCount / 5);
        void Text(string value, Vector2 pos, Color color, float scale = .8f) =>
            b.DrawString(FontAssets.MouseText.Value, HarbingerRules.Corrupt(value, tick, 713), pos, color,
                0, Vector2.Zero, scale, SpriteEffects.None, 0);
        Text("ERROR::0xDEAD//NULL", new(panel.X + 10, panel.Y + 5), Color.White);
        Vector2 icon = new(panel.X + 39, panel.Y + 76);
        for (int y = -17; y <= 17; y++) {
            int span = (int)MathF.Sqrt(Math.Max(0, 17 * 17 - y * y));
            Fill(new Rectangle((int)icon.X - span, (int)icon.Y + y, span * 2 + 1, 1), new Color(195, 0, 0));
        }
        HarbingerVisuals.Line(b, icon - new Vector2(7), icon + new Vector2(7), Color.White, 4);
        HarbingerVisuals.Line(b, icon + new Vector2(7, -7), icon + new Vector2(-7, 7), Color.White, 4);
        Text("0xDEAD // TERMINAL REFERENCE LOST", new(panel.X + 70, panel.Y + 53), Color.Black, .72f);
        Text("NULL::01AF / CORE NOT FOUND", new(panel.X + 70, panel.Y + 80), Color.Black, .72f);
        Text("FATAL EXCEPTION 0xC0000005", new(panel.X + 70, panel.Y + 107), Color.Black, .72f);
        Rectangle close = new(panel.Right - 27, panel.Y + 6, 20, 20);
        Rectangle ok = new(panel.Center.X - 48, panel.Bottom - 45, 96, 29);
        Fill(close, new Color(192, 192, 192)); Fill(ok, new Color(220, 220, 220));
        HarbingerVisuals.Line(b, ok.TopLeft(), new(ok.Right, ok.Top), Color.White, 2);
        HarbingerVisuals.Line(b, new(ok.Right, ok.Top), new(ok.Right, ok.Bottom), Color.Gray, 2);
        HarbingerVisuals.Line(b, new(ok.Right, ok.Bottom), ok.BottomLeft(), Color.Gray, 2);
        HarbingerVisuals.Line(b, new(close.X + 6, close.Y + 6), new(close.Right - 6, close.Bottom - 6), Color.Black, 2);
        HarbingerVisuals.Line(b, new(close.Right - 6, close.Y + 6), new(close.X + 6, close.Bottom - 6), Color.Black, 2);
        Text("OK", new(ok.Center.X - 12, ok.Y + 2), Color.Black);
        Vector2 mouse = new(Main.mouseX, Main.mouseY);
        if (panel.Contains(mouse.ToPoint())) Main.LocalPlayer.mouseInterface = true;
        if ((ok.Contains(mouse.ToPoint()) || close.Contains(mouse.ToPoint())) && Main.mouseLeft && Main.mouseLeftRelease) {
            dialog = false; Main.mouseLeftRelease = false; Main.LocalPlayer.mouseInterface = true;
        }
    }
    private void Reset()
    {
        if (!Main.dedServ && Filters.Scene[FilterKey]?.IsActive() == true) Filters.Scene[FilterKey].Deactivate();
        Current = null; dialog = false; layerOffset = Vector2.Zero; mouseAdjusted = false;
        textCache.Clear(); textTick = -1; compositorFailed = false;
    }
    public override void OnWorldUnload() => Reset();
    public override void Unload()
    {
        if (!Main.dedServ) On_FilterManager.EndCapture -= EndCapture;
        foreach (IDisposable hook in hooks) hook.Dispose(); hooks.Clear(); Reset();
        RenderTarget2D old = copy; SpriteBatch batch = compositor; copy = null; compositor = null;
        if (!Main.dedServ) Main.QueueMainThreadAction(() => { old?.Dispose(); batch?.Dispose(); });
    }
}

public sealed class HarbingerViewPlayer : ModPlayer
{
    public override void ModifyDrawInfo(ref PlayerDrawSet drawInfo)
    {
        if (Player.whoAmI != Main.myPlayer || HarbingerInterference.Hack <= 0) return;
        int tick = (int)(Main.GameUpdateCount / 4);
        drawInfo.Position += new Vector2((tick * 17 % 5 - 2) * 4, tick % 3 - 1) * HarbingerInterference.Hack;
    }
}
