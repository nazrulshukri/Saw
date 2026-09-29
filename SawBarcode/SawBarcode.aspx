<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SawBarcode.aspx.cs" Inherits="ReadCrystal.SawBarcode" %>
<%@ Import Namespace="System.Web.Script.Serialization" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        .auto-style1 {
            width: 962px;
        }
        .auto-style2 {
            width: 689px;
        }
        .auto-style3 {
            width: 353px;
        }
    </style>
</head>
<body>


    <script type="text/javascript" src="js/jzebra.js"></script>
    <body onload="detectPrinter()">

        <table width="75%" border="0" align="center" cellpadding="0" cellspacing="0">
            <tr> <td colspan="2" ><span id="printerStatusBar">Loading...</span></td></tr>
            <tr>
                <td width="47%">
                    <form name="form1" method="post">
                        String untuk dicetak<br>
                        <textarea name="struk" cols="50" rows="7" id="struk"></textarea>
                        <br><input name="button" type=button onclick="printStruk(form1.struk.value)" value="Test Cetak">
                        <input type="reset" name="Reset" value="Reset">
                    </form>
                </td>
                <td width="53%">
                    <applet name="jZebra" code="jzebra.RawPrintApplet.class" archive="js/jzebra.jar" width="100" height="100">
                        <param name="printer" value="zebra">
                        <param name="sleep" value="200">
                    </applet>
                </td>
            </tr>
        </table>

       <script type="text/javascript" >

           function foo(myModel) {
               alert(myModel.Prop1);
               detectPrinter();
               printStruk(myModel.Prop1);
           }

           function detectPrinter() {
               var applet = document.jZebra;
               if (applet != null) {
                   applet.findPrinter("ZDesigner GX430t");
                   while (!applet.isDoneFinding()) {
                       // Wait
                   }
                   var ps = applet.getPrintService();
                   if (ps == null) var info = "Printer belum siap";
                   else var info = "Printer \"" + ps.getName() + "\" siap";
               }
               else
                   var info = "Java Runtime belum siap!";
               document.getElementById("printerStatusBar").innerHTML = info;
               window.setTimeout('detectPrinter()', 5000);
           }
           function writetoelement(str) {
               var test = str;
               var test1 = "[";
               var test = str.replace(test, test1);
               var test1 = "]";
               var test = str.replace(test, test1);
               document.getElementById("struk").value = test;//
               printStruk(document.getElementById("struk").value);
           }

           function testx() {

           }

           function printStruk(str) {
               window.setTimeout(testx, 5000);
               //   detectPrinter();
               var applet = document.jZebra;

               str = returnEnter(str);
               applet.append(str);
               // Send to the printer
               applet.print();

               if (applet != null) {
                   // Plain Text
                   str = returnEnter(str);
                   applet.append(str);
                   // Send to the printer
                   applet.print();
                   alert("printing now");
                   alert(str);
                   while (!applet.isDonePrinting()) {
                       // Wait
                   }
                   var e = applet.getException();
                   if (e == null) var info = "Printed Successfully";
                   else var info = "Error: " + e.getLocalizedMessage();
               }
               else {
                   var info = "Printer belum siap";
               }
               document.getElementById("printerStatusBar").innerHTML = info;
           }

           function returnEnter(dataStr) {
               return dataStr.replace(/(\r\n|\r|\n)/g, "\n");
           }
    </script>



    <form id="barcode" runat="server">
    <div>
    
        <table style="width:100%;">
            <tr>
                <td class="auto-style3">&nbsp;</td>
                <td class="auto-style2">
                    <asp:Label ID="Label3" runat="server" Font-Names="Segoe Print" Font-Size="Larger" Text="Saw Barcode Label System"></asp:Label>
                </td>
                <td class="auto-style1">
                    <asp:Label ID="Label4" runat="server" Font-Names="Code 128" Font-Size="Larger" Text="Saw Barcode Label System"></asp:Label>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style3">&nbsp;</td>
                <td class="auto-style1" colspan="2">&nbsp;</td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style3">
                    <asp:Label ID="Label1" runat="server" Text="BADGE ID"></asp:Label>
                </td>
                <td class="auto-style1" colspan="2">
                    <asp:TextBox ID="txtBadgeID" runat="server" BackColor="#FFFF99" Height="34px" Width="377px"></asp:TextBox>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style3">
                    <asp:Label ID="Label2" runat="server" Text="SHOP ORDER"></asp:Label>
                </td>
                <td class="auto-style1" colspan="2">
                    <asp:TextBox ID="txtSOID" runat="server" BackColor="#FFFF99" Height="28px" Width="374px"></asp:TextBox>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style3">
                    &nbsp;</td>
                <td class="auto-style1" colspan="2">
                    <asp:Button ID="btnPrint" runat="server" Height="35px" Text="Submit" Width="112px"  OnClick="btnPrint_Click3" />
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>

                <td>
                    <asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>
                </td>
            </tr>



        </table>
    
    </div>
    </form>



</body>

</html>
