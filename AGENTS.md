# AGENTS.md

## Project

winptp is a native Windows desktop utility for creating and printing labels with the Brother PT-P300BT.

Repository root: `F:\DEV\winptp`

## Development principles

- Inspect existing implementation before changing it.
- Make only the requested changes.
- Preserve working behaviour unless explicitly asked to change it.
- Keep dependencies modest.
- Prefer native/lightweight Windows technologies.
- Keep printer/protocol logic separate from UI logic where practical.
- Do not modify unrelated code.
- Do not commit unless explicitly requested.
- Do not track generated build output, temporary files, secrets, or machine-specific files.

## Initial technical direction

- Target modern .NET on Windows.
- WPF is the preferred UI framework unless investigation gives a strong reason otherwise.
- Windows handles Bluetooth pairing.
- Communicate with the PT-P300BT through its Bluetooth Serial/RFCOMM COM port.
- Render labels locally and send Brother raster commands to the printer.
- Existing open-source PT-P300BT implementations may be inspected as technical references, but source code must not be copied unless its licence clearly permits it.

## Initial milestone

Prove communication and printing before building the full editor:

1. Detect/select the printer COM port.
2. Generate a minimal monochrome raster label.
3. Print a hard-coded text label successfully.
4. Only then build the normal label-editor UI.

## Agent completion reports

Report:
- files changed
- implementation summary
- relevant checks/tests performed
- limitations or unresolved issues
- Git status

Do not commit unless explicitly asked.
