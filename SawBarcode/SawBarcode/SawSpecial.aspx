<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="SawSpecial.aspx.cs" Inherits="SawSpecial.Saw" %>

<!DOCTYPE html>




<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <style type="text/css">
        .auto-style1 {
            width: 962px;
        }
        .auto-style3 {
        }
        .auto-style4 {
            width: 137px;
        }
        #Select1 {
            width: 384px;
            height: 21px;
        }
        .auto-style6 {
            width: 48px;
        }
        .auto-style7 {
            width: 124px;
        }
        .auto-style8 {
            width: 2960px;
        }
        .auto-style9 {
            width: 20px;
        }
        .auto-style11 {
        }
        .auto-style12 {
            height: 39px;
        }
        .auto-style13 {
            width: 137px;
            height: 39px;
        }
        .auto-style14 {
            width: 962px;
            height: 39px;
        }
    </style>
</head>
<body>


    <script type="text/javascript" src="js/jzebra.js"></script>
    <body style="background-color:#E6E6FA" onload="detectPrinter()">



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
                   if (ps == null) var info = "Printer Not Ready";
                   else var info = "Printer \"" + ps.getName() + "\" is ready";
               }
               else
                   var info = "Java Runtime not ready!";
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
               event.preventDefault();
               window.setTimeout(testx, 5000);
               //   detectPrinter();
               var applet = document.jZebra;

              // str = returnEnter(str);
              // applet.append(str);
               // Send to the printer
              // applet.print();

               if (applet != null) {
                   // Plain Text
                   str = returnEnter(str);
                   applet.append(str);
                   // Send to the printer
                   alert("press to print");
                  
                       applet.print();
                       //alert(str);
                       while (!applet.isDonePrinting()) {
                           // Wait
                       
                       var e = applet.getException();
                       if (e == null) var info = "Printed Successfully";
                       else var info = "Error: " + e.getLocalizedMessage();
                   }

               }
               else {
                   var info = "Printer is not ready";
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
                <td class="auto-style3" colspan="7" style="text-align: center"  bgcolor="#42f49b">

                    <asp:Label ID="Label3" runat="server" Font-Names="Arial" Font-Size="XX-Large" Text="Saw Barcode Label System" ForeColor="#6699FF"></asp:Label>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style11">&nbsp;</td>
                <td class="auto-style4">&nbsp;</td>
                <td class="auto-style8">&nbsp;</td>
                <td class="auto-style6">Scan</td>
                <td class="auto-style7">Success</td>
                <td class="auto-style9">Error</td>
                <td class="auto-style1">&nbsp;</td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style11">
                    &nbsp;</td>
                <td class="auto-style4">
                    <asp:Label ID="Label1" runat="server" Text="BADGE ID"></asp:Label>
                </td>
                <td class="auto-style8">
                    <asp:TextBox ID="txtBadgeID" runat="server" BackColor="#FFFF99" Height="34px" Width="377px"></asp:TextBox>
                </td>
                <td class="auto-style6" rowspan="3">
                    <asp:ListBox ID="lstOCRID" runat="server" Height="149px" Width="132px"></asp:ListBox>
                </td>
                <td class="auto-style7" rowspan="3">
                    <asp:ListBox ID="lstOCRIDSuccess" runat="server" Height="149px" Width="132px"></asp:ListBox>
                </td>
                <td class="auto-style9" rowspan="3">
                    <asp:ListBox ID="lstOCRIDError" runat="server" Height="149px" Width="132px"></asp:ListBox>
                </td>
                <td class="auto-style1" rowspan="3">
                    <asp:ListBox ID="lstOCRIDAll" runat="server" Height="149px" Visible="False" Width="132px"></asp:ListBox>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style11">
                    &nbsp;</td>
                <td class="auto-style4">
                    <asp:Label ID="Label2" runat="server" Text="SHOP ORDER"></asp:Label>
                </td>
                <td class="auto-style8">
                    <asp:TextBox ID="txtSOID" runat="server" BackColor="#FFFF99" Height="34px" Width="377px"></asp:TextBox>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style11">
                    &nbsp;</td>
                <td class="auto-style4">
                    OCR SCAN</td>
                <td class="auto-style8">
                    <asp:TextBox ID="txt2DScan" runat="server" BackColor="#FFFF99" Height="34px" Width="377px" OnTextChanged="txt2DScan_TextChanged"></asp:TextBox>
                    <asp:Button ID="btnAddOCRID" runat="server" OnClick="btnAddOCRID_Click" Text="Add" />
                </td>
                <td>
                    <asp:Button ID="Button1" runat="server" OnClick="Button1_Click" Text="Button" />
                </td>
            </tr>
            <tr>
                <td class="auto-style11">
                    &nbsp;</td>
                <td class="auto-style4">
                    &nbsp;</td>
                <td class="auto-style8">
                    <asp:Label ID="Label6" runat="server" Font-Size="Small" ForeColor="Red" Text="For ICN6 and XXXX Device (SCAN HERE)"></asp:Label>
                </td>
                <td class="auto-style6">
                    <asp:Button ID="btnDelete" runat="server" OnClick="btnDelete_Click" Text="Delete" Width="74px" />
                </td>
                <td class="auto-style7" >
                    <applet name="jZebra" code="jzebra.RawPrintApplet.class" archive="js/jzebra.jar"  style="height: 20px; width: 20px">
                        <param name="printer" value="zebra">
                        <param name="sleep" value="200">
                    </applet></td> <td colspan="2" ><span id="printerStatusBar">Loading...</span></td>
                <td class="auto-style1">
                    <asp:Button ID="btnAdd" runat="server" OnClick="btnAdd_Click" Text="Add" Width="74px" Visible="False" />
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style11" colspan="7" bgcolor="#42f49b">
                    &nbsp;</td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style11">
                    &nbsp;</td>
                <td class="auto-style4">
                    <asp:Label ID="Label4" runat="server" Text="BIMLINE"></asp:Label>
                </td>
                <td class="auto-style1" colspan="5">
                    <asp:Label ID="Label5" runat="server" Font-Size="Small" ForeColor="Red" Text="Please Select BimLine"></asp:Label>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style12">
                    </td>
                <td class="auto-style13">
                    </td>
                <td class="auto-style14" colspan="5">
                    <asp:Panel ID="Panel1" runat="server">
                        <asp:DropDownList ID="cbBimline" runat="server" OnSelectedIndexChanged="cbBimline_SelectedIndexChanged">
                        </asp:DropDownList>
                        <asp:Button ID="btnPrint" runat="server" Height="35px" OnClick="btnPrint_Click3" Text="Submit" Width="112px" />
                        <asp:Button ID="btnTest" runat="server" OnClick="btnTest_Click" Text="Test" Visible="False" />
                    </asp:Panel>
                </td>
                <td class="auto-style12"></td>
            </tr>
            <tr>
                <td class="auto-style11">
                    &nbsp;</td>
                <td class="auto-style4">
                    &nbsp;</td>
                <td class="auto-style1" colspan="5">
                        <asp:PlaceHolder ID="PlaceHolder1" runat="server"></asp:PlaceHolder>
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>

                <td class="auto-style11">
                    &nbsp;</td>

                <td class="auto-style4">
                    &nbsp;</td>
            </tr>



        </table>
    
    </div>
    </form>



</body>

</html>
