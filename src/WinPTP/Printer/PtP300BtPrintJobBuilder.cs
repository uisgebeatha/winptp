using System.Buffers.Binary;
using System.IO;
using WinPTP.Rendering;

namespace WinPTP.Printer;

internal static class PtP300BtPrintJobBuilder
{
    public const ushort EndFeedMarginDots = 28;

    private const byte ActivePrintInformationFlags = 0xC4;

    public static byte[] Build(PtP300BtStatus status, LabelRaster raster)
    {
        ArgumentNullException.ThrowIfNull(status);
        ArgumentNullException.ThrowIfNull(raster);

        using MemoryStream job = new();
        job.Write(new byte[64]);
        job.Write([0x1B, 0x40]);
        job.Write([0x1B, 0x69, 0x61, 0x01]);
        job.Write(BuildPrintInformation(
            status.MediaType,
            status.TapeWidthMillimeters,
            status.FixedTapeLength,
            raster.RasterLineCount));
        job.Write([0x1B, 0x69, 0x4B, 0x08]);
        job.Write([0x1B, 0x69, 0x4D, 0x00]);

        Span<byte> marginCommand = stackalloc byte[5];
        marginCommand[0] = 0x1B;
        marginCommand[1] = 0x69;
        marginCommand[2] = 0x64;
        BinaryPrimitives.WriteUInt16LittleEndian(marginCommand[3..], EndFeedMarginDots);
        job.Write(marginCommand);

        job.Write([0x4D, 0x00]);

        for (int line = 0; line < raster.RasterLineCount; line++)
        {
            job.Write(BuildRasterTransfer(raster.GetRasterLine(line)));
        }

        job.WriteByte(0x1A);
        return job.ToArray();
    }

    public static byte[] BuildPrintInformation(
        byte mediaType,
        byte mediaWidthMillimeters,
        byte mediaLengthMillimeters,
        int rasterLineCount)
    {
        if (rasterLineCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(rasterLineCount));
        }

        byte[] command =
        [
            0x1B, 0x69, 0x7A,
            ActivePrintInformationFlags,
            mediaType,
            mediaWidthMillimeters,
            mediaLengthMillimeters,
            0x00, 0x00, 0x00, 0x00,
            0x00,
            0x00
        ];

        BinaryPrimitives.WriteUInt32LittleEndian(command.AsSpan(7, 4), (uint)rasterLineCount);
        return command;
    }

    public static byte[] BuildRasterTransfer(ReadOnlySpan<byte> rasterLine)
    {
        if (rasterLine.Length != LabelRaster.BytesPerRasterLine)
        {
            throw new ArgumentException(
                $"A raster line must contain exactly {LabelRaster.BytesPerRasterLine} bytes.",
                nameof(rasterLine));
        }

        byte[] command = new byte[3 + LabelRaster.BytesPerRasterLine];
        command[0] = 0x47;
        command[1] = LabelRaster.BytesPerRasterLine;
        command[2] = 0x00;
        rasterLine.CopyTo(command.AsSpan(3));
        return command;
    }
}
