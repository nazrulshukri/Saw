# SawBarcode

ASP.NET Web Forms app (.NET Framework 4.0) for printing saw labels to a Zebra printer.

## Label printing (no Java)

The old jZebra Java applet (`js/jzebra.jar`) is gone from all three print pages
(`Saw.aspx`, `SawSpecial.aspx`, `SawBarcode.aspx`). They now print through
**Zebra Browser Print** using one shared script, `SawBarcode/js/zebra-print.js`.
The flow is unchanged: scan, Submit, Print / PrintAll, and the labels go as raw ZPL to the
local printer (`ZDesigner GX430t`) on each operator PC. The status bar next to the
Delete button replaces the old "Java Runtime not ready!" text.

`zebra-print.js` talks straight to the Browser Print service on the operator PC
(`http://localhost:9100`), so **no Zebra SDK file has to be copied into the project**.

### Setup on each operator PC (once)
1. Install **Zebra Browser Print** (free, from the Zebra support site) and leave it running.
2. Open Browser Print and check that `ZDesigner GX430t` is listed.
3. Open the Saw page. The status bar should show `Printer "ZDesigner GX430t" is ready`.
   If Browser Print asks whether to allow the site, accept it.

Java is no longer needed on the PC or in the browser.

### Status bar messages
| Message | Meaning |
|---|---|
| `Printer "ZDesigner GX430t" is ready` | OK |
| `Zebra Browser Print not running or not installed` | Install / start Browser Print |
| `Printer "ZDesigner GX430t" not found. Browser Print has: ...` | Printer name differs; see below |
| `Printer Not Ready (Browser Print sees no printer)` | Printer off / not connected |
| `js/zebra-print.js not found on the server` | The server was published without the new file |

If the name is not found but Browser Print has exactly one printer, that printer is used.
With several printers it refuses to guess. To use a different printer name, change
`printerName` at the top of `js/zebra-print.js`.

Note: `ZebraPrint.print()` is intentionally synchronous. "Print All" prints from an `onclick`
and then posts back to the server, and an asynchronous request would be cancelled by the reload.

## Configuration

Passwords in `Web.config` are replaced with `CHANGE_ME`. Put the real connection strings
in your local / server copy only. Do not commit real credentials.

## Build

Open `SawBarcode.sln` in Visual Studio, restore NuGet packages, build, publish.
`js/zebra-print.js` is listed in `SawBarcode.csproj` so it is included in the publish output.
