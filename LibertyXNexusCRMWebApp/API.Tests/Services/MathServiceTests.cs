using Xunit;

namespace API.Tests.Services
{
    /// <summary>
    /// Tests for the MathService calculator.
    /// </summary>
    public class MathServiceTests
    {
        private readonly MathService _sut = new();

        [Theory]
        [InlineData("15% of 500000", "That is 75,000.00.")]
        [InlineData("increase 1200 by 8%", "After increase: 1,296.00.")]
        [InlineData("1200 increase by 18%", "After increase: 1,416.00.")]
        [InlineData("2000 per month for 10 years", "Total contribution: 240,000.00.")]
        [InlineData("500 per month for 25 years", "Total contribution: 150,000.00.")]
        [InlineData("difference between 4200000 and 3700000", "The difference is 500,000.00.")]
        public void Solve_ReturnsTheCorrectAnswer(string input, string expected)
        {
            Assert.Equal(expected, _sut.Solve(input));
        }

        [Fact]
        public void Solve_UnknownInput_ReturnsHelpMessage()
        {
            Assert.StartsWith("I couldn't fully understand", _sut.Solve("hello there 12"));
        }
    }
}