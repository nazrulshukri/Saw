# SawBarcode

ASP.NET Web Forms app (.NET Framework 4.0) for printing saw labels to a Zebra printer.

## Label printing (Zebra Browser Print, no Java)

A web page cannot see a USB printer by itself, so `Saw.aspx` talks to **Zebra Browser Print**
running on the operator PC (`http://127.0.0.1:9100`). No Java and no printer name in the code:
the page lists the Zebra printers on the PC, picks the one used last time (cookie), else the USB
one, else the first found. The operator can change it in the drop-down / "Find printers".

Each operator PC (one-time): install Zebra Browser Print (free, zebra.com), keep it running at
Windows start-up, and click "Yes/Accept" the first time the Saw page connects to it.

## Configuration

Passwords in `Web.config` are replaced with `CHANGE_ME`. Put the real connection strings
in your local / server copy only — do not commit real credentials.

## Build

Open `SawBarcode.sln` in Visual Studio, restore NuGet packages, build, publish.
