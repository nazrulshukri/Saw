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

           // ===== Printer detection for jZebra (Java) =====
           // No printer name is hardcoded: once Java has started, jZebra lists the printers
           // on this PC and the Zebra one (or the one picked last time) is used.
           // The page never calls the applet before Java says it is ready (jzebraReady) and
           // never loops waiting on it, so it does not hang while the Java "Run" prompt is up.
           // Labels are queued and printed as soon as Java and the printer are ready.
           var ZEBRA_HINTS = ["zdesigner", "zebra", "zpl"];   // only used to auto-pick the printer
           var PRINTER_COOKIE = "SawPrinter";                 // printer picked on this PC
           var jz = {
               ready: false,       // jZebra has started (jzebraReady was called)
               finding: false,     // printer search running
               printing: false,    // label sent, waiting for jZebra to finish
               printers: [],       // printer names found by jZebra
               printerName: null,  // printer used for labels
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

           function initPrinter() {
               holdPostBack();
               window.setTimeout(function () {
                   if (!jz.ready) setPrinterStatus("Java has not started. Click \"Run\" on the Java prompt, or check Java is installed and allowed for this site, then reload.");
               }, 30000);
           }

           // --- called by jZebra (Java) ---
           function jzebraReady() {
               jz.ready = true;
               window.setTimeout(detectPrinter, 0);   // return to Java first, then use the applet
           }

           function jzebraDoneFinding() {
               window.setTimeout(printersFound, 0);
           }

           function jzebraDonePrinting() {
               window.setTimeout(printDone, 0);
           }

           // Search the printers on this PC. Does not wait: the result comes in printersFound().
           function detectPrinter() {
               if (!jz.ready) {
                   setPrinterStatus("Waiting for Java... click \"Run\" if Java asks");
                   return;
               }
               if (jz.finding) return;
               jz.finding = true;
               setPrinterStatus("Searching printers...");
               try {
                   // any search makes jZebra load the full printer list
                   document.jZebra.findPrinter(readSavedPrinter() || ZEBRA_HINTS[0]);
               }
               catch (e) {
                   jz.finding = false;
                   setPrinterStatus("Java error: " + htmlEncode(e.message));
                   return;
               }
               window.setTimeout(checkFinding, 500);
           }

           // Backup in case the jzebraDoneFinding callback does not arrive
           function checkFinding() {
               if (!jz.finding) return;
               var done = true;
               try { done = document.jZebra.isDoneFinding(); } catch (e) { }
               if (done) printersFound();
               else window.setTimeout(checkFinding, 500);
           }

           function printersFound() {
               if (!jz.finding) return;   // already handled
               jz.finding = false;

               var list = [];
               try {
                   var names = document.jZebra.getPrinters();
                   if (names != null) {
                       names = String(names).split(",");
                       for (var i = 0; i < names.length; i++)
                           if (names[i] != "") list.push(names[i]);
                   }
               }
               catch (e) { }
               jz.printers = list;

               // Printer picked last time on this PC, else the first Zebra printer found
               var index = -1;
               var saved = readSavedPrinter();
               for (var i = 0; i < list.length; i++) {
                   if (list[i] == saved) { index = i; break; }
               }
               for (var h = 0; index < 0 && h < ZEBRA_HINTS.length; h++) {
                   for (var i = 0; i < list.length; i++) {
                       if (list[i].toLowerCase().indexOf(ZEBRA_HINTS[h]) >= 0) { index = i; break; }
                   }
               }

               var sel = document.getElementById("printerList");
               if (sel != null) {
                   sel.options.length = 0;
                   sel.options[0] = new Option("-- select printer --", "-1");
                   for (var i = 0; i < list.length; i++)
                       sel.options[sel.options.length] = new Option(list[i], String(i));
                   sel.selectedIndex = index + 1;
               }
               usePrinter(index);
           }

           // Operator picked a printer from the list
           function choosePrinter(sel) {
               var index = parseInt(sel.value, 10);
               if (index >= 0) savePrinter(jz.printers[index]);
               usePrinter(index);
           }

           function usePrinter(index) {
               jz.printerName = null;
               if (index < 0 || index >= jz.printers.length) {
                   if (jz.printers.length == 0) setPrinterStatus("No printer found on this PC");
                   else setPrinterStatus("Printer Not Ready - select the Zebra printer from the list");
                   return;
               }
               try {
                   document.jZebra.setPrinter(index);
               }
               catch (e) {
                   setPrinterStatus("Java error: " + htmlEncode(e.message));
                   return;
               }
               jz.printerName = jz.printers[index];
               setPrinterStatus("Printer \"" + htmlEncode(jz.printerName) + "\" is ready");
               processQueue();
           }

           // Print the next queued label once Java and the printer are ready
           function processQueue() {
               if (jz.printing || jz.finding) return;
               if (jz.queue.length == 0) {
                   releasePostBack();
                   return;
               }
               if (!jz.ready) {
                   setPrinterStatus("Waiting for Java... click \"Run\" if Java asks. The label will print after that.");
                   return;
               }
               if (jz.printerName == null) {
                   setPrinterStatus("Printer Not Ready - select the Zebra printer from the list to print");
                   return;
               }

               var data = jz.queue.shift();
               var applet = document.jZebra;
               // Send to the printer
               alert("press to print");
               try {
                   applet.clearException();
                   applet.clear();
                   applet.append(data);
                   applet.print();
               }
               catch (e) {
                   printFailed("Error: " + htmlEncode(e.message));
                   return;
               }
               jz.printing = true;
               setPrinterStatus("Printing to \"" + htmlEncode(jz.printerName) + "\"...");
               window.setTimeout(checkPrinting, 500);
           }

           // Backup in case the jzebraDonePrinting callback does not arrive
           function checkPrinting() {
               if (!jz.printing) return;
               var done = true;
               try { done = document.jZebra.isDonePrinting(); } catch (e) { }
               if (done) printDone();
               else window.setTimeout(checkPrinting, 500);
           }

           function printDone() {
               if (!jz.printing) return;   // already handled
               jz.printing = false;

               var error = null;
               try {
                   if (document.jZebra.getException() != null)
                       error = String(document.jZebra.getExceptionMessage());
               }
               catch (e) { }

               if (error != null) {
                   printFailed("Error: " + htmlEncode(error));
                   return;
               }
               setPrinterStatus("Printed Successfully");
               processQueue();   // next label, then the held postback
           }

           // Same as before: when printing fails the page does not post back
           function printFailed(info) {
               jz.queue = [];
               jz.postBack = null;
               setPrinterStatus(info);
           }

           // A postback reloads the page and stops Java, so hold it (e.g. PrintAll)
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

               // Queue the label; processQueue() sends it with jZebra as soon as
               // Java and the printer are ready (no waiting loop here)
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
                    <%-- no "printer" param: the printer is searched by the page (detectPrinter) after Java is ready --%>
                    <applet name="jZebra" code="jzebra.RawPrintApplet.class" archive="js/jzebra.jar" mayscript="mayscript" style="height: 20px; width: 20px">
                        <param name="sleep" value="200"/>
                    </applet></td> <td colspan="2" ><span id="printerStatusBar">Waiting for Java... click "Run" if Java asks</span><br />
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
