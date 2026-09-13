using WinPTP.Rendering;

namespace WinPTP.Tests;

public sealed class LabelRasterComposerTests
{
    [Fact]
    public void Compose_OneCopyReturnsOriginalRasterUnchanged()
    {
        LabelRaster original = CreateRaster();

        LabelRaster composed = LabelRasterComposer.Compose(
            original,
            copies: 1,
            LabelRasterComposer.CopySeparatorDots);

        Assert.Same(original, composed);
        Assert.Equal(original.PackedData.ToArray(), composed.PackedData.ToArray());
    }

    [Fact]
    public void Compose_TwoCopiesContainsOriginalBlankSeparatorAndOriginal()
    {
        LabelRaster original = CreateRaster();

        LabelRaster composed = LabelRasterComposer.Compose(
            original,
            copies: 2,
            LabelRasterComposer.CopySeparatorDots);

        Assert.Equal(
            (2 * original.RasterLineCount) + LabelRasterComposer.CopySeparatorDots,
            composed.RasterLineCount);
        Assert.Equal(original.PackedData.ToArray(), GetCopyData(composed, startLine: 0, original));
        AssertSeparatorIsBlank(
            composed,
            startLine: original.RasterLineCount,
            LabelRasterComposer.CopySeparatorDots);
        Assert.Equal(
            original.PackedData.ToArray(),
            GetCopyData(
                composed,
                original.RasterLineCount + LabelRasterComposer.CopySeparatorDots,
                original));
    }

    [Fact]
    public void Compose_ThreeCopiesContainsTwoBlankSeparatorsAndIdenticalPixels()
    {
        LabelRaster original = CreateRaster();
        int copyStride = original.RasterLineCount + LabelRasterComposer.CopySeparatorDots;

        LabelRaster composed = LabelRasterComposer.Compose(
            original,
            copies: 3,
            LabelRasterComposer.CopySeparatorDots);

        Assert.Equal(
            (3 * original.RasterLineCount)
                + (2 * LabelRasterComposer.CopySeparatorDots),
            composed.RasterLineCount);

        for (int copy = 0; copy < 3; copy++)
        {
            Assert.Equal(
                original.PackedData.ToArray(),
                GetCopyData(composed, copy * copyStride, original));
        }

        AssertSeparatorIsBlank(
            composed,
            original.RasterLineCount,
            LabelRasterComposer.CopySeparatorDots);
        AssertSeparatorIsBlank(
            composed,
            copyStride + original.RasterLineCount,
            LabelRasterComposer.CopySeparatorDots);
    }

    [Theory]
    [InlineData(1, 3)]
    [InlineData(2, 34)]
    [InlineData(3, 65)]
    [InlineData(99, 3041)]
    public void CalculateRasterLineCount_UsesCopiesAndCopiesMinusOneSeparators(
        int copies,
        int expectedLineCount)
    {
        int lineCount = LabelRasterComposer.CalculateRasterLineCount(
            originalRasterLineCount: 3,
            copies,
            separatorDots: 28);

        Assert.Equal(expectedLineCount, lineCount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Compose_CopyCountOutsideOneToNinetyNineThrows(int copies)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => LabelRasterComposer.Compose(CreateRaster(), copies));
    }

    [Fact]
    public void Compose_PreservesPrinterHeadGeometry()
    {
        LabelRaster composed = LabelRasterComposer.Compose(
            CreateRaster(),
            copies: 2,
            LabelRasterComposer.CopySeparatorDots);

        Assert.Equal(128, composed.DotsPerRasterLine);
        Assert.Equal(16, LabelRaster.BytesPerRasterLine);
        Assert.Equal(
            composed.RasterLineCount * LabelRaster.BytesPerRasterLine,
            composed.PackedData.Length);
    }

    private static LabelRaster CreateRaster()
    {
        byte[] packedData = new byte[3 * LabelRaster.BytesPerRasterLine];
        packedData[0] = 0x80;
        packedData[LabelRaster.BytesPerRasterLine + 5] = 0x24;
        packedData[(2 * LabelRaster.BytesPerRasterLine) + 15] = 0x01;
        return new LabelRaster(3, packedData);
    }

    private static byte[] GetCopyData(
        LabelRaster composed,
        int startLine,
        LabelRaster original)
    {
        int byteOffset = startLine * LabelRaster.BytesPerRasterLine;
        return composed.PackedData.Span
            .Slice(byteOffset, original.PackedData.Length)
            .ToArray();
    }

    private static void AssertSeparatorIsBlank(
        LabelRaster composed,
        int startLine,
        int separatorDots)
    {
        for (int line = startLine; line < startLine + separatorDots; line++)
        {
            Assert.All(composed.GetRasterLine(line).ToArray(), value => Assert.Equal(0x00, value));
        }
    }
}
