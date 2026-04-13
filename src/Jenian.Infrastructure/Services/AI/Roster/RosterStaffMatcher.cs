using System.Text.RegularExpressions;

namespace Jenian.Infrastructure.Services.AI.Roster
{
  // This class is responsible for matching a given staff name to the most likely row of OCR tokens from a roster image.
  // It uses a combination of heuristics to filter out non-name tokens, normalize text, and score potential matches.
  // The main method is FindBestStaffRow, which takes a list of OCR tokens and a target staff name, and returns the best matching row of tokens if a good match is found.
  // The matching process includes:
  // 1. Normalizing the target staff name (uppercasing, removing punctuation, collapsing whitespace).
  // 2. Filtering OCR tokens to find candidates that are likely to be person names (not noise, not too far right, contain letters, no digits).
  // 3. Scoring each candidate token against the normalized target name using a combination of exact match, substring match, and word overlap.
  // 4. Selecting the best scoring candidate and checking if it meets a minimum score threshold.
  // 5. If a good match is found, collecting all tokens that are on the same horizontal line (within a Y-center threshold) to return as the matched row.
  // The NoiseLabels set contains common non-name tokens that are often found in roster OCR outputs and should be ignored when looking for staff names.
  // The scoring function is designed to give a high score for exact matches, a slightly lower score for substring matches, and then a score based on word overlap for more partial matches.
  public class RosterStaffMatcher
  {
    private static readonly HashSet<string> NoiseLabels = new(StringComparer.OrdinalIgnoreCase) {
      "MIDTOWN WEEK",
      "MIDTOWN WEEK 1",
      "MIDTOWN WEEK 2",
      "DISPENSARY",
      "FULL-TIME",
      "PART-TIME",
      "CASUAL",
      "OFF-SITE EMPLOYEES",
      "EMPLOYEES NUMBER",
      "MON",
      "TUE",
      "WED",
      "THU",
      "THUR",
      "FRI",
      "SAT",
      "SUN"
    };

    public static StaffRowMatch? FindBestStaffRow(List<OcrRosterToken> tokens, string staffName) {
      if (string.IsNullOrWhiteSpace(staffName))
        return null;

      var normalizedTarget = NormalizeName(staffName);

      var candidateNameTokens = tokens
        .Where(IsLikelyPersonNameToken)
        .ToList();

      if (candidateNameTokens.Count == 0)
        return null;

      var ranked = candidateNameTokens
        .Select(t => new {
          Token = t,
          Score = ScoreNameMatch(normalizedTarget, NormalizeName(t.Text))
        })
        .OrderByDescending(x => x.Score)
        .ToList();

      var best = ranked.FirstOrDefault();

      if (best is null || best.Score < 0.45)
        return null;

      var rowTokens = tokens
        .Where(t => Math.Abs(t.YCenter - best.Token.YCenter) <= 12)
        .OrderBy(t => t.XCenter)
        .ToList();

      return new StaffRowMatch {
        NameToken = best.Token,
        RowTokens = rowTokens
      };
    }

    private static bool IsLikelyPersonNameToken(OcrRosterToken token) {
      var text = token.Text.Trim();

      if (string.IsNullOrWhiteSpace(text))
        return false;

      if (NoiseLabels.Contains(text))
        return false;

      if (token.XCenter > 260)
        return false;

      if (!text.Any(char.IsLetter))
        return false;

      if (Regex.IsMatch(text, @"\d"))
        return false;

      return true;
    }

    private static string NormalizeName(string value) {
      var upper = value.ToUpperInvariant();
      upper = Regex.Replace(upper, @"[^\p{L}\p{N}\s]", " ");
      upper = Regex.Replace(upper, @"\s+", " ").Trim();

      return upper;
    }

    private static double ScoreNameMatch(string target, string candidate) {
      if (string.IsNullOrWhiteSpace(target) || string.IsNullOrWhiteSpace(candidate))
        return 0.0;

      if (string.Equals(target, candidate, StringComparison.OrdinalIgnoreCase))
        return 1.0;

      if (candidate.Contains(target, StringComparison.OrdinalIgnoreCase) ||
          target.Contains(candidate, StringComparison.OrdinalIgnoreCase)) {
        return 0.9;
      }

      var targetWords = target.Split(' ', StringSplitOptions.RemoveEmptyEntries);
      var candidateWords = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);

      var commonWordCount = targetWords
        .Intersect(candidateWords, StringComparer.OrdinalIgnoreCase)
        .Count();

      var maxWordCount = Math.Max(targetWords.Length, candidateWords.Length);
      var overlapScore = maxWordCount == 0 ? 0.0 : (double)commonWordCount / maxWordCount;

      if (targetWords.Length > 0 &&
          candidateWords.Length > 0 &&
          string.Equals(targetWords[0], candidateWords[0], StringComparison.OrdinalIgnoreCase)) {
        overlapScore += 0.1;
      }

      return Math.Min(overlapScore, 1.0);

    }
  }
}
