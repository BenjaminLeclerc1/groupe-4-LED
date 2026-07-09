using System;
using System.Collections.Generic;
using UnityEngine;

// Minimal 3x5 pixel bitmap font - just enough to render short prompts (e.g.
// "PRESS SPACE") on a low-resolution LED wall, where no readable font exists
// yet. Add more glyphs to Glyphs as new text is needed.
public static class PixelFont
{
    public const int GlyphWidth = 3;
    public const int GlyphHeight = 5;
    private const int Spacing = 1;

    private static readonly Dictionary<char, string[]> Glyphs = new()
    {
        ['A'] = new[] { ".#.", "#.#", "###", "#.#", "#.#" },
        ['C'] = new[] { ".##", "#..", "#..", "#..", ".##" },
        ['E'] = new[] { "###", "#..", "##.", "#..", "###" },
        ['G'] = new[] { ".##", "#..", "#.#", "#.#", ".##" },
        ['M'] = new[] { "#.#", "###", "#.#", "#.#", "#.#" },
        ['O'] = new[] { ".#.", "#.#", "#.#", "#.#", ".#." },
        ['P'] = new[] { "##.", "#.#", "##.", "#..", "#.." },
        ['R'] = new[] { "##.", "#.#", "##.", "#.#", "#.#" },
        ['S'] = new[] { ".##", "#..", ".#.", "..#", "##." },
        ['V'] = new[] { "#.#", "#.#", "#.#", "#.#", ".#." },
        [' '] = new[] { "...", "...", "...", "...", "..." },
        ['0'] = new[] { "###", "#.#", "#.#", "#.#", "###" },
        ['1'] = new[] { ".#.", "##.", ".#.", ".#.", "###" },
        ['2'] = new[] { "###", "..#", "###", "#..", "###" },
        ['3'] = new[] { "###", "..#", "###", "..#", "###" },
        ['4'] = new[] { "#.#", "#.#", "###", "..#", "..#" },
        ['5'] = new[] { "###", "#..", "###", "..#", "###" },
        ['6'] = new[] { "###", "#..", "###", "#.#", "###" },
        ['7'] = new[] { "###", "..#", "..#", "..#", "..#" },
        ['8'] = new[] { "###", "#.#", "###", "#.#", "###" },
        ['9'] = new[] { "###", "#.#", "###", "..#", "###" },
    };

    public static int MeasureWidth(string text, int scale = 1)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        return (text.Length * (GlyphWidth + Spacing) - Spacing) * scale;
    }

    // originX/originY is the bottom-left corner of the text, matching
    // LEDWallBuffer's row-0-at-bottom convention. Unknown characters are
    // skipped (just leave a gap).
    public static void Draw(string text, int originX, int originY, Color color, int scale, Action<int, int, Color> setPixel)
    {
        int cursorX = originX;

        foreach (char rawChar in text)
        {
            char c = char.ToUpperInvariant(rawChar);
            if (!Glyphs.TryGetValue(c, out string[] rows))
            {
                cursorX += (GlyphWidth + Spacing) * scale;
                continue;
            }

            for (int row = 0; row < GlyphHeight; row++)
            {
                // rows[0] is the glyph's top row; buffer row 0 is the bottom
                // of the screen, so flip while drawing.
                int y = originY + (GlyphHeight - 1 - row) * scale;
                string rowPattern = rows[row];

                for (int col = 0; col < GlyphWidth; col++)
                {
                    if (rowPattern[col] != '#')
                    {
                        continue;
                    }

                    for (int sy = 0; sy < scale; sy++)
                    for (int sx = 0; sx < scale; sx++)
                        setPixel(cursorX + col * scale + sx, y + sy, color);
                }
            }

            cursorX += (GlyphWidth + Spacing) * scale;
        }
    }
}
