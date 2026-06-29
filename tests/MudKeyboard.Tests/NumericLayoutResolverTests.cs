using MudKeyboard.Internal;

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
    [InlineData(typeof(int?))]
    [InlineData(typeof(long?))]
    public void IntegerTypes_MapToPlainNumpad(Type type) =>
        Assert.Equal("numpad", NumericLayoutResolver.Resolve(type));

    [Fact]
    public void UnknownType_FallsBackToDecimalKeypad() =>
        Assert.Equal("decimal", NumericLayoutResolver.Resolve(typeof(string)));
}
