<%@ WebHandler Language="C#" Class="PrintService" %>
<%@ Assembly Name="System.Drawing" %>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing.Printing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;

// Server-side printing: lists the printers installed on THIS web server and sends raw ZPL to one of them.
//   GET  PrintService.ashx?action=list
//   POST PrintService.ashx?action=print&printer=<name>      (body = ZPL text)
// Optional Web.config key "AllowedPrinters" (comma separated names) limits which printers can be used.
public class PrintService : IHttpHandler
{
    private const int MaxBytes = 512 * 1024;

    public bool IsReusable { get { return false; } }

    public void ProcessRequest(HttpContext ctx)
    {
        ctx.Response.ContentType = "application/json";
        ctx.Response.Cache.SetCacheability(HttpCacheability.NoCache);
        try
        {
            string action = (ctx.Request.QueryString["action"] ?? "").ToLowerInvariant();
            if (action == "list")
            {
                ListPrinters(ctx);
            }
            else if (action == "print" && ctx.Request.HttpMethod == "POST")
            {
                Print(ctx);
            }
            else
            {
                Fail(ctx, 400, "Unknown action");
            }
        }
        catch (Exception ex)
        {
            Fail(ctx, 500, ex.Message);
        }
    }

    private static List<string> GetPrinters()
    {
        List<string> result = new List<string>();
        string allowed = ConfigurationManager.AppSettings["AllowedPrinters"];
        List<string> allowList = null;
        if (!string.IsNullOrEmpty(allowed))
        {
            allowList = new List<string>();
            foreach (string a in allowed.Split(','))
            {
                if (a.Trim().Length > 0) allowList.Add(a.Trim().ToLowerInvariant());
            }
        }
        foreach (string name in PrinterSettings.InstalledPrinters)
        {
            if (IsVirtualPrinter(name)) continue;   // PDF / XPS / OneNote / Fax are not label printers
            if (allowList == null || allowList.Contains(name.ToLowerInvariant())) result.Add(name);
        }
        return result;
    }

    private static bool IsVirtualPrinter(string name)
    {
        string n = name.ToLowerInvariant();
        return n.Contains("print to pdf") || n.Contains("xps") || n.Contains("onenote")
            || n.Contains("fax") || n.Contains("send to") || n.Contains("pdf");
    }

    private static string GetDefaultPrinter()
    {
        try { return new PrinterSettings().PrinterName ?? ""; }
        catch (Exception) { return ""; }
    }

    private void ListPrinters(HttpContext ctx)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("{\"default\":\"").Append(HttpUtility.JavaScriptStringEncode(GetDefaultPrinter())).Append("\",\"printers\":[");
        bool first = true;
        foreach (string name in GetPrinters())
        {
            if (!first) sb.Append(",");
            first = false;
            sb.Append("\"").Append(HttpUtility.JavaScriptStringEncode(name)).Append("\"");
        }
        sb.Append("]}");
        ctx.Response.Write(sb.ToString());
    }

    private void Print(HttpContext ctx)
    {
        string printer = ctx.Request.QueryString["printer"] ?? "";
        bool known = false;
        foreach (string name in GetPrinters())
        {
            if (string.Equals(name, printer, StringComparison.OrdinalIgnoreCase)) { printer = name; known = true; break; }
        }
        if (!known)
        {
            Fail(ctx, 400, "Printer not found on the server: " + printer);
            return;
        }
        if (ctx.Request.ContentLength > MaxBytes)
        {
            Fail(ctx, 413, "Label data too large");
            return;
        }

        string zpl;
        using (StreamReader reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
        {
            zpl = reader.ReadToEnd();
        }
        if (zpl.Trim().Length == 0)
        {
            Fail(ctx, 400, "Nothing to print");
            return;
        }

        byte[] bytes = Encoding.GetEncoding(1252).GetBytes(zpl);
        RawPrinterHelper.SendBytesToPrinter(printer, bytes);
        ctx.Response.Write("{\"ok\":true}");
    }

    private static void Fail(HttpContext ctx, int status, string message)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.Write("{\"error\":\"" + HttpUtility.JavaScriptStringEncode(message) + "\"}");
    }
}

// Sends raw bytes (ZPL) to a Windows printer queue (winspool RAW datatype).
public static class RawPrinterHelper
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    private class DOCINFOA
    {
        [MarshalAs(UnmanagedType.LPStr)] public string pDocName;
        [MarshalAs(UnmanagedType.LPStr)] public string pOutputFile;
        [MarshalAs(UnmanagedType.LPStr)] public string pDataType;
    }

    [DllImport("winspool.Drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool OpenPrinter([MarshalAs(UnmanagedType.LPStr)] string szPrinter, out IntPtr hPrinter, IntPtr pd);

    [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool ClosePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartDocPrinter(IntPtr hPrinter, Int32 level, [In, MarshalAs(UnmanagedType.LPStruct)] DOCINFOA di);

    [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndDocPrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool StartPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool EndPagePrinter(IntPtr hPrinter);

    [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
    private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, Int32 dwCount, out Int32 dwWritten);

    public static void SendBytesToPrinter(string printerName, byte[] data)
    {
        IntPtr hPrinter;
        if (!OpenPrinter(printerName.Normalize(), out hPrinter, IntPtr.Zero))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Cannot open printer '" + printerName + "'");

        IntPtr unmanaged = Marshal.AllocCoTaskMem(data.Length);
        try
        {
            Marshal.Copy(data, 0, unmanaged, data.Length);

            DOCINFOA di = new DOCINFOA();
            di.pDocName = "Saw Barcode Label";
            di.pDataType = "RAW";

            if (!StartDocPrinter(hPrinter, 1, di))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "StartDocPrinter failed");
            try
            {
                if (!StartPagePrinter(hPrinter))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "StartPagePrinter failed");
                try
                {
                    int written;
                    if (!WritePrinter(hPrinter, unmanaged, data.Length, out written) || written != data.Length)
                        throw new Win32Exception(Marshal.GetLastWin32Error(), "WritePrinter failed");
                }
                finally { EndPagePrinter(hPrinter); }
            }
            finally { EndDocPrinter(hPrinter); }
        }
        finally
        {
            Marshal.FreeCoTaskMem(unmanaged);
            ClosePrinter(hPrinter);
        }
    }
}
