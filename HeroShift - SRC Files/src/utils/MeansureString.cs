using CounterStrikeSharp.API;
using System.Text.RegularExpressions;

namespace src.player;

/*
 * MeansureString - measures how WIDE a piece of text will be, so the plugin can
 * centre things in chat and the HTML HUD. (The class name is a typo of "Measure"
 * that is kept because other files reference it.)
 *
 * The problem it solves: CS2's chat font is proportional, so "iii" and "WWW" are
 * nothing alike in width. Padding a line to a fixed character COUNT therefore
 * produces ragged output. There is no way to ask the engine how wide a string
 * renders, so the width of each glyph is hardcoded in the CharWidth table below and
 * a string's width is just the sum of its characters. The numbers are in the same
 * arbitrary units as the ChatMessage.MaxWidth config value - they are only ever
 * compared against each other, so the unit itself does not matter. Characters
 * missing from the table fall back to DefaultCharWidth (the width of a typical
 * lowercase letter), which is why unusual glyphs may still measure slightly off.
 *
 * The single public entry point is GetTextDashed(), used by SkillUtils.PrintToChat
 * to draw the decorated separator lines around plugin chat messages: it pads the
 * given content with a repeated symbol on both sides until the line reaches
 * targetWidth. The box-drawing and arrow glyphs at the end of the table (the dash,
 * and the triangles/diamond used in the message borders) are there for exactly that
 * purpose.
 *
 * Colour codes must not count towards the width, since they are control characters
 * that render as nothing - StripColors removes them before measuring.
 */
public static class MeansureString
{
    // Fallback for any character absent from the table below.
    static readonly float DefaultCharWidth = 22.25f;
    // Per-glyph widths for the CS2 chat font, in arbitrary units shared with
    // Config.ChatMessage.MaxWidth. Note how much narrower i/j/l/f/t are than m/w/W.
    static readonly Dictionary<char, float> CharWidth = new()
    {
        ['0'] = 22.25f,
        ['1'] = 22.25f,
        ['2'] = 22.25f,
        ['3'] = 22.25f,
        ['4'] = 22.25f,
        ['5'] = 22.25f,
        ['6'] = 22.25f,
        ['7'] = 22.25f,
        ['8'] = 22.25f,
        ['9'] = 22.25f,
        ['a'] = 22.25f,
        ['b'] = 22.25f,
        ['c'] = 20.00f,
        ['d'] = 22.25f,
        ['e'] = 22.25f,
        ['f'] = 11.11f,
        ['g'] = 22.25f,
        ['h'] = 22.25f,
        ['i'] = 8.89f,
        ['j'] = 8.89f,
        ['k'] = 20.00f,
        ['l'] = 8.89f,
        ['m'] = 33.33f,
        ['n'] = 22.25f,
        ['o'] = 22.25f,
        ['p'] = 22.25f,
        ['q'] = 22.25f,
        ['r'] = 13.33f,
        ['s'] = 20.00f,
        ['t'] = 11.11f,
        ['u'] = 22.25f,
        ['v'] = 20.00f,
        ['w'] = 28.89f,
        ['x'] = 20.00f,
        ['y'] = 20.00f,
        ['z'] = 20.00f,
        ['A'] = 26.69f,
        ['B'] = 26.69f,
        ['C'] = 28.89f,
        ['D'] = 28.89f,
        ['E'] = 26.69f,
        ['F'] = 24.44f,
        ['G'] = 31.11f,
        ['H'] = 28.89f,
        ['I'] = 11.11f,
        ['J'] = 20.00f,
        ['K'] = 26.69f,
        ['L'] = 22.25f,
        ['M'] = 33.33f,
        ['N'] = 28.89f,
        ['O'] = 31.11f,
        ['P'] = 26.69f,
        ['Q'] = 31.11f,
        ['R'] = 28.89f,
        ['S'] = 26.69f,
        ['T'] = 24.44f,
        ['U'] = 28.89f,
        ['V'] = 26.69f,
        ['W'] = 37.75f,
        ['X'] = 26.69f,
        ['Y'] = 26.69f,
        ['Z'] = 24.44f,
        ['\''] = 7.64f,
        ['!'] = 11.11f,
        ['@'] = 40.61f,
        ['#'] = 22.25f,
        ['$'] = 22.25f,
        ['%'] = 35.56f,
        ['^'] = 18.77f,
        ['&'] = 26.69f,
        ['*'] = 15.56f,
        ['('] = 13.33f,
        [')'] = 13.33f,
        ['_'] = 22.25f,
        ['-'] = 13.33f,
        ['+'] = 23.36f,
        ['='] = 23.36f,
        [','] = 11.11f,
        ['.'] = 11.11f,
        [';'] = 11.11f,
        [':'] = 11.11f,
        ['<'] = 23.36f,
        ['>'] = 23.36f,
        ['/'] = 11.11f,
        ['?'] = 22.25f,
        ['\\'] = 11.11f,
        ['|'] = 10.39f,
        ['`'] = 13.33f,
        ['~'] = 23.36f,
        ['"'] = 14.20f,
        [' '] = 11.11f,
        ['―'] = 40.00f,
        ['◢'] = 30.00f,
        ['◆'] = 30.00f,
        ['◤'] = 30.00f,
    };

    // Sum of the per-character widths. Assumes the string has already had colour codes
    // stripped, otherwise those control characters add phantom width.
    private static float GetWidth(string s)
    {
        float w = 0;
        foreach (char c in s)
            w += CharWidth.TryGetValue(c, out var cw) ? cw : DefaultCharWidth;
        return w;
    }

    // Removes CS2 chat colour codes. They are the control characters 0x01-0x10 (the same
    // values as the ChatColors constants) and occupy no visual width, so they must not be
    // measured.
    private static string StripColors(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return s;
        return Regex.Replace(s, @"[\x01-\x10]", string.Empty);
    }

    // Centres `content` on a line of `targetWidth` by padding both sides with `symbol`,
    // each run prefixed with `color`. Pass an empty content to get a plain full-width rule.
    //
    // The content is measured with its colour codes stripped, but the ORIGINAL content
    // (colours intact) is what gets returned - only the measurement ignores them.
    // Content already at or over the target width is returned unpadded rather than
    // truncated, so a long name overflows instead of being cut.
    public static string GetTextDashed(string content, float targetWidth, char symbol, string color)
    {
        string cleanContent = StripColors(content);
        float contentWidth = GetWidth(cleanContent);
        float dashWidth = CharWidth.TryGetValue(symbol, out var cw) ? cw : DefaultCharWidth;

        float remaining = targetWidth - contentWidth;

        if (remaining <= 0) return content;
        // Floor, so the line never exceeds targetWidth; the leftover under one symbol
        // width is simply not filled.
        int totalDashes = (int)Math.Floor(remaining / dashWidth);

        // Odd counts put the extra symbol on the right.
        int left = totalDashes / 2;
        int right = totalDashes - left;

        return color + new string(symbol, left) + content + color + new string(symbol, right);
    }
}