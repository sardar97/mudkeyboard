using System.Numerics;
using MudKeyboard.Internal;
using MudKeyboard.Layouts;
using MudKeyboard.Services;

namespace MudKeyboard.Tests;

/// <summary>
/// The type → docked-keyboard layout mapping that lets <c>MudKeyboardNumericField&lt;T&gt;</c> pick the
/// right keypad from its bound CLR type: decimal → money, double/float → decimal keypad, integers →
/// plain numpad. Nullable numeric types resolve like their underlying type.
/// </summary>
public class NumericLayoutResolverTests
{
    [Fact]
    public void Decimal_MapsToMoney() =>
        Assert.Equal("money", NumericLayoutResolver.Resolve(typeof(decimal)));

    [Fact]
    public void NullableDecimal_MapsToMoney() =>
        Assert.Equal("money", NumericLayoutResolver.Resolve(typeof(decimal?)));

    [Theory]
    [InlineData(typeof(double))]
    [InlineData(typeof(float))]
    [InlineData(typeof(double?))]
    [InlineData(typeof(float?))]
    public void FloatingPoint_MapsToDecimalKeypad(Type type) =>
        Assert.Equal("decimal", NumericLayoutResolver.Resolve(type));

    [Theory]
    [InlineData(typeof(int))]
    [InlineData(typeof(long))]
    [InlineData(typeof(short))]
    [InlineData(typeof(byte))]
    [InlineData(typeof(uint))]
    [InlineData(typeof(ulong))]
    [InlineData(typeof(ushort))]
    [InlineData(typeof(sbyte))]
    [InlineData(typeof(nint))]
    [InlineData(typeof(nuint))]
    [InlineData(typeof(int?))]
    [InlineData(typeof(long?))]
    [InlineData(typeof(short?))]
    [InlineData(typeof(byte?))]
    [InlineData(typeof(uint?))]
    [InlineData(typeof(ulong?))]
    [InlineData(typeof(ushort?))]
    [InlineData(typeof(sbyte?))]
    public void IntegerTypes_MapToPlainNumpad(Type type) =>
        Assert.Equal("numpad", NumericLayoutResolver.Resolve(type));

    [Theory]
    // Not numeric MudNumericField types — they fall back to a safe numeric keypad with a "." key rather
    // than throwing, so a misuse degrades gracefully instead of breaking the docked keyboard.
    [InlineData(typeof(string))]
    [InlineData(typeof(bool))]
    [InlineData(typeof(char))]
    [InlineData(typeof(DateTime))]
    [InlineData(typeof(Guid))]
    [InlineData(typeof(object))]
    [InlineData(typeof(BigInteger))]
    [InlineData(typeof(DayOfWeek))] // an enum
    public void UnrecognisedTypes_FallBackToDecimalKeypad(Type type) =>
        Assert.Equal("decimal", NumericLayoutResolver.Resolve(type));

    [Fact]
    public void TokenConstants_MatchTheStringsTheShimAndResolveLayoutUse()
    {
        // The resolver emits these exact tokens; the JS shim forwards them verbatim and ResolveLayout
        // switches on them. Pin the literals so the two halves can never silently drift apart.
        Assert.Equal("money", NumericLayoutResolver.Money);
        Assert.Equal("decimal", NumericLayoutResolver.Decimal);
        Assert.Equal("numpad", NumericLayoutResolver.Numpad);
    }

    [Theory]
    // End-to-end contract: the token the resolver produces for a type must drive ResolveLayout to the
    // intended built-in layout. This ties the wrapper (type → token) to the docked keyboard
    // (token → layout) so the whole detection feature is verified, not just each half in isolation.
    [InlineData(typeof(decimal), nameof(LayoutLibrary.Price))]
    [InlineData(typeof(decimal?), nameof(LayoutLibrary.Price))]
    [InlineData(typeof(double), nameof(LayoutLibrary.NumpadWithDecimal))]
    [InlineData(typeof(float), nameof(LayoutLibrary.NumpadWithDecimal))]
    [InlineData(typeof(int), nameof(LayoutLibrary.Numpad))]
    [InlineData(typeof(long), nameof(LayoutLibrary.Numpad))]
    public void ResolvedToken_DrivesTheDockedKeyboardToTheExpectedLayout(Type type, string expectedLayout)
    {
        var token = NumericLayoutResolver.Resolve(type);
        var (layout, symbol) = KeyboardInteropService.ResolveLayout(token);

        var expected = expectedLayout switch
        {
            nameof(LayoutLibrary.Price) => LayoutLibrary.Price,
            nameof(LayoutLibrary.NumpadWithDecimal) => LayoutLibrary.NumpadWithDecimal,
            nameof(LayoutLibrary.Numpad) => LayoutLibrary.Numpad,
            _ => throw new ArgumentOutOfRangeException(nameof(expectedLayout), expectedLayout, null),
        };

        Assert.Same(expected, layout);
        // The numeric keypads never have a numbers/symbols face to flip to.
        Assert.Null(symbol);
    }
}
