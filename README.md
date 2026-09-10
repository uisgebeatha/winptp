# winptp

Simple native Windows application for creating and printing labels with the Brother PT-P300BT.

## Current state

- PT-P300BT connection and status queries are verified over the Windows Bluetooth serial/RFCOMM interface.
- Text printing is verified with 12 mm tape using the Brother raster protocol.
- The editor supports editable text with a live preview generated from the final one-bit printer raster.
- Text can use installed Windows font families with automatic largest-fitting or adjustable manual sizing.
- The calculated physical label length updates live with the final raster.
- Printing currently supports 12 mm tape only.

Development path: F:\DEV\winptp
