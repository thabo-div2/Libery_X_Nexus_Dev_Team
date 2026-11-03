using API.Services.Interfaces;
using System.Globalization;
using System.Text.RegularExpressions;

public class MathService : IMathService
{
    private const string Num = @"(\d+(?:\.\d+)?)";

    private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);
    private static string F(decimal value) => value.ToString("N2", CultureInfo.InvariantCulture);
    public bool LooksLikeMathQuestion(string input)
    {
        var text = input.ToLower();

        return Regex.IsMatch(text, @"\d") && (
               text.Contains("%") ||
               text.Contains("percent") ||
               text.Contains("increase") ||
               text.Contains("decrease") ||
               text.Contains("total") ||
               text.Contains("difference") ||
               text.Contains("per month") ||
               text.Contains("years") ||
               text.Contains("months"));
    }

    public string Solve(string input)
    {
        var text = input.ToLower();

        try
        {
            // 1. Percentage: "15% of 500000"
            var percentMatch = Regex.Match(text, Num + @"\s*%\s*of\s*" + Num, RegexOptions.None, TimeSpan.FromSeconds(1));
            if (percentMatch.Success)
            {
                var percent = D(percentMatch.Groups[1].Value);
                var value = D(percentMatch.Groups[2].Value);

                var result = (percent / 100m) * value;
                return $"That is {F(result)}.";
            }

            // 2. Increase: "increase 1200 by 8%" or "1200 increase by 18%"
            var increaseMatch = Regex.Match(text, Num + @"\s*(?:increase|increased)\D*?" + Num + @"\s*%", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (!increaseMatch.Success)
            {
                increaseMatch = Regex.Match(text, @"increase\D*?" + Num + @"\D*?" + Num + @"\s*%", RegexOptions.None, TimeSpan.FromSeconds(1));
            }
            if (increaseMatch.Success)
            {
                var baseVal = D(increaseMatch.Groups[1].Value);
                var percent = D(increaseMatch.Groups[2].Value);

                var result = baseVal * (1 + percent / 100m);
                return $"After increase: {F(result)}.";
            }

            // 3. Difference: "difference between 4200000 and 3700000"
            var diffMatch = Regex.Match(text, @"difference\D*?" + Num + @"\D+?" + Num, RegexOptions.None, TimeSpan.FromSeconds(1));
            if (diffMatch.Success)
            {
                var a = D(diffMatch.Groups[1].Value);
                var b = D(diffMatch.Groups[2].Value);

                var result = Math.Abs(a - b);
                return $"The difference is {F(result)}.";
            }

            // 4. Monthly savings: "2000 per month for 10 years"
            var monthlyMatch = Regex.Match(text, Num + @"\D*?per month\D*?" + Num + @"\s*year", RegexOptions.None, TimeSpan.FromSeconds(1));
            if (monthlyMatch.Success)
            {
                var monthly = D(monthlyMatch.Groups[1].Value);
                var years = D(monthlyMatch.Groups[2].Value);

                var total = monthly * 12 * years;
                return $"Total contribution: {F(total)}.";
            }

            return "I couldn't fully understand the calculation. Try something like '15% of 500000' or '2000 per month for 10 years'.";
        }
        catch
        {
            return "Something went wrong while calculating. Please rephrase your question.";
        }
    }
}