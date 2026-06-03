using MudKeyboard.Internal;

namespace MudKeyboard.Tests;

public class PricepadFormatterTests
{
    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("123", "123")]
    [InlineData("£1.23", "123")]
    [InlineData("$1,234.50", "123450")]
    [InlineData("abc", "")]
    public void ExtractDigits_KeepsOnlyAsciiDigits(string? input, string expected) =>
        Assert.Equal(expected, PricepadFormatter.ExtractDigits(input));

    // Pence-first entry: the last `decimalPlaces` digits are the fraction.
    [Theory]
    [InlineData(null, "£0.00")]
    [InlineData("", "£0.00")]
    [InlineData("5", "£0.05")]
    [InlineData("52", "£0.52")]
    [InlineData("523", "£5.23")]
    [InlineData("100", "£1.00")]
    [InlineData("123456", "£1234.56")]
    public void Format_TwoDecimalPlaces_PenceFirst(string? digits, string expected) =>
        Assert.Equal(expected, PricepadFormatter.Format(digits, "£", 2));

    [Fact]
    public void Format_TrimsLeadingZerosFromIntegerPart() =>
        Assert.Equal("£5.00", PricepadFormatter.Format("0500", "£", 2));

    [Fact]
    public void Format_HonoursCustomCurrencySymbol() =>
        Assert.Equal("$5.23", PricepadFormatter.Format("523", "$", 2));

    [Fact]
    public void Format_NullCurrencySymbol_OmitsPrefix() =>
        Assert.Equal("5.23", PricepadFormatter.Format("523", null, 2));

    [Theory]
    [InlineData("5", "£5")]
    [InlineData("523", "£523")]
    [InlineData("0050", "£50")]
    [InlineData("", "£0")]
    public void Format_ZeroDecimalPlaces_HasNoPoint(string digits, string expected) =>
        Assert.Equal(expected, PricepadFormatter.Format(digits, "£", 0));

    [Fact]
    public void Format_NegativeDecimalPlaces_ClampedToZero() =>
        Assert.Equal("£523", PricepadFormatter.Format("523", "£", -1));

    [Theory]
    [InlineData("1234")]
    [InlineData("0")]
    [InlineData("9")]
    public void Format_IgnoresNonDigitsInInput(string digits) =>
        Assert.Equal(
            PricepadFormatter.Format(digits, "£", 2),
            PricepadFormatter.Format("x" + digits + "!", "£", 2));

    // Formatting then re-extracting the digits round-trips for a normalised run (at least one
    // integer digit, no leading zeros). Short runs are zero-padded ("5" → "£0.05"), so those do not.
    [Theory]
    [InlineData("523")]
    [InlineData("100")]
    [InlineData("1234")]
    public void Format_ThenExtractDigits_RoundTrips(string digits)
    {
        var formatted = PricepadFormatter.Format(digits, "£", 2);

        Assert.Equal(digits, PricepadFormatter.ExtractDigits(formatted));
    }
}
