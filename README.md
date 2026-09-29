# SawBarcode

ASP.NET Web Forms app (.NET Framework 4.0) for printing saw labels to a Zebra printer.

## Label printing (no Java)

The old jZebra Java applet (`js/jzebra.jar`) has been replaced in `Saw.aspx` by
**Zebra Browser Print**. Labels still print to the same local printer
(`ZDesigner GX430t`) on each operator PC; only Java is removed.

Setup on each operator PC:
1. Install **Zebra Browser Print** (free, from the Zebra support site).
2. Open Browser Print and check that `ZDesigner GX430t` is listed.
3. On the first print, accept the site in the Browser Print prompt.

Project setup:
- Copy the Browser Print JavaScript SDK (e.g. `BrowserPrint-3.1.250.min.js`, included in the
  Browser Print download) into `SawBarcode/js/`. If the version differs, update the
  `<script src="js/BrowserPrint-...">` line in `Saw.aspx`.

Still using the Java applet (not yet converted): `SawSpecial.aspx`, `SawBarcode.aspx`.

## Configuration

Passwords in `Web.config` are replaced with `CHANGE_ME`. Put the real connection strings
in your local / server copy only — do not commit real credentials.

## Build

Open `SawBarcode.sln` in Visual Studio, restore NuGet packages, build, publish.
