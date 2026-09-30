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


    <body style="background-color:#E6E6FA" onload="initPrinter()">


       <script type="text/javascript" >

           function foo(myModel) {
               alert(myModel.Prop1);
               detectPrinter();
               printStruk(myModel.Prop1);
           }

           // ===== Printer detection with Zebra Browser Print (no Java) =====
           // A web page cannot see USB printers by itself. Zebra Browser Print (free, installed
           // once on each operator PC) runs on the PC at http://127.0.0.1:9100 and gives the page
           // the list of Zebra printers (USB / driver / network). The page picks the Zebra
           // automatically - no printer name is hardcoded - and sends the ZPL label to it.
           var BROWSER_PRINT_URL = "http://127.0.0.1:9100/";
           var PRINTER_COOKIE = "SawPrinter";                 // printer picked on this PC
           var jz = {
               ready: false,       // Browser Print answered
               finding: false,     // printer search running
               printing: false,    // label being sent
               printers: [],       // Browser Print devices found on this PC
               printer: null,      // device used for labels
               queue: [],          // labels waiting to be printed
               doPostBack: null,   // original ASP.NET __doPostBack
               postBack: null      // postback held until the labels are printed
           };

           function setPrinterStatus(info) {
               var bar = document.getElementById("printerStatusBar");
               if (bar != null) bar.innerHTML = info;
           }

           function htmlEncode(s) {
               return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;");
           }

           function readSavedPrinter() {
               var parts = document.cookie.split(";");
               for (var i = 0; i < parts.length; i++) {
                   var kv = parts[i].replace(/^\s+/, "");
                   if (kv.indexOf(PRINTER_COOKIE + "=") == 0)
                       return decodeURIComponent(kv.substring(PRINTER_COOKIE.length + 1));
               }
               return null;
           }

           function savePrinter(name) {
               var expires = new Date();
               expires.setFullYear(expires.getFullYear() + 1);
               document.cookie = PRINTER_COOKIE + "=" + encodeURIComponent(name) + "; expires=" + expires.toUTCString() + "; path=/";
           }

           // Call Browser Print on this PC. text/plain keeps it a simple CORS request.
           function browserPrint(method, path, body, success, failure) {
               var xhr = new XMLHttpRequest();
               xhr.open(method, BROWSER_PRINT_URL + path, true);
               xhr.timeout = 15000;
               if (body != null) xhr.setRequestHeader("Content-Type", "text/plain;charset=UTF-8");
               xhr.onreadystatechange = function () {
                   if (xhr.readyState != 4) return;
                   if (xhr.status == 200) success(xhr.responseText);
                   else failure(xhr.status == 0 ? "Zebra Browser Print is not running on this PC" : "Browser Print error " + xhr.status + " " + xhr.responseText);
               };
               xhr.ontimeout = function () { failure("Zebra Browser Print did not answer"); };
               xhr.send(body);
           }

           function initPrinter() {
               holdPostBack();
               detectPrinter();
           }

           // Search the Zebra printers on this PC (does not block the page)
           function detectPrinter() {
               if (jz.finding) return;
               jz.finding = true;
               setPrinterStatus("Searching printers...");
               browserPrint("GET", "available", null, function (text) {
                   jz.finding = false;
                   jz.ready = true;
                   var list = [];
                   try {
                       var result = JSON.parse(text);
                       var devices = result.printer || [];
                       for (var i = 0; i < devices.length; i++)
                           if (devices[i] && devices[i].name) list.push(devices[i]);
                   }
                   catch (e) { }
                   printersFound(list);
               }, function (err) {
                   jz.finding = false;
                   jz.ready = false;
                   printersFound([]);
                   setPrinterStatus(htmlEncode(err) + ". Install / start Zebra Browser Print, then click \"Find printers\".");
               });
           }

           function printersFound(list) {
               jz.printers = list;

               // Printer picked last time on this PC, else USB Zebra first, else the first one found
               var index = -1;
               var saved = readSavedPrinter();
               for (var i = 0; i < list.length; i++)
                   if (list[i].name == saved || list[i].uid == saved) { index = i; break; }
               for (var i = 0; index < 0 && i < list.length; i++)
                   if (String(list[i].connection).toLowerCase() == "usb") index = i;
               if (index < 0 && list.length > 0) index = 0;

               var sel = document.getElementById("printerList");
               if (sel != null) {
                   sel.options.length = 0;
                   sel.options[0] = new Option("-- select printer --", "-1");
                   for (var i = 0; i < list.length; i++)
                       sel.options[sel.options.length] = new Option(list[i].name + " (" + list[i].connection + ")", String(i));
                   sel.selectedIndex = index + 1;
               }
               if (jz.ready) usePrinter(index);
           }

           // Operator picked a printer from the list
           function choosePrinter(sel) {
               var index = parseInt(sel.value, 10);
               if (index >= 0) savePrinter(jz.printers[index].uid || jz.printers[index].name);
               usePrinter(index);
           }

           function usePrinter(index) {
               jz.printer = null;
               if (index < 0 || index >= jz.printers.length) {
                   if (jz.printers.length == 0) setPrinterStatus("No Zebra printer found on this PC. Check the USB cable / power, then click \"Find printers\".");
                   else setPrinterStatus("Printer Not Ready - select the Zebra printer from the list");
                   return;
               }
               jz.printer = jz.printers[index];
               setPrinterStatus("Printer \"" + htmlEncode(jz.printer.name) + "\" is ready");
               processQueue();
           }

           // Print the next queued label once the printer is ready
           function processQueue() {
               if (jz.printing || jz.finding) return;
               if (jz.queue.length == 0) {
                   releasePostBack();
                   return;
               }
               if (jz.printer == null) {
                   if (jz.ready) setPrinterStatus("Printer Not Ready - select the Zebra printer from the list to print");
                   return;
               }

               var data = jz.queue.shift();
               jz.printing = true;
               setPrinterStatus("Printing to \"" + htmlEncode(jz.printer.name) + "\"...");
               browserPrint("POST", "write", JSON.stringify({ device: jz.printer, data: data }), function () {
                   jz.printing = false;
                   setPrinterStatus("Printed Successfully");
                   processQueue();   // next label, then the held postback
               }, function (err) {
                   jz.printing = false;
                   printFailed("Error: " + htmlEncode(err));
               });
           }

           // When printing fails the page does not post back
           function printFailed(info) {
               jz.queue = [];
               jz.postBack = null;
               setPrinterStatus(info);
           }

           // A postback reloads the page, so hold it (e.g. PrintAll)
           // until the queued labels have been sent to the printer.
           function holdPostBack() {
               if (jz.doPostBack != null || typeof window.__doPostBack != "function") return;
               jz.doPostBack = window.__doPostBack;
               window.__doPostBack = function (eventTarget, eventArgument) {
                   if (jz.printing || jz.queue.length > 0) {
                       jz.postBack = [eventTarget, eventArgument];
                       return;
                   }
                   jz.doPostBack(eventTarget, eventArgument);
               };
           }

           function releasePostBack() {
               if (jz.postBack == null) return;
               var pb = jz.postBack;
               jz.postBack = null;
               jz.doPostBack(pb[0], pb[1]);
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
               //alert("done");

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

               // Queue the label; processQueue() sends it with Browser Print as soon as
               // the printer is found (no waiting loop here)
               jz.queue.push(returnEnter(str));
               holdPostBack();
               processQueue();
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

                    <asp:Label ID="Label3" runat="server" Font-Names="Arial" Font-Size="XX-Large" Text="Saw Barcode Label System   V2019.2.6.3" ForeColor="#FF0066"></asp:Label>
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
                    </td> <td colspan="2" ><span id="printerStatusBar">Searching printers...</span><br />
                    <select id="printerList" onchange="choosePrinter(this)" style="width: 250px"><option value="-1">-- select printer --</option></select>
                    <input type="button" value="Find printers" onclick="detectPrinter()" /></td>
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
