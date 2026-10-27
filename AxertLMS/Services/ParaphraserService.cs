using System.Text.RegularExpressions;

namespace AxertLMS.Services
{
    public class ParaphraserService
    {
        private static readonly Dictionary<string, string[]> Synonyms =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["important"] = new[] { "significant", "essential", "notable" },
                ["shows"] = new[] { "demonstrates", "illustrates", "indicates" },
                ["show"] = new[] { "demonstrate", "illustrate", "indicate" },
                ["use"] = new[] { "utilize", "apply", "employ" },
                ["uses"] = new[] { "utilizes", "applies", "employs" },
                ["help"] = new[] { "assist", "support", "aid" },
                ["helps"] = new[] { "assists", "supports", "aids" },
                ["make"] = new[] { "create", "produce", "develop" },
                ["makes"] = new[] { "creates", "produces", "develops" },
                ["get"] = new[] { "obtain", "receive", "acquire" },
                ["gets"] = new[] { "obtains", "receives", "acquires" },
                ["explain"] = new[] { "describe", "clarify", "illustrate" },
                ["explains"] = new[] { "describes", "clarifies", "illustrates" },
                ["good"] = new[] { "effective", "beneficial", "valuable" },
                ["bad"] = new[] { "poor", "unfavorable", "inadequate" },
                ["big"] = new[] { "large", "substantial", "considerable" },
                ["small"] = new[] { "limited", "minor", "compact" },
                ["many"] = new[] { "numerous", "several", "multiple" },
                ["different"] = new[] { "distinct", "various", "alternative" },
                ["because"] = new[] { "since", "as", "given that" },
                ["also"] = new[] { "additionally", "furthermore", "likewise" },
                ["however"] = new[] { "nevertheless", "nonetheless", "still" },
                ["therefore"] = new[] { "consequently", "thus", "as a result" },
                ["need"] = new[] { "require", "necessitate", "call for" },
                ["needs"] = new[] { "requires", "necessitates", "calls for" },
                ["learn"] = new[] { "study", "understand", "explore" },
                ["learning"] = new[] { "studying", "understanding", "exploration" },
                ["improve"] = new[] { "enhance", "strengthen", "refine" },
                ["improves"] = new[] { "enhances", "strengthens", "refines" },
                ["problem"] = new[] { "issue", "challenge", "difficulty" },
                ["answer"] = new[] { "response", "solution", "reply" },
                ["idea"] = new[] { "concept", "notion", "principle" },
                ["method"] = new[] { "approach", "technique", "procedure" },
                ["result"] = new[] { "outcome", "finding", "consequence" },
                ["start"] = new[] { "begin", "commence", "initiate" },
                ["ends"] = new[] { "concludes", "finishes", "terminates" },
                ["clear"] = new[] { "understandable", "evident", "explicit" },
                ["accurate"] = new[] { "precise", "correct", "reliable" }
            };

        public string Paraphrase(string text, string style)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            string result = text.Trim();

            string normalizedStyle = style?.Trim().ToLowerInvariant() ?? "standard";

            if (normalizedStyle == "simpler")
            {
                result = Regex.Replace(
                    result,
                    @"\b(utilize|utilizes|utilized)\b",
                    match => MatchCase(match.Value, "use"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\b(approximately)\b",
                    match => MatchCase(match.Value, "about"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\b(demonstrate|demonstrates|demonstrated)\b",
                    match => MatchCase(match.Value, "show"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\b(commence|commences|commenced)\b",
                    match => MatchCase(match.Value, "start"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\b(terminate|terminates|terminated)\b",
                    match => MatchCase(match.Value, "end"),
                    RegexOptions.IgnoreCase);
            }
            else
            {
                result = Regex.Replace(
                    result,
                    @"\bvery\b",
                    match => MatchCase(match.Value, "particularly"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\ba lot of\b",
                    match => MatchCase(match.Value, "numerous"),
                    RegexOptions.IgnoreCase);
            }

            if (normalizedStyle == "formal")
            {
                result = Regex.Replace(
                    result,
                    @"\bcan't\b",
                    match => MatchCase(match.Value, "cannot"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\bwon't\b",
                    match => MatchCase(match.Value, "will not"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\bdon't\b",
                    match => MatchCase(match.Value, "do not"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\bdoesn't\b",
                    match => MatchCase(match.Value, "does not"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\bisn't\b",
                    match => MatchCase(match.Value, "is not"),
                    RegexOptions.IgnoreCase);

                result = Regex.Replace(
                    result,
                    @"\baren't\b",
                    match => MatchCase(match.Value, "are not"),
                    RegexOptions.IgnoreCase);
            }

            var usedWords = new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

            result = Regex.Replace(
                result,
                @"\b[\p{L}]+\b",
                match =>
                {
                    string word = match.Value;

                    if (!Synonyms.TryGetValue(word, out string[]? alternatives))
                    {
                        return word;
                    }

                    string? replacement = alternatives.FirstOrDefault(
                        candidate => !usedWords.Contains(candidate));

                    if (replacement == null)
                    {
                        return word;
                    }

                    usedWords.Add(replacement);

                    return MatchCase(word, replacement);
                });

            return result;
        }

        private static string MatchCase(string original, string replacement)
        {
            if (string.IsNullOrEmpty(original) ||
                string.IsNullOrEmpty(replacement))
            {
                return replacement;
            }

            if (original.All(char.IsUpper))
            {
                return replacement.ToUpperInvariant();
            }

            if (char.IsUpper(original[0]))
            {
                return char.ToUpperInvariant(replacement[0]) +
                       replacement.Substring(1);
            }

            return replacement;
        }
    }
}