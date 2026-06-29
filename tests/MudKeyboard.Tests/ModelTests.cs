using MudKeyboard.Models;

namespace MudKeyboard.Tests;

public class KeyboardPaletteTests
{
    [Fact]
    public void Compose_NullPalette_ReturnsStyleUnchanged()
    {
        Assert.Equal("color:red", KeyboardPalette.Compose(null, "color:red"));
        Assert.Null(KeyboardPalette.Compose(null, null));
    }

    [Fact]
    public void Compose_EmptyPalette_ReturnsStyleUnchanged()
    {
        var palette = new KeyboardPalette();

        Assert.Equal("color:red", KeyboardPalette.Compose(palette, "color:red"));
    }

    [Fact]
    public void Compose_SetSlots_EmitMudPaletteVariables()
    {
        var palette = new KeyboardPalette { AccentColor = "#00897b", AccentTextColor = "#fff" };

        var style = KeyboardPalette.Compose(palette, null);

        Assert.Contains("--mud-palette-primary:#00897b;", style);
        Assert.Contains("--mud-palette-primary-darken:#00897b;", style); // accent also pins the darken twin
        Assert.Contains("--mud-palette-primary-text:#fff;", style);
    }

    [Fact]
    public void Compose_UnsetSlots_AreOmittedSoTheyFallThroughToTheTheme()
    {
        var palette = new KeyboardPalette { Surface = "#101010" };

        var style = KeyboardPalette.Compose(palette, null);

        Assert.Contains("--mud-palette-surface:#101010;", style);
        Assert.DoesNotContain("--mud-palette-primary", style);
        Assert.DoesNotContain("--mud-palette-text-primary", style);
    }

    [Fact]
    public void Compose_AppendsCallerStyleAfterOverrides_SoExplicitStyleWins()
    {
        var palette = new KeyboardPalette { Surface = "#101010" };

        var style = KeyboardPalette.Compose(palette, "border-radius:8px");

        Assert.EndsWith("border-radius:8px", style);
        Assert.True(style!.IndexOf("--mud-palette-surface", StringComparison.Ordinal)
                    < style.IndexOf("border-radius", StringComparison.Ordinal));
    }
}

public class KeyboardKeyTests
{
    [Theory]
    [InlineData("{bksp}", "⌫")]
    [InlineData("{enter}", "⏎")]
    [InlineData("{shift}", "⇧")]
    [InlineData("{caps}", "⇪")]
    [InlineData("{sym}", "123")]
    [InlineData("{esc}", "Esc")]
    public void FromToken_CommandTokens_GetGlyphLabels(string token, string expectedLabel)
    {
        var key = KeyboardKey.FromToken(token);

        Assert.Equal(token, key.ActionToken);
        Assert.Equal(expectedLabel, key.DisplayLabel);
    }

    [Theory]
    [InlineData("a")]
    [InlineData("5")]
    [InlineData("£")]
    public void FromToken_LiteralTokens_LabelEqualsToken_AndStandardWidth(string token)
    {
        var key = KeyboardKey.FromToken(token);

        Assert.Equal(token, key.DisplayLabel);
        Assert.Equal(1.0d, key.WidthMultiplier);
        Assert.False(key.IsCommand);
    }

    [Fact]
    public void FromToken_Backspace_IsCommandButNotEnter()
    {
        var key = KeyboardKey.FromToken(KeyTokens.Backspace);

        Assert.True(key.IsCommand);
        Assert.False(key.IsEnter);
    }

    [Fact]
    public void FromToken_Enter_IsEnterAndNotFlaggedAsCommand()
    {
        // Enter is deliberately excluded from IsCommand so it commits rather than being skipped.
        var key = KeyboardKey.FromToken(KeyTokens.Enter);

        Assert.True(key.IsEnter);
        Assert.False(key.IsCommand);
    }

    [Fact]
    public void FromToken_Space_IsWideCommand()
    {
        var key = KeyboardKey.FromToken(KeyTokens.Space);

        Assert.True(key.IsCommand);
        Assert.True(key.WidthMultiplier > 1.0d);
    }
}
