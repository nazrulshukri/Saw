<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Saw.aspx.cs" Inherits="SawBarcode.Saw" %>

<!DOCTYPE html>




<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8"/>
	<meta http-equiv="X-UA-Compatible" content="IE=edge"/>
	<meta name="viewport" content="width=device-width, initial-scale=1"/>
    <title></title>
    <style type="text/css">
        .auto-style1 {
            width: 962px;
        }
        .auto-style3 {
            background-color: #42f49b;
        }
        .auto-style4 {
            width: 137px;
        }
        #Select1 {
            width: 384px;
            height: 21px;
        }
        .auto-style6 {
            width: 62px;
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
    </style>
</head>
<%--<body>--%>


    <body style="background-color:#E6E6FA" onload="detectPrinter()">
    <script type="text/javascript" src="js/jzebra.js"></script>


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

           function mapcodegenerate(element) {

               //alert(document.getElementById(element.id).parentElement.parentElement.querySelectorAll('[Qmapcode]')['0'].attributes['Qmapcode'].nodeValue);

               //alert(document.getElementById(element.id).parentElement.parentElement.querySelectorAll('[Hmapcode]')['0'].attributes['Hmapcode'].nodeValue);

               var finalstr = "";
               var rdbQ = document.getElementById(element.id).parentElement.parentElement.querySelectorAll('[Qmapcode]')['0'];
               var rdbQ = document.getElementById(element.id).parentElement.parentElement.querySelectorAll('[Qquantity]')['0'];

               var rdbH = document.getElementById(element.id).parentElement.parentElement.querySelectorAll('[Hmapcode]')['0'];
               if (rdbQ != null && rdbQ != undefined && rdbQ.childNodes['0'].checked) {
                   var Qmap = rdbQ.attributes['Qmapcode'].nodeValue;
                   var Qmap1 = new RegExp(Qmap, 'g');
                   var Qqtyrep = rdbQ.attributes['Qquantity'].nodeValue;

                   var Qqty = rdbQ.attributes['quaterqty'].nodeValue;

                   var qarry = Qqty.split(";");

                   for (var i = 1; i <= 4; i++) {
                       finalstr += element.attributes['Bmapcode'].nodeValue.replace(Qmap1, "Q" + i + "__" + Qmap).replace(Qqtyrep, qarry[i - 1].split(":")[1]), "\n";

                       //var substr = finalstr.substr(finalstr.indexOf("FD(Q)"), finalstr.length);
                       //var str = finalstr.substring(finalstr.indexOf("FD(Q)") + 6, finalstr.indexOf("FS"));
                       ////var qty1 = new RegExp("^FD(Q)" + str + "^FS^", 'g');
                       //finalstr = finalstr.replace(qarry[i - 1].split(":")[1]);
                   }
                   return finalstr;
                   //finalstr += finalstr;
               }
               else if (rdbH != null && rdbH != undefined && rdbH.childNodes['0'].checked) {
                   var Hmap = rdbH.attributes['Hmapcode'].nodeValue;//rdbH.attributes['Hmapcode'].nodeValue;
                   var Hmap1 = new RegExp(Hmap, 'g');

                   var Hhalfrep = rdbH.attributes['Hquantity'].nodeValue;

                   var Hqty = rdbH.attributes['halfqty'].nodeValue;

                   var Harry = Hqty.split(";");
                   for (var i = 1; i <= 2; i++) {
                       finalstr += element.attributes['Bmapcode'].nodeValue.replace(Hmap1, "H" + i + "__" + Hmap).replace(Hhalfrep, Harry[i - 1].split(":")[1]), "\n";
                   }

                   return finalstr;
                   //str += finalstr;
               }
               else {
                   return null;
               }

           }

           function printStruk(str, btnid) {
               
               element = document.getElementById(btnid);
               //event.preventDefault();
               //alert(testx);
               //window.setTimeout(testx, 5000);
               //alert("Start");
               detectPrinter();
               //alert("done");

               var applet = document.jZebra;
               if (element != null && element.id != "btnPrintAll") {
                   var res = mapcodegenerate(element);
                   if (res != null) {
                       str = res;
                   }

               }
               else if (element != null && document.getElementById(element.id).parentElement.querySelectorAll('input:checked').length > 0)// (document.getElementById(element.id).parentElement.parentElement.querySelectorAll('[checked]').length > 0)
               {
                   debugger;
                   var buttons = document.getElementById(element.id).parentElement.parentElement.querySelectorAll('[Bmapcode]');
                   var finalstr = "";
                   for (var item in buttons) {
                       var btnelement = buttons[item];

                       if (document.getElementById(btnelement.id) == undefined) {
                           continue;
                       }
                       var res = mapcodegenerate(btnelement);
                       if (res != null) {
                           finalstr += res, "\n";
                       }
                       else {
                           finalstr += btnelement.attributes['Bmapcode'].nodeValue, "\n";
                       }
                   }
                   str = finalstr;
               }

               // str = returnEnter(str);
               // applet.append(str);
               // Send to the printer
               // applet.print();

               if (applet != null) {
                   // Plain Text
                   //debugger;
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
               //debugger;
               return dataStr.replace(/("\|\r\n|\r|\n|\")/g, "\n");
           }
           function myFab() {
               var txt;
               if (confirm("This is DHAM wafer, OCR has dashed?")) {
                   txt = "You pressed Y, has dashed";
               } else {
                   txt = "You pressed N, no dashed";
               }
               document.getElementById(inpHide).value = txt;
           }

           function DeleteData() {
               $.ajax({
                   type: "POST",
                   url: "Saw.aspx",
                   data: "{}",
                   contentType: "application/json; charset=utf-8",
                   dataType: "json",
                   success: function (msg) {
                       alert("Your function called sucessfully");
                       //if your Codebehind method return something then that data or result will be accessed using .d attribute of msg
                       //like this...

                       //alert("Your method return data is ..." + msg.d);
                   },
                   error: function (msg) {
                       alert("There's some error in calling your function.");
                   }
               });
               return false;
           }


    </script>


    <form id="barcode" runat="server">
    <div>
    
        <table style="width:100%;">
            <tr>
                <td class="auto-style3" colspan="7" style="text-align: center"> <%--bgcolor="#42f49b"--%>

                    <asp:Label ID="Label3" runat="server" Font-Names="Arial" Font-Size="XX-Large" Text="Saw Barcode Label System   V2019.2.6" ForeColor="#FF0066"></asp:Label>
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
                    <asp:ListBox ID="lstOCRID" runat="server" Height="149px" Width="132px" OnSelectedIndexChanged="lstOCRID_SelectedIndexChanged"></asp:ListBox>
                </td>
                <td class="auto-style7" rowspan="3">
                    <asp:ListBox ID="lstOCRIDSuccess" runat="server" Height="149px" Width="132px"></asp:ListBox>
                </td>
                <td class="auto-style9" rowspan="3">
                    <asp:ListBox ID="lstOCRIDError" runat="server" Height="149px" Width="132px" OnSelectedIndexChanged="lstOCRIDError_SelectedIndexChanged"></asp:ListBox>
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
                              
                    <div id="div1">
                    <div id="div2"><input id="inpHide" type="hidden" runat="server" /> </div>
                    </div>             
                    <asp:TextBox ID="hiddenInput" runat="server" BackColor="#FFFF99" Height="16px" Width="16px" ></asp:TextBox>
                </td>
                <td>&nbsp;</td>
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
                    <asp:Label ID="lblScanQty" runat="server" Font-Size="Smaller"></asp:Label>
                </td>
                <td class="auto-style7" >
                    <applet name="jZebra" code="jzebra.RawPrintApplet.class" archive="js/jzebra.jar"  style="height: 20px; width: 20px">
                        <param name="printer" value="zebra"/>
                        <param name="sleep" value="200"/>
                    </applet></td> <td colspan="2" ><span id="printerStatusBar">Loading...</span></td>
                <td class="auto-style1">
                    <asp:Button ID="btnAdd" runat="server" OnClick="btnAdd_Click" Text="Add" Width="74px" Visible="False" />
                </td>
                <td>&nbsp;</td>
            </tr>
            <tr>
                <td class="auto-style3" colspan="7" > <%--bgcolor="#42f49b"--%>
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
                <td class="auto-style11">
                    &nbsp;</td>
                <td class="auto-style4">
                    &nbsp;</td>
                <td class="auto-style1" colspan="5">
                    <asp:Panel ID="Panel1" runat="server">
                        <asp:DropDownList ID="cbBimline" runat="server" OnSelectedIndexChanged="cbBimline_SelectedIndexChanged">
                        </asp:DropDownList>
                        <asp:Button ID="btnPrint" runat="server" Height="35px" OnClick="btnPrint_Click" Text="Submit" Width="112px" />
                        
                        <%--<asp:Button ID="btnTest" runat="server" OnClick="btnTest_Click" Text="Test" />--%>
                        <%--<asp:Button ID="btnTest1" runat="server" OnClick="btnTest1_Click" Text="Test1" />--%>
                        <asp:Button ID="btnDHAMDashYes" runat="server" OnClick="btnDHAMDashYes_Click" Text="DHAM &quot;DASH&quot;" Visible="False" />
                        <asp:Button ID="btnDHAMDashNo" runat="server" Text="DHAM &quot;NODASH&quot;" Visible="False" />
                        <asp:Button ID="bSearch" runat="server" Height="35px" OnClick="btnPrint_Click" Text="Search" Width="112px" Visible="false"/>

                        <asp:GridView ID="Gv1" runat="server" AlternatingRowStyle-BackColor="#e6f7ff" AutoGenerateColumns="false" Font-Names="Arial" Font-Size="11pt" HeaderStyle-BackColor="#66ccff" OnRowDataBound="Gv1_RowDataBound"><%--OnRowCommand="Gv1_RowCommand"--%>
                                <Columns>
                                    <asp:BoundField DataField="Id" HeaderText="Id" ItemStyle-Width="50px" />
                                    <asp:BoundField DataField="MWO" HeaderText="MWO" ItemStyle-Width="50px" />
                                    <asp:BoundField DataField="Fab" HeaderText="Fab" ItemStyle-Width="100px" />
                                    <asp:BoundField DataField="WaferBatch" HeaderText="WaferBatch" ItemStyle-Width="150px" />
                                    <asp:BoundField DataField="WaferDescription" HeaderText="WaferDescription" ItemStyle-Width="150px" />
                                    <asp:BoundField DataField="Quantity" HeaderText="Quantity" ItemStyle-Width="100px" />
                                    <asp:TemplateField HeaderText="Quater">
                                        <ItemTemplate>
                                            <%--<asp:CheckBox ID="chkQ" runat="server" Width="100px" Text="Quarter" OnCheckedChanged="chkQ_CheckedChanged" AutoPostBack="true" />--%>
                                            <asp:RadioButton ID="rdbQ"  runat="server"  GroupName="RegulaQH" Text="Quarter" Width="100px" DataTextField="Quarter" AutoPostBack="true" OnCheckedChanged="rdbQ_CheckedChanged"/><%--AutoPostBack="false"--%>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Half">
                                        <ItemTemplate>
                                            <%--<asp:CheckBox ID="chkH" runat="server" Width="100px" Text="Half" OnCheckedChanged="chkH_CheckedChanged" AutoPostBack="true" />--%>
                                            <asp:RadioButton ID="rdbH" runat="server" AutoPostBack="true" GroupName="RegulaQH" Text="Half" Width="100px" DataTextField="Half" OnCheckedChanged="rdbH_CheckedChanged"/><%----%>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <asp:TemplateField HeaderText="Print">
                                        <ItemTemplate>
                                            <%--<asp:ButtonField ButtonType="Button" CommandName="ExecuteRowPrt" DataTextField="Print" onclick/>--%>
                                            <asp:Button ID="btnprint"  runat="server"  CommandName="ExecuteRowPrt" DataTextField="Print" OnClick="btnprint_Click1" /><%-- UseSubmitBehavior="true" --%>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                    <%--<asp:BoundField ItemStyle-Width = "150px" DataField = "Quarter" HeaderText = "Quarter" />
                                <asp:BoundField ItemStyle-Width = "150px" DataField = "Half" HeaderText = "Half" />
                                <asp:BoundField ItemStyle-Width = "150px" DataField = "Print" HeaderText = "Print" />--%>
                                </Columns>
                            </asp:GridView>
                        <asp:Button ID="btnPrintAll" runat="server" Text="PrintAll" UseSubmitBehavior="false" Visible="false" OnClick="btnPrintAll_Click"/>
                        <br />
                        <asp:Label ID="lbErr" runat="server"></asp:Label>
                        <br />
                        <asp:GridView ID="Gv2" runat="server" AlternatingRowStyle-BackColor="#e6f7ff" 
                            Font-Names="Arial" Font-Size="11pt" HeaderStyle-BackColor="#66ccff">
                            <AlternatingRowStyle BackColor="#E6F7FF" />
                            <HeaderStyle BackColor="#66CCFF" />
                        </asp:GridView>
                        <br />
                        <br />
                    </asp:Panel>
                </td>
                <td>&nbsp;</td>
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
