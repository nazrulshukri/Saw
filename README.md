# SawBarcode

ASP.NET Web Forms app (.NET Framework 4.0) for printing saw labels to a Zebra printer.

## Label printing (jZebra / Java)

`Saw.aspx` prints ZPL labels with the jZebra 1.5.6 Java applet (`js/jzebra.jar`).

- **No hardcoded printer.** After Java starts, the page asks jZebra for the printers on the
  PC and picks the one used last time on that PC (saved in a cookie), otherwise the first
  printer whose name contains `ZDesigner` / `Zebra` / `ZPL`. The operator can pick another
  printer from the list next to the status text; the choice is remembered.
- **No hang on the Java "Run" prompt.** The page does not call the applet until jZebra
  reports it is ready (`jzebraReady`) and never loops waiting on it. A label requested
  earlier (e.g. right after a postback) waits in a queue and prints once Java is running
  and a printer is selected.
- A postback (e.g. PrintAll) waits until its label has been sent, so the page reload does
  not stop Java in the middle of printing.

Each operator PC needs Java with the site allowed to run the applet (click **Run** on the
Java prompt, or add the site to the Java Exception Site List).

Not changed yet (still the old blocking code with `ZDesigner GX430t` hardcoded):
`SawSpecial.aspx`, `SawBarcode.aspx`.

## Configuration

Passwords in `Web.config` are replaced with `CHANGE_ME`. Put the real connection strings
in your local / server copy only — do not commit real credentials.

## Build

Open `SawBarcode.sln` in Visual Studio, restore NuGet packages, build, publish.
