using MudKeyboard.Internal;
using MudKeyboard.Models;

namespace MudKeyboard.Tests;

public class KeyboardEngineBackspaceTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Backspace_OnEmptyOrNull_ReturnsEmptyAndDoesNotThrow(string? current) =>
        Assert.Equal(string.Empty, KeyboardEngine.Backspace(current));

    [Theory]
    [InlineData("a", "")]
    [InlineData("ab", "a")]
    [InlineData("abc", "ab")]
    [InlineData("£1.23", "£1.2")]
    public void Backspace_RemovesTrailingCharacter(string current, string expected) =>
        Assert.Equal(expected, KeyboardEngine.Backspace(current));
}

public class KeyboardEngineMaxLengthTests
{
    [Fact]
    public void Append_NoMaxLength_AppendsVerbatim() =>
        Assert.Equal("abc", KeyboardEngine.Append("ab", "c", null));

    [Fact]
    public void Append_NullCurrent_TreatedAsEmpty() =>
        Assert.Equal("x", KeyboardEngine.Append(null, "x", null));

    [Fact]
    public void Append_UnderMaxLength_AppendsVerbatim() =>
        Assert.Equal("abc", KeyboardEngine.Append("ab", "c", 5));

    [Fact]
    public void Append_AtMaxLength_KeepsValue() =>
        Assert.Equal("abc", KeyboardEngine.Append("ab", "c", 3));

    [Fact]
    public void Append_OverMaxLength_TruncatesToCap() =>
        Assert.Equal("abc", KeyboardEngine.Append("ab", "cd", 3));

    [Fact]
    public void Append_AlreadyAtCap_RejectsFurtherInput() =>
        Assert.Equal("abc", KeyboardEngine.Append("abc", "d", 3));

    [Fact]
    public void Append_MultiCharText_RespectsCapMidToken() =>
        Assert.Equal("1234", KeyboardEngine.Append("12", "3456", 4));
}

public class KeyboardEngineProjectionTests
{
    // A custom layout is just data; FromRows must project it to the same shape it was given.
    [Fact]
    public void FromRows_CustomLayout_PreservesRowAndKeyCounts()
    {
        var layout = new KeyboardLayout
        {
            Rows = new string[][]
            {
                ["a", "b", "c"],
                ["d", "e"],
                [KeyTokens.Space, KeyTokens.Enter],
            },
        };

        var rows = KeyboardKey.FromRows(layout.Rows);

        Assert.Equal(3, rows.Count);
        Assert.Equal(new[] { 3, 2, 2 }, rows.Select(r => r.Count));
    }

    [Fact]
    public void FromRows_PreservesActionTokensInOrder()
    {
        var layout = new KeyboardLayout
        {
            Rows = new string[][] { ["1", "2"], [KeyTokens.Backspace] },
        };

        var rows = KeyboardKey.FromRows(layout.Rows);

        Assert.Equal("1", rows[0][0].ActionToken);
        Assert.Equal("2", rows[0][1].ActionToken);
        Assert.Equal(KeyTokens.Backspace, rows[1][0].ActionToken);
    }

    [Fact]
    public void BuildDisplayKeys_CustomLayout_PreservesRowAndKeyCounts()
    {
        var layout = new KeyboardLayout
        {
            Rows = new string[][]
            {
                ["q", "w", "e", "r", "t"],
                [KeyTokens.Shift, "z", "x", KeyTokens.Backspace],
            },
        };

        var rows = KeyboardEngine.BuildDisplayKeys(layout, ShiftState.Off);

        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { 5, 4 }, rows.Select(r => r.Count));
    }
}
