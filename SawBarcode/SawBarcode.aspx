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


    <script type="text/javascript" src="js/zebra-print.js"></script>
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
                    &nbsp;
                </td>
            </tr>
        </table>

       <script type="text/javascript" >

           function foo(myModel) {
               alert(myModel.Prop1);
               detectPrinter();
               printStruk(myModel.Prop1);
           }

           // Printing via Zebra Browser Print, see js/zebra-print.js (no Java)
           var printerPoll = null;

           function setPrinterStatus(info) {
               document.getElementById("printerStatusBar").innerHTML = info;
           }

           function detectPrinter() {
               if (typeof ZebraPrint === "undefined") {
                   setPrinterStatus("js/zebra-print.js not found on the server");
                   return;
               }
               ZebraPrint.detect(function (r) {
                   setPrinterStatus(r.message);
                   if (printerPoll != null) window.clearTimeout(printerPoll);
                   printerPoll = window.setTimeout(detectPrinter, 5000);
               });
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
               if (typeof ZebraPrint === "undefined") {
                   setPrinterStatus("js/zebra-print.js not found on the server");
                   return;
               }
               // Waits until the data is sent (like the old applet call), then shows the result
               str = returnEnter(str);
               setPrinterStatus(ZebraPrint.print(str).message);
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
