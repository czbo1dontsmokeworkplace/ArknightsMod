using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Graphics;
using Terraria.GameContent;

namespace ArknightsMod.Content.NPCs.Enemy.RoaringFlare.ImperialArtilleyCoreTargeteer;

// A display-only intrusion layer: rising red terminal fragments and brief, structured
// peripheral traces. Stateless hashes keep each packet coherent without using game RNG.
internal static class HarbingerHackHud
{
    private static readonly Color Code = new(242, 76, 107), Trace = new(99, 172, 218);
    private static readonly string[] CodeLines = {
        "typedef struct __gate {", "    uint32_t mask = 0xDEAD;", "    void* source = NULL;",
        "    bus[0x2F] ^= latch;", "    if (gate != 0x0000) {", "        patch(mask << 3);",
        "        return __FAULT__;", "    }", "} memory_fault;", "#define CORE_REF 0x01AF",
        "static void trace_init() {", "    reg_t* e = &core[13];", "    e->flags &= ~0xFF;",
        "    for (;;) { sync(e); }", "}", "extern volatile int _lost;", "    stack[sp++] = 0xBAD0;",
        "    __write(bus, &signal);", "    if (!ack) goto retry;", "retry:  latch = 0xFFFF;",
        "    checksum ^= 0xC013;", "    // terminal reference lost", "    return (void*) -1;", "};",
        "static struct gate* alloc_trace(uint32_t index) {", "    source = reinterpret_cast<void*>(0xDEAD);",
        "    trace_error(core_ref, stack[sp++], 0x0000013F);", "    if (signal != ACK) return __CORE_NOT_FOUND__;"
    };
    private static readonly string[] Logs = {
        "\\SYSTEM32\\CORE_REF", "   error code:", "   error   = 0xC000012F", "   fault   = 0x0000DEAD",
        "   bus     = [NULL]", "   retry   = 0x0000013F", "   signal  = LOST", "   access  = DENIED"
    };
    private static uint Hash(int value) {
        uint n = unchecked((uint)value); n ^= n >> 16; n *= 0x7feb352du; n ^= n >> 15; n *= 0x846ca68bu;
        return n ^ (n >> 16);
    }
    private static float Random(int seed) => (Hash(seed) & 65535) / 65535f;
    private static float Pulse(int age, int lifetime) => age < 0 || age >= lifetime ? 0
        : Math.Min(1, Math.Min((age + 1) / 2f, (lifetime - age) / 5f));

    internal static void Draw(SpriteBatch batch, float width, float height, int clock, int seed, float strength)
    {
        if (strength <= 0 || width < 80 || height < 80) return;
        strength = Math.Clamp(strength, 0, 1);
        float unit = Math.Clamp(height / 900f, .4f, 2f);
        // Clock is continuous throughout the existing hack window. A row's identity
        // changes only when it has scrolled past the top, never once per draw/frame.
        DrawCode(batch, width, height, clock, seed, unit, strength);
        DrawPackets(batch, width, height, clock, seed, unit, strength);
    }
    private static void DrawCode(SpriteBatch batch, float width, float height, int clock, int seed, float unit, float strength)
    {
        DynamicSpriteFont font = FontAssets.MouseText.Value;
        float fontScale = .49f * unit, lineHeight = 12 * unit;
        float scroll = clock * .88f * unit;
        int firstRow = (int)(scroll / lineHeight);
        float fraction = scroll % lineHeight;
        for (int column = 0; column < 2; column++) {
            float x = width * (column == 0 ? .31f : .59f);
            float bottom = height * (column == 0 ? .985f : 1.02f);
            int rows = column == 0 ? 17 : 9;
            for (int row = 0; row < rows; row++) {
                float y = bottom - row * lineHeight - fraction;
                float fade = Math.Clamp((y - height * .67f) / (46 * unit), 0, 1)
                    * Math.Clamp((height - y) / (18 * unit), 0, 1);
                int index = (firstRow + rows - row + (int)(Hash(seed + column * 79) % CodeLines.Length)) % CodeLines.Length;
                string text = CodeLines[index];
                float alpha = strength * fade * (column == 0 ? .62f : .24f);
                if (alpha <= .005f) continue;
                // Indented fragments and uneven blank lines retain the reference's code-block silhouette.
                Vector2 at = new(x + (row % 7 == 0 ? 9 * unit : 0), y);
                batch.DrawString(font, text, at, Code * alpha, 0, Vector2.Zero, fontScale, SpriteEffects.None, 0);
            }
        }
        // Faint peripheral diagnostics, separate from the denser lower-center stream.
        for (int side = 0; side < 2; side++) {
            float x = side == 0 ? 7 * unit : Math.Max(width * .78f, width - 238 * unit);
            float baseY = height * (side == 0 ? .54f : .13f);
            for (int row = 0; row < Logs.Length; row++) {
                int index = (row + clock / 24 + side * 3) % Logs.Length;
                float fade = row == 0 ? .22f : .13f;
                batch.DrawString(font, Logs[index], new Vector2(x, baseY + row * lineHeight * 1.16f), Code * (strength * fade),
                    0, Vector2.Zero, fontScale * .92f, SpriteEffects.None, 0);
            }
        }
    }
    private static void DrawPackets(SpriteBatch batch, float width, float height, int clock, int seed, float unit, float strength)
    {
        const int slots = 14;
        for (int slot = 0; slot < slots; slot++) {
            int period = 29 + (int)(Hash(seed + slot * 67) % 31);
            int localClock = clock + (int)(Hash(slot * 313 + seed) % (uint)period);
            int cycle = localClock / period, age = localClock % period;
            int key = unchecked(seed + slot * 719 + cycle * 13007);
            int lifetime = 8 + (int)(Hash(key + 17) % 14);
            float opacity = Pulse(age, lifetime) * strength;
            if (opacity <= 0) continue;
            int edge = slot % 4;
            float w = (edge < 2 ? 110 + Random(key + 3) * 300 : 42 + Random(key + 3) * 95) * unit;
            float h = (edge < 2 ? 12 + Random(key + 5) * 42 : 38 + Random(key + 5) * 100) * unit;
            w = Math.Min(w, width * .48f); h = Math.Min(h, height * .22f);
            float x, y;
            if (edge < 2) {
                x = Random(key + 11) * Math.Max(1, width - w);
                y = edge == 0 ? height * (.045f + Random(key + 23) * .12f) : height * (.84f + Random(key + 23) * .11f) - h;
            }
            else {
                x = edge == 2 ? width * (.012f + Random(key + 23) * .065f) : width * (.985f - Random(key + 23) * .065f) - w;
                y = height * .18f + Random(key + 11) * Math.Max(1, height * .58f - h);
            }
            // Quantized anchors and right-angle routes make a packet internally regular;
            // the next short burst has a new location, size and missing segments.
            x = MathF.Floor(x / (4 * unit)) * 4 * unit; y = MathF.Floor(y / (4 * unit)) * 4 * unit;
            DrawPacket(batch, new Vector2(x, y), new Vector2(w, h), key, unit, opacity);
        }
    }
    private static void DrawPacket(SpriteBatch batch, Vector2 origin, Vector2 size, int seed, float unit, float opacity)
    {
        float cut = Math.Min(9 * unit, Math.Min(size.X, size.Y) * .24f);
        Color color = Trace * (opacity * .32f);
        void Line(Vector2 a, Vector2 b, float alpha = 1) => HarbingerVisuals.Line(batch, origin + a, origin + b, color * alpha, .9f * unit);
        Span<Vector2> corner = stackalloc Vector2[8] {
            new(cut, 0), new(size.X - cut, 0), new(size.X, cut), new(size.X, size.Y - cut),
            new(size.X - cut, size.Y), new(cut, size.Y), new(0, size.Y - cut), new(0, cut)
        };
        uint gaps = Hash(seed + 61);
        for (int edge = 0; edge < 8; edge++) if ((gaps & (1u << edge)) != 0 || edge % 2 == 0)
            Line(corner[edge], corner[(edge + 1) % 8], .7f);
        int tracks = 2 + (int)(Hash(seed + 41) % 5);
        for (int i = 0; i < tracks; i++) {
            float y = Math.Min(size.Y - unit, (i + 1) * 2.6f * unit);
            float start = Random(seed + 43 + i * 19) * size.X * .22f;
            float turn = size.X * (.30f + Random(seed + i * 97) * .40f);
            float end = size.X * (.82f + Random(seed + 13 + i * 7) * .18f);
            float step = Math.Min(3 * unit, Math.Max(unit, size.Y - y));
            Line(new(start, y), new(turn - step, y));
            Line(new(turn - step, y), new(turn, y + step));
            Line(new(turn, y + step), new(end, y + step));
        }
        // A small empty node and disconnected contact pads, rather than solid screen blocks.
        float node = 3 * unit, nx = size.X * .76f, ny = size.Y * .55f;
        Line(new(nx, ny), new(nx + node, ny)); Line(new(nx + node, ny), new(nx + node, ny + node));
        Line(new(nx + node, ny + node), new(nx, ny + node)); Line(new(nx, ny + node), new(nx, ny));
        for (int i = 0; i < 3; i++) Line(new(size.X - (8 + i * 4) * unit, size.Y + 3 * unit),
            new(size.X - (6 + i * 4) * unit, size.Y + 3 * unit), .65f);
    }
}
