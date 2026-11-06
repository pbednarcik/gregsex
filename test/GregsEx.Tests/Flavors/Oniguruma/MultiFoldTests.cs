using GregsEx.Flavors.Oniguruma;
using Xunit;

namespace GregsEx.Tests.Flavors.Oniguruma;

// The parser looks up folds to several code points once per ignore-case character: by the code point a fold starts
// with, and by a code point that folds to several. Both lookups must give what a scan of the whole table gives, in
// table order, for every BMP code point, and for each code point the table names moved into every supplementary plane
// (a lookup that dropped the high bits would find them). Entries are compared by content: concurrent first uses may
// each build their own equal copy of the table.
public sealed class MultiFoldTests
{
    [Fact]
    public void LookupsByFirstCodePointAndByMemberAgreeWithTheTable()
    {
        var table = OnigUnicode.MultiFolds;
        var points = new SortedSet<int>(Enumerable.Range(0, 0x10000)) { 0x10FFFF };
        foreach (var (fold, members) in table)
        {
            foreach (var cp in fold.Concat(members))
            {
                for (var plane = 1; plane <= 16; plane++)
                {
                    points.Add(cp | (plane << 16));
                }
            }
        }

        foreach (var cp in points)
        {
            var starting = table.Where(e => e.Fold[0] == cp).Select(Spell).ToArray();
            Assert.Equal(starting, OnigUnicode.MultiFoldsStartingWith(cp).ToArray().Select(Spell));

            var holding = table.Where(e => Array.IndexOf(e.Members, cp) >= 0).Select(Spell).FirstOrDefault();
            var found = OnigUnicode.MultiFoldOf(cp);
            Assert.Equal(holding, found is { } entry ? Spell(entry) : null);
        }
    }

    // A run spells a fold through the folds of its characters, so an upper-case run matches too; a character with a
    // fold of its own also matches what it folds to.
    [Theory]
    [InlineData("(?i)\\ASS\\z", "ß", true)]                     // ß
    [InlineData("(?i)\\AFFI\\z", "ﬃ", true)]                    // ﬃ, the longest fold ffi wins over ff and fi
    [InlineData("(?i)\\AStRaSSe\\z", "straße", true)]
    [InlineData("(?i)\\Aﬃ\\z", "FFI", true)]
    [InlineData("(?i)\\Aǰ\\z", "J̌", true)]                      // ǰ folds to j + combining caron
    [InlineData("(?i)\\Aselect\\z", "SeLeCt", true)]
    [InlineData("(?i)\\A\U0001FB00\\z", "ff", false)]           // U+1FB00 has no fold; its low 16 bits are ﬀ's
    [InlineData("(?i)\\A\U0001FB00\\z", "\U0001FB00", true)]
    public void IgnoreCaseRunsMatchThroughFoldsToSeveralCodePoints(string pattern, string input, bool matches)
    {
        var workspace = OnigurumaPattern.Parse(pattern, RegexProfiles.OnigurumaTextMate, OnigurumaCompileOptions.TextMate).CreateWorkspace();
        Assert.Equal(matches ? RegexSearchStatus.Match : RegexSearchStatus.NoMatch, workspace.IsMatch(input));
    }

    private static string Spell((int[] Fold, int[] Members) entry)
        => string.Join(",", entry.Fold) + ":" + string.Join(",", entry.Members);
}
