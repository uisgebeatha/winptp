using WinPTP.Printer;

namespace WinPTP.Tests;

public sealed class PtP300BtStatusParserTests
{
    [Fact]
    public void Parse_ValidPtP300BtResponse_ParsesAllStatusFields()
    {
        byte[] response = CreateValidResponse();
        response[8] = 0x12;
        response[9] = 0x34;
        response[10] = 24;
        response[11] = 0x03;
        response[17] = 42;
        response[18] = 0x05;
        response[19] = 0x01;
        response[20] = 0xAB;
        response[21] = 0xCD;

        PtP300BtStatus status = PtP300BtStatusParser.Parse(response);

        Assert.Equal(0x1234, status.ErrorFlags);
        Assert.Equal(24, status.TapeWidthMillimeters);
        Assert.Equal(0x03, status.MediaType);
        Assert.Equal(42, status.FixedTapeLength);
        Assert.Equal(0x05, status.StatusType);
        Assert.Equal(0x01, status.PhaseType);
        Assert.Equal(0xABCD, status.Phase);
    }

    [Fact]
    public void Parse_ModelByte72_IsAccepted()
    {
        byte[] response = CreateValidResponse();

        PtP300BtStatus status = PtP300BtStatusParser.Parse(response);

        Assert.NotNull(status);
    }

    [Theory]
    [InlineData(31)]
    [InlineData(33)]
    public void Parse_InvalidResponseLength_Throws(int responseLength)
    {
        byte[] response = new byte[responseLength];

        PtP300BtResponseException exception = Assert.Throws<PtP300BtResponseException>(
            () => PtP300BtStatusParser.Parse(response));

        Assert.Contains($"{responseLength} bytes", exception.Message);
    }

    [Fact]
    public void Parse_InvalidMagic_Throws()
    {
        byte[] response = CreateValidResponse();
        response[2] = 0x00;

        PtP300BtResponseException exception = Assert.Throws<PtP300BtResponseException>(
            () => PtP300BtStatusParser.Parse(response));

        Assert.Contains("invalid printer status header", exception.Message);
    }

    [Fact]
    public void Parse_WrongPrinterModel_Throws()
    {
        byte[] response = CreateValidResponse();
        response[4] = 0x71;

        PtP300BtResponseException exception = Assert.Throws<PtP300BtResponseException>(
            () => PtP300BtStatusParser.Parse(response));

        Assert.Contains("not a PT-P300BT", exception.Message);
        Assert.Contains("0x71", exception.Message);
    }

    [Theory]
    [InlineData(0x00, "Not loaded")]
    [InlineData(0x01, "Laminated TZe")]
    [InlineData(0x03, "Non-laminated TZeN")]
    [InlineData(0x11, "Heat-shrink")]
    [InlineData(0x4A, "Continuous tape")]
    [InlineData(0x4B, "Die-cut labels")]
    [InlineData(0xFF, "Unsupported")]
    public void MediaTypeDescription_KnownMediaType_IsReadable(int mediaType, string expectedDescription)
    {
        byte[] response = CreateValidResponse();
        response[11] = (byte)mediaType;

        PtP300BtStatus status = PtP300BtStatusParser.Parse(response);

        Assert.Equal((byte)mediaType, status.MediaType);
        Assert.Equal(expectedDescription, status.MediaTypeDescription);
    }

    [Fact]
    public void State_NoErrorsAndIdlePhase_IsReady()
    {
        byte[] response = CreateValidResponse();
        response[8] = 0x00;
        response[9] = 0x00;
        response[19] = 0x00;
        response[20] = 0x00;
        response[21] = 0x00;

        PtP300BtStatus status = PtP300BtStatusParser.Parse(response);

        Assert.True(status.IsReady);
        Assert.Equal("Ready", status.StateDescription);
    }

    [Fact]
    public void State_ActivePhase_IsNotReady()
    {
        byte[] response = CreateValidResponse();
        response[19] = 0x01;
        response[20] = 0x00;
        response[21] = 0x02;

        PtP300BtStatus status = PtP300BtStatusParser.Parse(response);

        Assert.False(status.IsReady);
        Assert.StartsWith("Not ready", status.StateDescription);
    }

    [Fact]
    public void State_NonZeroErrorFlags_IsError()
    {
        byte[] response = CreateValidResponse();
        response[8] = 0x00;
        response[9] = 0x02;

        PtP300BtStatus status = PtP300BtStatusParser.Parse(response);

        Assert.False(status.IsReady);
        Assert.Equal("Error (printer flags 0x0002)", status.StateDescription);
    }

    private static byte[] CreateValidResponse()
    {
        byte[] response = new byte[PtP300BtStatusParser.ResponseLength];
        response[0] = 0x80;
        response[1] = 0x20;
        response[2] = 0x42;
        response[3] = 0x30;
        response[4] = 0x72;
        return response;
    }
}
