# winptp

Simple native Windows application for creating and printing labels with the Brother PT-P300BT.

## Current state

- PT-P300BT connection and status queries are verified over the Windows Bluetooth serial/RFCOMM interface.
- Text printing is verified with 12 mm tape using the Brother raster protocol.
- The editor supports explicit multiline text and Bold/Italic/Underline with a live preview generated from the final one-bit printer raster.
- Text can use installed Windows font families with automatic largest-fitting or adjustable manual sizing.
- Auto sizing fits the styled text block's ink bounds; Manual sizing clamps to the printable area.
- Physical label length and estimated tape use update live, displayed in whole millimetres rounded up; internal lengths remain precise.
- Multiple copies are produced as one continuous composite strip with approximately 4 mm of blank space between labels.
- Estimated tape use reflects the composite raster plus the configured final feed; it does not include calibrated mechanical cassette waste.
- Printing currently supports 12 mm tape only.

Development path: F:\DEV\winptp
