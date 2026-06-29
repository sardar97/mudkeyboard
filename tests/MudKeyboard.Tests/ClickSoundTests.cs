using System.Text;
using Bunit;
using MudKeyboard.Components;
using MudKeyboard.Internal;
using MudKeyboard.Models;

namespace MudKeyboard.Tests;

/// <summary>
/// The synthesised click sound (<see cref="ClickSound"/>) and the way the keyboards play it: a
/// Blazor-rendered <c>&lt;audio&gt;</c> element, no JavaScript. The element appears only after a key
/// press and re-mounts on each press so the browser autoplays it.
/// </summary>
public class ClickSoundGeneratorTests
{
    [Fact]
    public void DataUri_IsABase64WavDataUri()
    {
        Assert.StartsWith("data:audio/wav;base64,", ClickSound.DataUri);
    }

    [Fact]
    public void DataUri_DecodesToACanonicalWavFile()
    {
        const string prefix = "data:audio/wav;base64,";
        var base64 = ClickSound.DataUri[prefix.Length..];
        var bytes = Convert.FromBase64String(base64);

        // RIFF/WAVE header markers at their canonical offsets, then a non-empty data chunk.
        Assert.True(bytes.Length > 44);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(bytes, 0, 4));
        Assert.Equal("WAVE", Encoding.ASCII.GetString(bytes, 8, 4));
        Assert.Equal("data", Encoding.ASCII.GetString(bytes, 36, 4));
    }

    [Fact]
    public void DataUri_IsCached_SoTheSameInstanceComesBack()
    {
        Assert.Same(ClickSound.DataUri, ClickSound.DataUri);
    }
}

public class KeyboardSoundRenderingTests : MudComponentTestContext
{
    [Fact]
    public void Numpad_WithSound_RendersNoAudioBeforeAnyPress()
    {
        var cut = Render<MudNumpad>(p => p.Add(c => c.Sound, true));

        Assert.Empty(cut.FindAll("audio"));
    }

    [Fact]
    public void Numpad_WithSound_RendersAutoplayingAudioAfterAPress()
    {
        var cut = Render<MudNumpad>(p => p.Add(c => c.Sound, true));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "7").Click();

        cut.WaitForAssertion(() =>
        {
            var audio = cut.Find("audio");
            Assert.True(audio.HasAttribute("autoplay"));
            Assert.StartsWith("data:audio/wav;base64,", audio.GetAttribute("src"));
        });
    }

    [Fact]
    public void Numpad_WithoutSound_NeverRendersAudio()
    {
        var cut = Render<MudNumpad>();

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "7").Click();

        Assert.Empty(cut.FindAll("audio"));
    }

    [Fact]
    public void SoundSrc_OverridesTheDefaultClick()
    {
        const string custom = "https://example.com/click.mp3";
        var cut = Render<MudNumpad>(p => p
            .Add(c => c.Sound, true)
            .Add(c => c.SoundSrc, custom));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "7").Click();

        cut.WaitForAssertion(() => Assert.Equal(custom, cut.Find("audio").GetAttribute("src")));
    }

    [Fact]
    public void Keyboard_WithSound_PlaysOnKeyPress()
    {
        var cut = Render<MudKeyboard.Components.MudKeyboard>(p => p.Add(c => c.Sound, true));

        cut.FindAll("button").First(b => b.TextContent.Trim() == "a").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("audio")));
    }

    [Fact]
    public void Pricepad_WithSound_PlaysOnKeyPress()
    {
        var cut = Render<MudPricepad>(p => p.Add(c => c.Sound, true));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "7").Click();

        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("audio")));
    }

    [Fact]
    public void Audio_ReMountsOnEachPress_SoItReplaysRatherThanStacking()
    {
        var cut = Render<MudNumpad>(p => p.Add(c => c.Sound, true));

        cut.FindAll("button").Single(b => b.TextContent.Trim() == "1").Click();
        cut.FindAll("button").Single(b => b.TextContent.Trim() == "2").Click();

        // A keyed element means there is never more than one <audio> at a time — the previous one is
        // replaced, so a rapid run of keys re-triggers the click instead of stacking elements.
        cut.WaitForAssertion(() => Assert.Single(cut.FindAll("audio")));
    }
}
