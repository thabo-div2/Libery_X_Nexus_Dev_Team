using API.Services.Interfaces;
using System.Text.RegularExpressions;

public class MathService : IMathService
{
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
            var percentMatch = Regex.Match(text, @"(\d+)%\s*of\s*(\d+)");
            if (percentMatch.Success)
            {
                var percent = decimal.Parse(percentMatch.Groups[1].Value);
                var value = decimal.Parse(percentMatch.Groups[2].Value);

                var result = (percent / 100m) * value;
                return $"That is {result:N2}.";
            }

            // 2. Increase: "increase 1200 by 8%"
            var increaseMatch = Regex.Match(text, @"(\d+).*(increase).*(\d+)%");
            if (increaseMatch.Success)
            {
                var baseVal = decimal.Parse(increaseMatch.Groups[1].Value);
                var percent = decimal.Parse(increaseMatch.Groups[3].Value);

                var result = baseVal * (1 + percent / 100m);
                return $"After increase: {result:N2}.";
            }

            // 3. Difference: "difference between 4200000 and 3700000"
            var diffMatch = Regex.Match(text, @"difference.*?(\d+).*?(\d+)");
            if (diffMatch.Success)
            {
                var a = decimal.Parse(diffMatch.Groups[1].Value);
                var b = decimal.Parse(diffMatch.Groups[2].Value);

                var result = Math.Abs(a - b);
                return $"The difference is {result:N2}.";
            }

            // 4. Monthly savings: "2000 per month for 10 years"
            var monthlyMatch = Regex.Match(text, @"(\d+).*per month.*(\d+).*year");
            if (monthlyMatch.Success)
            {
                var monthly = decimal.Parse(monthlyMatch.Groups[1].Value);
                var years = decimal.Parse(monthlyMatch.Groups[2].Value);

                var total = monthly * 12 * years;
                return $"Total contribution: {total:N2}.";
            }

            // fallback
            return "I couldn't fully understand the calculation. Try something like '15% of 500000' or '2000 per month for 10 years'.";
        }
        catch
        {
            return "Something went wrong while calculating. Please rephrase your question.";
        }
    }
}