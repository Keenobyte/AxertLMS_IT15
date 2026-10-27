using AxertLMS.Models;
using System.Text.RegularExpressions;

namespace AxertLMS.Services
{
    public class InternalQuizGeneratorService
    {
        private static readonly HashSet<string> StopWords =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "about", "after", "again", "against", "also", "among",
                "because", "before", "being", "between", "both", "could",
                "does", "each", "from", "have", "having", "into", "just",
                "more", "most", "other", "over", "same", "should", "some",
                "such", "than", "that", "their", "them", "then", "there",
                "these", "they", "this", "those", "through", "under",
                "until", "very", "were", "what", "when", "where", "which",
                "while", "will", "with", "would", "your", "lesson", "module",
                "chapter", "topic", "topics", "course", "student", "students"
            };

        public List<QuizQuestionDto> Generate(
            string moduleTitle,
            string moduleDescription,
            string difficulty = "Easy")
        {
            moduleTitle = string.IsNullOrWhiteSpace(moduleTitle)
                ? "Untitled Module"
                : moduleTitle.Trim();

            moduleDescription ??= string.Empty;

            string source = $"{moduleTitle}. {moduleDescription}";

            List<string> keywords = Regex
                .Matches(source, @"\b[\p{L}][\p{L}\p{N}-]{2,}\b")
                .Select(match => match.Value.Trim('-', ' '))
                .Where(word => word.Length >= 3)
                .Where(word => !StopWords.Contains(word))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(12)
                .ToList();

            if (keywords.Count == 0)
            {
                keywords.Add(moduleTitle);
            }

            if (keywords.Count == 1)
            {
                keywords.Add("main concept");
            }

            var questions = new List<QuizQuestionDto>();

            // Identification questions.
            for (int i = 0; i < Math.Min(2, keywords.Count); i++)
            {
                questions.Add(new QuizQuestionDto
                {
                    question_type = "Identification",
                    question_text =
                        $"Identify the key term associated with this module: {moduleTitle}.",
                    correct_answer = keywords[i]
                });
            }

            // True/False questions.
            questions.Add(new QuizQuestionDto
            {
                question_type = "True False",
                question_text =
                    $"The term '{keywords[0]}' appears among the identified key terms for '{moduleTitle}'.",
                correct_answer = "True"
            });

            questions.Add(new QuizQuestionDto
            {
                question_type = "True False",
                question_text =
                    $"The module title is '{moduleTitle}'.",
                correct_answer = "True"
            });

            questions.Add(new QuizQuestionDto
            {
                question_type = "True False",
                question_text =
                    $"The identified key term '{keywords[0]}' is absent from the terms selected for this quiz.",
                correct_answer = "False"
            });

            // Multiple-choice questions with distinct options.
            for (int i = 0; i < 5; i++)
            {
                string answer = keywords[i % keywords.Count];

                var distractors = keywords
                    .Where(word =>
                        !string.Equals(
                            word,
                            answer,
                            StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

                string[] fallbackOptions =
                {
                    "General concept",
                    "Supporting detail",
                    "Related example"
                };

                int fallbackIndex = 0;

                while (distractors.Count < 3)
                {
                    string fallback = fallbackOptions[
                        fallbackIndex % fallbackOptions.Length];

                    fallbackIndex++;

                    if (!string.Equals(
                        fallback,
                        answer,
                        StringComparison.OrdinalIgnoreCase)
                        && !distractors.Contains(
                            fallback,
                            StringComparer.OrdinalIgnoreCase))
                    {
                        distractors.Add(fallback);
                    }
                }

                var options = new List<string>
                {
                    answer,
                    distractors[0],
                    distractors[1],
                    distractors[2]
                };

                // Rotate the correct answer so it is not always option A.
                int correctIndex = i % 4;

                string temporary = options[0];
                options[0] = options[correctIndex];
                options[correctIndex] = temporary;

                questions.Add(new QuizQuestionDto
                {
                    question_type = "Multiple Choice",
                    question_text =
                        $"Which of the following is an identified key term for '{moduleTitle}'?",
                    option_a = options[0],
                    option_b = options[1],
                    option_c = options[2],
                    option_d = options[3],
                    correct_answer = answer
                });
            }

            return questions;
        }
    }

    public class QuizQuestionDto
    {
        public string question_type { get; set; } = string.Empty;

        public string question_text { get; set; } = string.Empty;

        public string? option_a { get; set; }

        public string? option_b { get; set; }

        public string? option_c { get; set; }

        public string? option_d { get; set; }

        public string correct_answer { get; set; } = string.Empty;
    }
}