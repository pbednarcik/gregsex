using BenchmarkDotNet.Attributes;

namespace GregsEx.Benchmarks.Oniguruma;

// Parsing an ignore-case keyword alternation, the shape of the large keyword rules in TextMate grammars (SQL, CSS,
// PowerShell): every ignore-case character is checked for folds to several code points (ss, ff, fi...). The words are
// generated from syllables, most of which hold an s, f, i or t that such a fold can start with.
[MemoryDiagnoser]
[ShortRunJob]
public class OnigurumaIgnoreCaseParse
{
    private static readonly string[] Syllables = ["sel", "ect", "ins", "ert", "up", "da", "te", "fi", "le", "st", "at", "us", "off", "set", "ra", "no"];

    private string _keywords = null!;

    [Params(200, 800)]
    public int Words { get; set; }

    [GlobalSetup]
    public void Prepare()
    {
        var words = new List<string>(Words);
        for (var i = 0; i < Words; i++)
        {
            words.Add(Syllables[i % 16] + Syllables[(i / 16) % 16] + Syllables[(i / 256) % 16] + "_" + (i % 7));
        }

        _keywords = @"\b(?i)(" + string.Join("|", words) + @")\b";
    }

    [Benchmark]
    public OnigurumaPattern Keywords() => OnigurumaPattern.Parse(_keywords, RegexProfiles.OnigurumaTextMate, OnigurumaCompileOptions.TextMate);
}
