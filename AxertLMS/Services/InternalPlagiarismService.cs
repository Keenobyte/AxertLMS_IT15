using System;
using System.Collections.Generic;
using System.Linq;

namespace AxertLMS.Services
{
    public class InternalPlagiarismService
    {
        public double CheckSimilarity(string submissionText, string sourceText)
        {
            if (string.IsNullOrEmpty(submissionText) || string.IsNullOrEmpty(sourceText))
                return 0.0;

            string cleanSub = CleanText(submissionText);
            string cleanSrc = CleanText(sourceText);

            var subTrigrams = GetTrigrams(cleanSub);
            var srcTrigrams = GetTrigrams(cleanSrc);

            if (subTrigrams.Count == 0 || srcTrigrams.Count == 0)
                return 0.0;

            int matches = subTrigrams.Intersect(srcTrigrams).Count();
            double similarity = (2.0 * matches) / (subTrigrams.Count + srcTrigrams.Count) * 100;

            return Math.Round(similarity, 2);
        }

        private string CleanText(string text)
        {
            return new string(text.ToLower()
                .Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c))
                .ToArray());
        }

        private List<string> GetTrigrams(string text)
        {
            var words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var trigrams = new List<string>();

            for (int i = 0; i < words.Length - 2; i++)
            {
                trigrams.Add($"{words[i]} {words[i + 1]} {words[i + 2]}");
            }

            return trigrams;
        }
    }
}