using WinPTP.Printer;
using WinPTP.Rendering;

namespace WinPTP.Tests;

public sealed class PtP300BtPrintJobBuilderTests
{
    [Fact]
    public void BuildPrintInformation_UsesExpectedLayoutAndLittleEndianRasterCount()
    {
        byte[] command = PtP300BtPrintJobBuilder.BuildPrintInformation(
            mediaType: 0x01,
            mediaWidthMillimeters: 12,
            mediaLengthMillimeters: 0,
            rasterLineCount: 0x12345678);

        Assert.Equal(
            new byte[]
            {
                0x1B, 0x69, 0x7A,
                0xC4,
                0x01,
                0x0C,
                0x00,
                0x78, 0x56, 0x34, 0x12,
                0x00,
                0x00
            },
            command);
    }

    [Fact]
    public void BuildRasterTransfer_NonZeroLine_UsesUncompressedFraming()
    {
        byte[] rasterLine = Enumerable.Range(1, LabelRaster.BytesPerRasterLine)
            .Select(value => (byte)value)
            .ToArray();

        byte[] command = PtP300BtPrintJobBuilder.BuildRasterTransfer(rasterLine);

        Assert.Equal(0x47, command[0]);
        Assert.Equal(0x10, command[1]);
        Assert.Equal(0x00, command[2]);
        Assert.Equal(rasterLine, command[3..]);
    }

    [Fact]
    public void Build_UsesRequiredUncompressedSequenceAndFinalPrintCommand()
    {
        PtP300BtStatus status = new(
            ErrorFlags: 0,
            TapeWidthMillimeters: 12,
            MediaType: 0x4A,
            FixedTapeLength: 7,
            StatusType: 0,
            PhaseType: 0,
            Phase: 0);
        byte[] rasterLine = new byte[LabelRaster.BytesPerRasterLine];
        rasterLine[0] = 0x80;
        LabelRaster raster = new(1, rasterLine);

        byte[] job = PtP300BtPrintJobBuilder.Build(status, raster);

        Assert.All(job[..64], value => Assert.Equal(0x00, value));
        Assert.Equal(new byte[] { 0x1B, 0x40, 0x1B, 0x69, 0x61, 0x01 }, job[64..70]);
        Assert.Equal(
            new byte[] { 0x1B, 0x69, 0x7A, 0xC4, 0x4A, 0x0C, 0x07, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00 },
            job[70..83]);
        Assert.Equal(new byte[] { 0x1B, 0x69, 0x4B, 0x08 }, job[83..87]);
        Assert.Equal(new byte[] { 0x1B, 0x69, 0x4D, 0x00 }, job[87..91]);
        Assert.Equal(new byte[] { 0x1B, 0x69, 0x64, 0x1C, 0x00 }, job[91..96]);
        Assert.Equal(new byte[] { 0x4D, 0x00 }, job[96..98]);
        Assert.Equal(PtP300BtPrintJobBuilder.BuildRasterTransfer(rasterLine), job[98..117]);
        Assert.Equal(0x1A, job[^1]);
    }
}
