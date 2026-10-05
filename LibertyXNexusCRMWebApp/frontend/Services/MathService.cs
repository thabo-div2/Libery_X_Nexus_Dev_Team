using System.Text.RegularExpressions;

public class MathService 
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
            var increaseMatch = Regex.Match(
                text,
                @"increase\s+([\d,.]+)\s+by\s+([\d,.]+)\s*%"
            );

            if (increaseMatch.Success)
            {
                var baseValue = decimal.Parse(
                    increaseMatch.Groups[1].Value.Replace(",", "")
                );

                var percentage = decimal.Parse(
                    increaseMatch.Groups[2].Value.Replace(",", "")
                );

                var increaseAmount = baseValue * (percentage / 100m);
                var result = baseValue + increaseAmount;

                return $"Increasing {baseValue:N2} by {percentage:N2}% gives {result:N2}.";
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
            var monthlyMatch = Regex.Match(
                text,
                @"([\d,.]+)\s*per\s*month\s*for\s*([\d,.]+)\s*years?"
            );

            if (monthlyMatch.Success)
            {
                var monthly = decimal.Parse(
                    monthlyMatch.Groups[1].Value.Replace(",", "")
                );

                var years = decimal.Parse(
                    monthlyMatch.Groups[2].Value.Replace(",", "")
                );

                var total = monthly * 12m * years;

                return $"Total contribution over {years:N0} years: {total:N2}.";
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