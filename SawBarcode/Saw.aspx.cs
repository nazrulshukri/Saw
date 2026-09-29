using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
//using System.Web.Services;
//using System.Web.Services.Protocols;
using System.Xml;
//using System.Xml.Serialization;
using System.Data.OracleClient;
using System.Data.SqlClient;
using System.Data;
//using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using Microsoft.VisualBasic;
//using System.ComponentModel;
//using System.Threading;
//using System.Web;
//using System.Web.Security;
//using System.Web.SessionState;
using System.Text;
//using Tools;
//using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
//using SawBarcode.camstarProd;
//using System.Windows;
//using System.Windows.Forms;
//using System.Xml.Linq;
using System.Net.Sockets;

namespace SawBarcode
{
    public partial class Saw : System.Web.UI.Page
    {
        #region List Variables

        //for wafermap
        List<string> crystalDmanExclude = new List<string>();  //to make exclusion for ink die
        List<string> crystaldescdB = new List<string>();
        List<string> crystaldesc = new List<string>();
        List<string> crystal = new List<string>();
        List<string> RecipedB = new List<string>();
        List<string> RecipedBExtra = new List<string>();
        List<string> MyBarcode = new List<string>();
        List<string> MyOCRID = new List<string>();
        List<string> Bin = new List<string>();
        List<string> TwinSI = new List<string>();

        //for saw printer
        List<string> PrinterSetting = new List<string>();
        List<string> lSOID = new List<string>();
        List<string> lDDATE = new List<string>();
        List<string> lCRYSTAL = new List<string>();
        List<string> lBIM = new List<string>();
        List<string> lQUANTITY = new List<string>();
        List<string> lBADGEID = new List<string>();
        List<string> lTOTALWAFER = new List<string>();
        List<string> lDIETYPE = new List<string>();
        List<string> lWAFERBATCH = new List<string>();
        List<string> lOCRID = new List<string>();
        List<string> hOCRID = new List<string>();
        List<string> lDEVICE = new List<string>();
        List<string> lPACKAGE = new List<string>();

        //support quarter
        List<string> lCRYSTALSAW = new List<string>();
        List<string> lQUANTITYQ1 = new List<string>();
        List<string> lQUANTITYQ2 = new List<string>();
        List<string> lQUANTITYQ3 = new List<string>();
        List<string> lQUANTITYQ4 = new List<string>();

        //for OCRID input
        List<string> lOCRIDInput = new List<string>();
        //for Enable/Disable ATBK map
        string EnaDisATBK = ConfigurationManager.AppSettings["EnaDisATBK"];
        string ZebraIPAddress = ConfigurationManager.AppSettings["ZebraIPAddress"];
        string ZebraDefaultPort = ConfigurationManager.AppSettings["ZebraDefaultPort"];

        #endregion Declare Variables

        public string myTotalBarcode = "";
        string SawPrefix = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            // cbBimline is null when the designer field is not wired to the markup
            // (old compiled page / designer out of date): look it up in the form instead
            if (cbBimline == null)
                cbBimline = FindControlRecursive(this, "cbBimline") as DropDownList;
            if (cbBimline == null)
            {
                Response.Write("<script>alert('Saw.aspx has no cbBimline dropdown. Replace Saw.aspx with the full file and rebuild.');</script>");
                return;
            }

            if (!IsPostBack)
            {
                cbBimline.Items.Clear();
                readBimLine();
            }
        }

        private static System.Web.UI.Control FindControlRecursive(System.Web.UI.Control root, string id)
        {
            if (root.ID == id)
                return root;
            foreach (System.Web.UI.Control child in root.Controls)
            {
                System.Web.UI.Control found = FindControlRecursive(child, id);
                if (found != null)
                    return found;
            }
            return null;
        }

        public void readBimLine()
        {
            var filePath1 = Server.MapPath("~/Config/BimLine.txt");
            const Int32 BufferSize = 1024;
            using (var fileStream = File.OpenRead(filePath1))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    try
                    {
                        cbBimline.Items.Add(line);
                    }
                    catch
                    {
 
                    }
                }

            }
        }

        #region Button Submit

        protected void btnPrint_Click(object sender, EventArgs e)
        {//submit
            string DataPath = "";
            string PrinterPath = "";
            string filepath1 = "";
            string template = "saw.LBL";
            string txtQty = "1";
            string hSOID = "FDG11262L";
            string hDDATE = "04-Apr-2017";
            string hCRYSTAL = "74LVC1G34/AY/8";
            string hOCRID = "A14142-03C4";
            string hBIM = "A01";
            string hQUANTITY = "10368";
            string hPRODUCT = "74LVC2G34GM";
            string hBADGEID = "B13009273";
            string hTOTALWAFER = "D06";
            string hDIETYPE = "TWIN";
            string RecipeExtra = "";

            lstOCRIDError.Items.Clear();
            lstOCRIDSuccess.Items.Clear();

            string [] FabInfo;

            //20260525-clear data GV1, lberr, add try catch showing message only.
            lbErr.Text = "";
            DataTable dt = new DataTable();
            Gv1.DataSource = dt;
            Gv1.DataBind();

            try
            {
                if (txtSOID.Text.ToString() != "") //2020Oct07 JB add validate MWO must be fill
                {
                    if (txtSOID.Text.ToString().Length == 10 && (txtSOID.Text.ToString().Substring(0, 1) == "E" || txtSOID.Text.ToString().Substring(0, 1) == "E" || txtSOID.Text.ToString().Substring(0, 1) == "P" || txtSOID.Text.ToString().Substring(0, 1) == "K"))
                    {
                        FabInfo = getWaferInfoCamstar(txtSOID.Text.ToString());
                    }
                    else
                    {
                        Response.Write("<script>alert('Warning: Shop Order PID/MWO invalid!');</script>");
                        return;

                        //FabInfo = getWaferInfo(txtSOID.Text.ToString());
                    }


                    string Fab = FabInfo[0];
                    string WaferDesc = FabInfo[3];
                    string WaferSize = FabInfo[4];

                    if (Fab == null || WaferDesc == null || WaferSize == null)
                    {
                        Response.Write("<script>alert('Warning: Error in Camstar Fab/WaferDesc/WaferSize');</script>");
                        return;
                    }
                    string Extra = getExtra(WaferDesc);

                    if (Fab == "ICN6" && WaferSize.ToString().Trim() == "6" && txt2DScan.Text.ToString().Length >= 1)
                    {
                        Response.Write("<script>alert('Warning: ICN6 and 6inch, Press ADD button to proceed');</script>");
                        return;
                    }
                    if (Fab == "ICN6" && WaferSize.ToString().Trim() == "6" && txt2DScan.Text.ToString().Length < 1 && lstOCRID.Items.Count < 1)
                    {
                        Response.Write("<script>alert('Warning: ICN6 and 6inch, Scan OCRID and press ADD button to proceed');</script>");
                        return;
                    }
                    if (Extra == "NOTInSAP" && txt2DScan.Text.ToString().Length >= 1)
                    {
                        Response.Write("<script>alert('Warning: Special Wafer from DHAM, press ADD button to proceed');</script>");
                        return;
                    }
                    if (Extra == "NOTInSAP" && txt2DScan.Text.ToString().Length < 1 && lstOCRID.Items.Count < 1)
                    {
                        Response.Write("<script>alert('Warning: Special Wafer from DHAM, Scan OCRID and press ADD button to proceed');</script>");
                        return;
                    }
                    if ((Extra != "NOTInSAP" && !(Fab == "ICN6" && WaferSize.ToString().Trim() == "6")) && txt2DScan.Text.ToString().Length >= 1)
                    {
                        Response.Write("<script>alert('Warning: Not ICN6, Not Special Wafer, Keep OCRID field blank');</script>");
                        return;
                    }
                    if (lblScanQty.Text == "")
                    {
                        lblScanQty.Text = "0";
                    }
                    if (txtSOID.Text.ToString().Length == 10 && (txtSOID.Text.ToString().Substring(0, 1) == "E" || txtSOID.Text.ToString().Substring(0, 1) == "E" || txtSOID.Text.ToString().Substring(0, 1) == "P" || txtSOID.Text.ToString().Substring(0, 1) == "K"))
                    {
                        if ((Convert.ToInt32(lblScanQty.Text) > 0) && Fab == "DHAM")
                        {
                            readTemplateSO3CamstarDHAM(DataPath, PrinterPath, "1", filepath1, txtBadgeID.Text, txtSOID.Text, template, WaferSize.ToString().Trim());
                        }
                        else
                        {
                            readTemplateSO3Camstar(DataPath, PrinterPath, "1", filepath1, txtBadgeID.Text, txtSOID.Text, template, WaferSize.ToString().Trim());
                        }
                    }
                    else
                    {
                        readTemplateSO3(DataPath, PrinterPath, "1", filepath1, txtBadgeID.Text, txtSOID.Text, template, WaferSize.ToString().Trim());
                    }
                    for (int i = 0; i < lstOCRID.Items.Count; i++)
                    {
                        //utility ut = new utility();
                        //string[] eqModel = ut.getAttrib("BIM-15B");
                        lstOCRIDError.Items.Add(lstOCRID.Items[i].ToString());
                    }
                    lstOCRID.Items.Clear();
                    lblScanQty.Text = "0";
                }
            }
            catch (Exception er)
            {
                lbErr.Text = er.Message.ToString();
                btnPrintAll.Visible = false;
            }
        }

        protected string[] getWaferInfo(string SOID)
        {
            string MyFab, Device, BimLine, Crystal;
            string[] WaferInfo = new string[1000];

            using (OracleConnection mesDbConn = new OracleConnection(ConfigurationManager.ConnectionStrings["MES"].ConnectionString))
            {
                mesDbConn.Open();
                using (OracleCommand cmd = mesDbConn.CreateCommand())
                {
                    cmd.CommandText = string.Format(@"select so.containername AS ShopOrder, w.containername AS MESWaferBatch, wso.qtyreserved QtyIn,pb.PRODUCTNAME AS Wafer12NC, p.description CrystalDesc,
                            o.mes_waferoriginname Fab,so.numberofwafer TOTALWAFER,RD.RESOURCENAME BIMLine, P.WAFERSIZE WAFERSIZE,
                            w.DIFFUSIONBATCHCODE AS DiffusionBatch,pf.productfamilyname Device
                            from container so, container w, mes_wafersusedbyso wso,product p, productbase pb, mes_waferorigin o,RESOURCEDEF RD, PRODUCTTYPE pt,PRODUCTFAMILY pf
                            where wso.mes_shopordercontainerid(+)= so.containerid
                            and wso.mes_wafercontainerid = w.containerid(+)
                            and w.productid=p.productid
                            and p.productbaseid=pb.productbaseid
                            and p.productid=pb.REVOFRCDID
                            and SO.PRODUCTFAMILYID = pf.PRODUCTFAMILYID
                            and o.mes_waferoriginid = p.mes_waferoriginid
                            and pt.PRODUCTTYPEID = p.PRODUCTTYPEID
                            and RD.RESOURCEID(+) = SO.MES_EQUIPMENTID
                            -- and so.containername = :pWOid
                            and so.containername = :pSOid
                            order by 1,2 ");

                    OracleParameter pWO = new OracleParameter(":pSOid", SOID) { OracleType = OracleType.Char };
                    cmd.Parameters.Add(pWO);
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        int i = 0;
                        while (reader.Read())
                        {
                            try
                            {
                                WaferInfo[i] = reader["Fab"].ToString();
                                WaferInfo[i + 1] = reader["Device"].ToString();
                                WaferInfo[i + 2] = reader["BIMLine"].ToString();
                                WaferInfo[i + 3] = reader["CRYSTALDESC"].ToString();
                                WaferInfo[i + 4] = reader["WAFERSIZE"].ToString();
                                i = i + 5;
                            }
                            catch
                            {

                            }

                        }
                    }
                }

                mesDbConn.Close();
            }
            return WaferInfo;
        }

        List<string> vMWO = null;
        List<string> vDevice = null;
        List<string> vLotID = null;
        List<string> vQTY = null;
        List<string> vCRYSTALDESC = null;
        List<string> vCONTAINERNAME = null;

        protected string[] getWaferInfoCamstar(string MWO)
        {
            string[] WaferInfo = new string[1000];

            SawBarcode.camstarProd.Ad_HocTxn mycamstar = new SawBarcode.camstarProd.Ad_HocTxn();

            var InfoByLotID = mycamstar.GetQueryResult("SBC-GetAllInfoByMWO_WaferPrint", "MWO", MWO);
            //20220902-JB-add variable for MWO to be display in GV1
            vMWO = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/MFGORDERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToList();
            var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToList();
            var vBIMLine = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/RESOURCENAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vWAFERSIZE = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERSIZE").OfType<XmlNode>().Select(n => n.InnerText).ToList();
            vCRYSTALDESC = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERDESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToList();
            var vWaferBatch = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERSCRIBENUMBER").OfType<XmlNode>().Select(n => n.InnerText).ToList();
            vLotID = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/MFGORDERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToList();
            vQTY = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/QTY").OfType<XmlNode>().Select(n => n.InnerText).ToList();
            var vTOTALWAFER = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/TOTALWAFER").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            vCONTAINERNAME = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToList();

            #region Commented Code by lotfi

            //MWO P130071528

            //var InfoByLotID = mycamstar.GetQueryResult("AW-GetCrystalDescByMWO", "MWO", MWO);
            //var vCRYSTALDESC = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            ////var vCRYSTALDESC0 = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            ////var vCRYSTALCONTAINERNAME = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            ////string y = null;
            ////var vCRYSTALDESC = y;

            ////var selected = from s in vCRYSTALCONTAINERNAME
            ////               select s;

            ////List<string> lCRYSTALCONTAINERNAME = selected.ToList();

            ////selected = from s in vCRYSTALDESC0
            ////               select s;

            ////List<string> lCRYSTALDESC0 = selected.ToList();


            //quick patch for camstar limitation due to non align pointers in common gateway webservices

            //List<string> lCRYSTALDESC0 = selected.ToList();  
            ////List<string> vCRYSTALDESC1 = selected.ToList();
            //List<string> lFab0 = selected.ToList();
            //List<string> vFab1 = selected.ToList();
            //List<string> lDevice0 = selected.ToList();
            //List<string> vDevice1 = selected.ToList();
            //List<string> lBIMLine0 = selected.ToList();
            //List<string> vBIMLine1 = selected.ToList();

            //   off this to support double marking 
            //    InfoByLotID = mycamstar.GetQueryResult("SBC-GetAllInfoByMWO", "MWO", MWO);


            //missing fab
            //////var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            ////selected = from s in vFab 
            ////           select s;
            ////List<string> lFab = selected.ToList();

            ////var vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            ////selected = from s in vDevice
            ////           select s;
            ////List<string> lDevice = selected.ToList();



            //////var vBIMLine = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/RESOURCENAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            ////selected = from s in vBIMLine
            ////           select s;
            ////List<string> lBIMLine = selected.ToList();



            ////var vWAFERSIZE = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/WAFERSIZE").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            ////selected = from s in vWAFERSIZE
            ////           select s;
            ////List<string> lWAFERSIZE = selected.ToList();


            //  InfoByLotID = mycamstar.GetQueryResult("SBC-GetAllInfoByMWO", "MWO", MWO);




            //  var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            // var vLotID = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //Use MWO (MFGORDERNAME) instead of LotID (CONTAINERNAME) due to request from saw team

            // var vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();



            ////selected = from s in vCONTAINERNAME
            ////           select s;

            ////List<string> lCONTAINERNAME = selected.ToList();

            ////int index1, index2;
            ////string swm, ocrid, ocrid0, Mycrystal;
            ////int ipointer = 0;
            ////int i = 0;
            ////foreach (string r in vCRYSTALCONTAINERNAME.Intersect(vCONTAINERNAME))
            ////{

            ////    try
            ////    {

            ////        {
            ////            int mytest = lCRYSTALCONTAINERNAME.IndexOf(r);
            ////            int mytest1 = lCONTAINERNAME.IndexOf(r);
            ////           // vCRYSTALDESC = lCRYSTALDESC0[mytest1].ToString();

            ////           // vFab1[i] = lFab0[mytest1].ToString();
            ////           // vDevice1[i] = lDevice0[mytest1].ToString();
            ////          //  vBIMLine1[i] = lBIMLine0[mytest1].ToString();
            ////            vCRYSTALDESC1[mytest1] = lCRYSTALDESC0[mytest].ToString();
            ////            i = i + 1;
            ////        }
            ////    }
            ////    catch
            ////    {  i = i + 1;}
            ////}

            #endregion Commented Code by lotfi

            int i = 0;
            for (int j = 0; i < vFab.Count(); i++)
            {
                WaferInfo[i] = vFab[j].ToString();
                WaferInfo[i + 1] = vDevice[j].ToString();
                WaferInfo[i + 2] = vBIMLine[j].ToString();
                WaferInfo[i + 3] = vCRYSTALDESC[j].ToString();

                double dWaferSize = Convert.ToInt32(vWAFERSIZE[j].ToString()) / 25.4;
                int iWaferSize;
                if (dWaferSize < 0)
                { 
                    iWaferSize = (int)(dWaferSize - 0.5); 
                }
                else
                { 
                    iWaferSize = (int)(dWaferSize + 0.5); 
                }
                WaferInfo[i + 4] = iWaferSize.ToString();

                j = j + 1;
                i = i + 5;
            }
            return WaferInfo;
        }

        public string getExtra(string crystal)
        {
            string Extra = "";
            using (SqlConnection repDbConn = new SqlConnection(ConfigurationManager.ConnectionStrings["RECIPE"].ConnectionString))
            {
                repDbConn.Open();

                using (SqlCommand cmd = repDbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM [WAFERRECIPE] TP WHERE WAFERNAME = '" + crystal + "' ;";
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Extra = reader["EXTRA"].ToString();
                        }
                    }
                }
                repDbConn.Close();
            }
            return Extra;
        }

        public List<string> getdmanCrystal()
        {
            //List<string> dmanCrystal = dmanCrystal();  //use this for dman
            List<string> dmanCrystal = new List<string>();
            var filePath1 = Server.MapPath("~/Config/dman_exclude.txt");
            //var filePath1 = Server.MapPath("~/Config/dman_excludegoo");
            const Int32 BufferSize = 1024;
            using (var fileStream = File.OpenRead(filePath1))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string line;
                while ((line = streamReader.ReadLine()) != null)
                {
                    try
                    {
                        dmanCrystal.Add(line);
                    }
                    catch
                    {

                    }
                }
                return dmanCrystal;
            }
        }

        public void readTemplateSO3(string DataPath, string PrinterPath, string txtQty, string filepath1, string txtBadgeID, string txtSOID, string template, string waferSize)
        {
            string[] separators1 = new[] { " " };
            string[] separators = new[] { "_" };
            string[] Records;
            string[] Records12NC;
            string bProduct;
            string swmfwm = "";
            string myBin = "";
            var filePath1 = "";
            string filePathTemplate = System.Web.HttpContext.Current.Server.MapPath("~/Config");
            PrinterPath = System.Web.HttpContext.Current.Server.MapPath("~/image/DOSPrinter.exe");
            //  var filePath2 = Server.MapPath("~/Config/");

            int LstOCRIDCounter = 0;

            DateTime now = DateTime.Now;
            string DDate = now.ToString("dd/MMM/yyyy");

            using (OracleConnection mesDbConn = new OracleConnection(ConfigurationManager.ConnectionStrings["MES"].ConnectionString))
            {

                mesDbConn.Open();
                using (OracleCommand cmd = mesDbConn.CreateCommand())
                {
                    cmd.CommandText = string.Format(@"select so.containername AS ShopOrder, 
                                             fpb.productname FGRequest12NC, 
                                             fp.DESCRIPTION Device,
                                             w.containername AS MESWaferBatch, 
                                             wso.qtyreserved QTYIN,
                                             pb.PRODUCTNAME AS Wafer12NC, 
                                             p.DESCRIPTION CrystalDesc,
                                             o.mes_waferoriginname Fab,
                                             so.numberofwafer TOTALWAFER,
                                             NVL (RD.RESOURCENAME,'-') BIMLine, 
                                             w.DIFFUSIONBATCHCODE AS DiffusionBatch,
                                             nvl((select distinct si.MES_SPECIALINSTRUCTIONNAME
                                             from vw_mes_containersi si
                                             where si.containername = so.containername
                                             and si.MES_SPECIALINSTRUCTIONNAME = 'Twin Die'),'') SI
                                             from container so, 
                                             container w, 
                                             mes_wafersusedbyso wso,
                                             product p, 
                                             productbase pb, 
                                             mes_waferorigin o,
                                             RESOURCEDEF RD, 
                                             PRODUCTTYPE pt,
                                             containermes_fginso cf, 
                                             mes_fginso mf, 
                                             product fp, 
                                             productbase fpb
                                             where wso.mes_shopordercontainerid(+)= so.containerid
                                             and wso.mes_wafercontainerid = w.containerid(+)
                                             and w.productid=p.productid
                                             and p.productbaseid=pb.productbaseid
                                             and p.productid=pb.REVOFRCDID
                                             and o.mes_waferoriginid = p.mes_waferoriginid
                                             and pt.PRODUCTTYPEID = p.PRODUCTTYPEID
                                             and RD.RESOURCEID(+) = SO.MES_EQUIPMENTID
                                             and cf.mes_fginsoid = mf.mes_fginsoid  -- new
                                             and fpb.productbaseid = fp.productbaseid
                                             and fp.productid = mf.MES_FINISHEDGOODID
                                             and cf.instanceid = so.containerid
                                             and so.containername = :pSOid");

                    OracleParameter pWO = new OracleParameter(":pSOid", txtSOID) { OracleType = OracleType.Char };
                    cmd.Parameters.Add(pWO);
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                string ws1 = reader["MESWaferBatch"].ToString();
                                //string[] ws = reader["MESWaferBatch"].ToString().Split('-');
                                //string slice = ws[2].ToString().Substring(1, 2);
                                //string batch = ws[1].ToString().Substring(0, 6);
                                string fab = reader["Fab"].ToString();
                                string SITwin = reader["SI"].ToString();
                                string bs = "";
                                string bsFWM = "";
                                bool FlagICN6 = false;
                                bool FlagException = false;
                                string cx = reader["CRYSTALDESC"].ToString();
                                string Extra = getExtra(cx);

                                if ((fab == "ICN6" && waferSize.ToString() == "6") || Extra == "NOTInSAP") 
                                { 
                                    FlagException = true;
                                }
                                if (FlagException == true)
                                {
                                    for (int i = 0; i < lstOCRID.Items.Count; i++)
                                    {

                                        string[] myWaferInfo;
                                        string myWaferBatch = "";
                                        string Slice = "";
                                        if (Extra == "NOTInSAP")
                                        {  //for non
                                            myWaferInfo = lstOCRID.Items[LstOCRIDCounter].ToString().Split('-');
                                            int SliceLen = myWaferInfo[0].ToString().Length;
                                            Slice = myWaferInfo[1].ToString();
                                        }
                                        if (fab == "ICN6" && waferSize.ToString() == "6")
                                        {
                                            myWaferInfo = lstOCRID.Items[LstOCRIDCounter].ToString().Split('-');
                                            myWaferBatch = "W-" + myWaferInfo[0].ToString().Substring(0, 6) + "-0" + myWaferInfo[1].ToString().Substring(0, 2);
                                            string[] WInfo = myWaferInfo[1].Split('.');
                                            try
                                            { Slice = WInfo[0].ToString(); }
                                            catch
                                            { 
                                            
                                            }
                                        }

                                        if (ws1 == myWaferBatch || ws1.Substring(ws1.ToString().Length - 2, 2) == Slice)
                                        {
                                            bsFWM = lstOCRID.Items[LstOCRIDCounter].ToString().Trim();
                                            lstOCRIDSuccess.Items.Add(lstOCRID.Items[LstOCRIDCounter].ToString());
                                            lstOCRID.Items.RemoveAt(LstOCRIDCounter);// check later Lotfi
                                            string xx = lstOCRID.Items.Count.ToString();
                                            FlagICN6 = true;
                                        }
                                    }
                                }
                                else
                                {
                                    bs = fabname(fab, ws1, waferSize);

                                    if (fab == "SILAN")
                                    {
                                        bsFWM = bs;
                                    }
                                    else
                                    {
                                        bsFWM = GenerateCheckChar(bs);
                                    }
                                }
                                //LstOCRIDCounter = LstOCRIDCounter + 1;

                                //bs = fabname(fab, ws1);
                                //bsFWM = GenerateCheckChar(bs);   
                                if ((FlagException == false) || (FlagException == true && FlagICN6 == true))
                                {
                                    if (fab.Trim() == "PHENITEC")
                                    {
                                        bsFWM = bsFWM.Substring(0, bsFWM.Length - 2);
                                    }
                                    string bs1 = bsFWM;  //lotfi

                                    //string cx = reader["CRYSTALDESC"].ToString();
                                    string myGroupFWM = "";
                                    //string Extra = getExtra(cx);
                                    if (Extra == "NOTInSAP" && txt2DScan.Text.Length < 1)
                                    {
                                    }
                                    if ((fab == "DHAM" || fab == "ICN6" || fab == "ICN8") && cx.Substring(0, 1).ToString() == "8" && Extra != "SWM")
                                    {
                                        myGroupFWM = "f";
                                    }
                                    //NOTInSAP use FWM
                                    if (Extra == "NOTInSAP")
                                    {
                                        myGroupFWM = "f";
                                    }
                                    //if (fab == "DHAM ) // requested by Aswafi 2017-06-20
                                    //{
                                    //    myGroupFWM = "f";
                                    //}

                                    string dev = reader["Device"].ToString();
                                    string crystalDmanExcludeValue = "False"; // not use
                                    swmfwm = createFWMSaw(cx, bs1, SITwin, txtSOID, cx, dev, myGroupFWM, fab, crystalDmanExcludeValue);

                                    string stringToCheck = swmfwm.ToString();
                                    string[] stringArray = { "]", "01", "S" };
                                    //string [] myArr = 

                                    SawPrefix = "";

                                    foreach (string x in stringArray)
                                    {
                                        if (stringToCheck.Contains(x))
                                        {
                                            SawPrefix = x.ToString();
                                        }
                                    }

                                    #region Commented code by lotfi
                                    //string wafercode = "0";
                                    //string statusFWM = "0";
                                    //if (swmfwm.Substring(0, 1).ToString() == "f") { statusFWM = "1"; }
                                    //if (myGroupFWM == "f") { statusFWM = "1"; }

                                    //MySaw.saw sawPrefix = new MySaw.saw();
                                    //if (cx.Contains("TA") == true || cx.Contains("TC")) { wafercode = "1"; }  //TATC

                                    ////initialize sawPrefix
                                    ////sawPrefix.Name = "";
                                    ////SawPrefix = "";
                                    ////--

                                    //if (wafercode == "1" & statusFWM == "1")
                                    //{
                                    //    sawPrefix.Name = "]";
                                    //    SawPrefix = "]";
                                    //}

                                    ////--
                                    //if (wafercode == "0" & statusFWM == "1")
                                    //{
                                    //    sawPrefix.Name = "";
                                    //    SawPrefix = "";
                                    //}

                                    //if (wafercode == "1" & statusFWM == "0")
                                    //{
                                    //    sawPrefix.Name = "0S";
                                    //    SawPrefix = "0S";
                                    //}

                                    //if (wafercode == "0" & statusFWM == "0")
                                    //{
                                    //    sawPrefix.Name = "S";
                                    //    SawPrefix = "S";
                                    //}
                                    #endregion Commented code by lotfi

                                    MyOCRID.Add(swmfwm);
                                    lSOID.Add(reader["ShopOrder"].ToString());
                                    lDDATE.Add(DDate);
                                    lDEVICE.Add(reader["Device"].ToString());
                                    string wb = reader["MESWaferBatch"].ToString();
                                    if (swmfwm.Substring(0, 1).ToString() == "S")
                                    {
                                        lOCRID.Add(SawPrefix + bs1);
                                        //  lOCRID.Add("#$" + SawPrefix + bs1);    //refer to badrul to remove manual entry, 5-mar-2018
                                        hOCRID.Add(SawPrefix + bs1);
                                    }
                                    else if (!swmfwm.ToUpper().Contains("NOSWM") && ((swmfwm.Substring(0, 1).ToString() == "f") || (myGroupFWM == "f")))
                                    {
                                        lOCRID.Add(bsFWM);
                                        hOCRID.Add(bsFWM);
                                    }
                                    else if (swmfwm == "TWIN")
                                    {
                                        lOCRID.Add(bs1 + "TWIN");  //lotfi, not sure yet
                                        hOCRID.Add(bs1 + "TWIN");
                                    }
                                    else
                                    {
                                        lOCRID.Add("NO MAP");
                                        hOCRID.Add("NO MAP" + "-" + wb);
                                    }

                                    if (fab == "DHAM" && swmfwm.Substring(0, 1).ToString() != SawPrefix)
                                    {
                                        myBin = "1-7";
                                    }
                                    else
                                    {
                                        myBin = "1";
                                    }
                                    //if (reader["BIMLine"].ToString() != "-")
                                    //{ lBIM.Add(reader["BIMLine"].ToString()); }
                                    //else
                                    //{ lBIM.Add(cbBimline.Text); }
                                    lBIM.Add(cbBimline.Text);
                                    Bin.Add(myBin);
                                    lQUANTITY.Add(reader["QtyIn"].ToString());
                                    lCRYSTAL.Add(reader["CRYSTALDESC"].ToString());
                                    crystal.Add(reader["CRYSTALDESC"].ToString());
                                    lBADGEID.Add(txtBadgeID.ToString());
                                    lTOTALWAFER.Add(reader["TOTALWAFER"].ToString());
                                    lDIETYPE.Add(SITwin);
                                    lWAFERBATCH.Add(reader["MESWaferBatch"].ToString());

                                    //lotfi init
                                    //sawPrefix.Name = "S";
                                    //SawPrefix = "S";
                                }
                            }
                            catch
                            {
                                return;
                            }
                        }
                    }
                }
                mesDbConn.Close();
            }
            try
            {
                int catcherror = Convert.ToInt16(lTOTALWAFER[0]);
            }
            catch
            {
                return;
            }

            filePath1 = filePathTemplate + "\\" + "saw.LBL";

            if (filePath1 == "")
            {
                return;
            }

            int j = 0;
            string[] LabelDataTotal = new string[1000000];
            int tTotal = 0;
            j = lOCRID.Count - 1;   //asap
            // j = Convert.ToInt16(lTOTALWAFER[0]) - 1; dont use this since there is a glitch in MES whre the qty not tally.
            if (lstOCRIDSuccess.Items.Count > 0)
            {
                j = lstOCRIDSuccess.Items.Count - 1;
            }

            //for (j = 0; j < Convert.ToInt16(lTOTALWAFER[0]); j++)
            for (int rx = j; j >= 0; j--)
            {
                const Int32 BufferSize = 1024;
                string[] LabelData = new string[1000];
                int i = 0;

                bool chk = true;

                using (var fileStream = File.OpenRead(filePath1))

                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;
                    // int j = Convert.ToInt16(lTOTALWAFER[1].ToString());
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        try
                        {
                            LabelData[i] = line;
                            if (line.Contains("#10#") == true)
                            {
                                string sData = LabelData[i].Replace("#10#", "");
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#SOID#") == true)
                            {
                                string sData = LabelData[i].Replace("#SOID#", lSOID[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#OCRID#") == true && line.Contains("^B") == true)
                            {
                                string nmap0 = "";
                                if (lOCRID[j].Contains("NO MAP") == true)
                                {
                                    string[] nmap = lOCRID[j].Split('-');
                                    nmap0 = nmap[0].ToString();
                                }
                                else
                                {
                                    nmap0 = lOCRID[j];
                                }

                                string sData = LabelData[i].Replace("#OCRID#", nmap0);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#hOCRID#") == true && line.Contains("CI") == true)
                            {
                                string nmap0 = "";
                                if (hOCRID[j].Contains("NO MAP") == true)
                                {
                                    string[] separator = new[] { "-W-" };
                                    string[] nmap = hOCRID[j].ToString().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                    nmap0 = nmap[0].ToString() + "-" + nmap[1].ToString();
                                }
                                else
                                {
                                    nmap0 = hOCRID[j];
                                }

                                string sData = LabelData[i].Replace("#hOCRID#", nmap0);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#DDATE#") == true)
                            {
                                string sData = LabelData[i].Replace("#DDATE#", lDDATE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#CRYSTAL#") == true)
                            {
                                string sData = LabelData[i].Replace("#CRYSTAL#", lCRYSTAL[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BIM#") == true)
                            {
                                string sData = LabelData[i].Replace("#BIM#", lBIM[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#QUANTITY#") == true)
                            {
                                string sData = LabelData[i].Replace("#QUANTITY#", lQUANTITY[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#PRODUCT#") == true)
                            {
                                string sData = LabelData[i].Replace("#PRODUCT#", lDEVICE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BADGEID#") == true)
                            {
                                string sData = LabelData[i].Replace("#BADGEID#", txtBadgeID);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#TOTALWAFER#") == true)
                            {
                                string sData = LabelData[i].Replace("#TOTALWAFER#", lTOTALWAFER[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#DIETYPE#") == true)
                            {
                                string sData = LabelData[i].Replace("#DIETYPE#", lDIETYPE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BOX#") == true)
                            {
                                string sData = LabelData[i].Replace("#BOX#", "");
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BIN#") == true)
                            {
                                string sData = LabelData[i].Replace("#BIN#", Bin[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#PACKAGE#") == true)
                            {
                                string sData = LabelData[i].Replace("#PACKAGE#", "");
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }

                            i = i + 1;
                            tTotal = tTotal + 1;
                            if (chk == false)
                            {

                            }
                        }
                        catch
                        {

                        }
                    }
                    if (chk == true)
                    {
                        writeSettingSO3(DataPath, PrinterPath, txtQty, "aa", "bb", LabelData, lSOID[j], template.ToUpper(), lOCRID[j], lBIM[j], lQUANTITY[j], lCRYSTAL[j], crystal[j], lBADGEID[j], lTOTALWAFER[j], lDIETYPE[j], lWAFERBATCH[j], MyOCRID[j], hOCRID[j]);
                    }
                }
                tTotal = tTotal - 1;
            }
            writeSettingTotal(DataPath, PrinterPath, txtQty, "aa", "bb", LabelDataTotal, lSOID[0], template.ToUpper(), lTOTALWAFER[0]);
        }

        public void readTemplateSO3Camstar(string DataPath, string PrinterPath, string txtQty, string filepath1, string txtBadgeID, string txtSOID, string template, string waferSize)
        {
            string crystalDmanExcludeValue = "False";
            string[] separators1 = new[] { " " };
            string[] separators = new[] { "_" };
            string[] Records;
            string[] Records12NC;
            string bProduct;
            string swmfwm = "";
            string myBin = "";
            var filePath1 = "";
            string package = "";
            string filePathTemplate = System.Web.HttpContext.Current.Server.MapPath("~/Config");
            PrinterPath = System.Web.HttpContext.Current.Server.MapPath("~/image/DOSPrinter.exe");
            //  var filePath2 = Server.MapPath("~/Config/");

            int LstOCRIDCounter = 0;

            DateTime now = DateTime.Now;
            string DDate = now.ToString("dd/MMM/yyyy");

            string MWO = txtSOID;

            SawBarcode.camstarProd.Ad_HocTxn mycamstar = new SawBarcode.camstarProd.Ad_HocTxn();

            List<string> cname = new List<string>();
            List<string> crystalDmanExclude = new List<string>();

            var InfoByLotID = mycamstar.GetQueryResult("SBC-GetAllInfoByMWO_WaferPrint", "MWO", MWO);

            //20220902-JB-add variable for MWO to be display in GV1
            var vMWO = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/MFGORDERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vBIMLine = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/RESOURCENAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vCRYSTALDESC = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERDESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vWAFERSIZE = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERSIZE").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vWaferBatch = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERSCRIBEMUNBER").OfType<XmlNode>().Select(n => n.InnerText).ToArray(); // error no data
            var vLotID = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/MFGORDERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vQTY = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/QTY").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vTOTALWAFER = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/TOTALWAFER").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vCONTAINERNAME = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            InfoByLotID = mycamstar.GetQueryResult("AW-GetAllDataByMO", "MWO", MWO);
            var vpackage = InfoByLotID.SelectNodes("//AW-GetAllDataByMO/NXPSUBPACKAGEGROUP").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            package = vpackage[0].ToString();

            #region commented by lotfi uncommented on 28-09-2020 by sri
            //MWO P130071528



            //InfoByLotID = mycamstar.GetQueryResult("AW-GetCrystalDescByMWO", "MWO", MWO);
            //var vCRYSTALDESC = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //// var vCRYSTALDESC0 = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //var vCRYSTALCONTAINERNAME = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            //string y = null;
            //var vCRYSTALDESC = y;

            //var selected = from s in vCRYSTALCONTAINERNAME
            //               select s;

            //List<string> lCRYSTALCONTAINERNAME = selected.ToList();

            //selected = from s in vCRYSTALDESC0
            //           select s;

            //List<string> lCRYSTALDESC0 = selected.ToList();


            //quick patch for camstar limitation due to non align pointers in common gateway webservices

            //List<string> lCRYSTALDESC0 = selected.ToList();  
            // List<string> vCRYSTALDESC1 = selected.ToList();
            //List<string> lFab0 = selected.ToList();
            //List<string> vFab1 = selected.ToList();
            //List<string> lDevice0 = selected.ToList();
            //List<string> vDevice1 = selected.ToList();
            //List<string> lBIMLine0 = selected.ToList();
            //List<string> vBIMLine1 = selected.ToList();


            //missing fab
            //var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //selected = from s in vFab
            //           select s;
            //List<string> lFab = selected.ToList();

            //var vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //selected = from s in vDevice
            //           select s;
            //List<string> lDevice = selected.ToList();



            //var vBIMLine = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/BIMLINE").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //selected = from s in vBIMLine
            //           select s;
            //List<string> lBIMLine = selected.ToList();



            //var vWAFERSIZE = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/WAFERSIZE").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //selected = from s in vWAFERSIZE
            //           select s;
            //List<string> lWAFERSIZE = selected.ToList();

            //   off this to support double marking 
            //    InfoByLotID = mycamstar.GetQueryResult("SBC-GetAllInfoByMWO", "MWO", MWO);
            
            // var vWaferBatch = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray(); // change back this the correct one
            // var vLotID = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //Use MWO (MFGORDERNAME) instead of LotID (CONTAINERNAME) due to request from saw team

            //    string DHAMQty = getWafermapQty(Waferid);

            #endregion commented by lotfi

            DataTable dt = new DataTable();
            int u = 0;
            dt.Columns.Add("Id", typeof(string));
            dt.Columns.Add("MWO", typeof(string));
            dt.Columns.Add("Fab", typeof(string));
            dt.Columns.Add("WaferBatch", typeof(string));
            dt.Columns.Add("WaferDescription", typeof(string));
            dt.Columns.Add("Quantity", typeof(string));
            dt.Columns.Add("Quarter", typeof(string));
            dt.Columns.Add("Half", typeof(string));
            dt.Columns.Add("Print", typeof(string));

            //btnClear.Visible = Visible;
            btnPrintAll.Visible = Visible;

            foreach (string data in vCONTAINERNAME)
            {
                double dWaferSize = Convert.ToInt32(vWAFERSIZE[u].ToString()) / 25.4;
                int iWaferSize;
                if (dWaferSize < 0)
                {
                    iWaferSize = (int)(dWaferSize - 0.5);
                }
                else
                {
                    iWaferSize = (int)(dWaferSize + 0.5);
                }

                dt.Rows.Add(u + 1, vMWO[u].ToString(), vFab[u].ToString(), vCONTAINERNAME[u].ToString(), vCRYSTALDESC[u].ToString(), vQTY[u], "Q__" + vCONTAINERNAME[u].ToString(), "H__" + vCONTAINERNAME[u].ToString(), "Print__" + vCONTAINERNAME[u].ToString());
                u++;
            }
            Gv1.DataSource = dt;

            #region Commented by lotfi
            //////selected = from s in vCONTAINERNAME
            //////           select s;

            //////List<string> lCONTAINERNAME = selected.ToList();

            //////int index1, index2;
            //////string swm, ocrid, ocrid0, Mycrystal;
            //////int ipointer = 0;

            //////foreach (string r in vCRYSTALCONTAINERNAME.Intersect(vCONTAINERNAME))
            //////{

            //////    try
            //////    {

            //////        {
            //////            int mytest = lCRYSTALCONTAINERNAME.IndexOf(r);
            //////            int mytest1 = lCONTAINERNAME.IndexOf(r);
            //////            vCRYSTALDESC = lCRYSTALDESC0[mytest1].ToString();

            //////            // vFab1[i] = lFab0[mytest1].ToString();
            //////            // vDevice1[i] = lDevice0[mytest1].ToString();
            //////            //  vBIMLine1[i] = lBIMLine0[mytest1].ToString();
            //////            vCRYSTALDESC1[mytest1] = lCRYSTALDESC0[mytest].ToString();

            //////        }
            //////    }
            //////    catch
            //////    { }
            //////}

            #endregion Commented by lotfi

            InfoByLotID = mycamstar.GetQueryResult("AW-GetAllInfoSI", "MWO", MWO);
            var vSI = InfoByLotID.SelectNodes("//AW-GetAllInfoSI/NXPSPECIALINSTRUCTIONNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            string SIQuarter = "2  HALF SAW  TWIN  DIE";
            string SIQuarter1 = "2  HALF SAW SINGLE DIE";
            string SIQuarter2 = "2  QUARTER SAW  TWIN  DIE";
            string SIQuarter3 = "2  QUARTER SAW SINGLE DIE";
            string SIQuarter4 = "8" + "\"" + " wafer. Do Quartering";
            string SIQuarter5 = "Wafer -  All cut to Quarter";
            string SIQuarter6 = "Wafer Cut to 1/2";

            int Qtr = 0;
            for (int k = 0; k < vSI.Count(); k++)
            {
                string mySITemp = vSI[k].ToString().ToUpper();
                if (mySITemp == SIQuarter) 
                { 
                    Qtr = (Qtr - 2 + 4); lCRYSTALSAW[k] = "HALF";
                }
                if (mySITemp == SIQuarter1) 
                { 
                    Qtr = (Qtr - 2 + 4); lCRYSTALSAW[k] = "HALF"; 
                }
                if (mySITemp == SIQuarter2) 
                { 
                    Qtr = (Qtr - 2 + 8); lCRYSTALSAW[k] = "QUARTER"; 
                }
                if (mySITemp == SIQuarter3) 
                {
                    Qtr = (Qtr - 2 + 8); lCRYSTALSAW[k] = "QUARTER";
                }
                if (mySITemp == SIQuarter4) 
                { 
                    Qtr = (vCRYSTALDESC.Count() * 4); break;
                }
                if (mySITemp == SIQuarter5) 
                { 
                    Qtr = (vCRYSTALDESC.Count() * 4); break;
                }
                if (mySITemp == SIQuarter6) 
                {
                    Qtr = (vCRYSTALDESC.Count() * 2); break;
                }
            }

            InfoByLotID = mycamstar.GetQueryResult("AW-GetAllInfoTwinDieSI", "MWO", MWO);
            var vSITwin = InfoByLotID.SelectNodes("//AW-GetAllInfoTwinDieSI/TWINSI").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            // to check this - lotfi

            for (int k = 0; k < vCRYSTALDESC.Count() + Qtr; k++)
            {
                string ws1 = vWaferBatch[k].ToString();
                string fab = vFab[k].ToString();

                //AW-GetAllInfoTwinDieSI
                string SITwin = "";
                try
                { 
                    SITwin = vSITwin[k].ToString();
                }
                catch
                { 
                
                }

                string bs = "";
                string bsFWM = "";
                bool FlagICN6 = false;
                bool FlagException = false;
                string cx = vCRYSTALDESC[k].ToString();
                string Extra = getExtra(cx);

                if ((fab == "ICN6" && waferSize.ToString() == "6") || Extra == "NOTInSAP") { FlagException = true; }
                if (FlagException == true)
                {
                    for (int i = 0; i < lstOCRID.Items.Count; i++)
                    {

                        string[] myWaferInfo;
                        string myWaferBatch = "";
                        string Slice = "";
                        if (Extra == "NOTInSAP")
                        {  //for non
                            if (lstOCRID.Items[LstOCRIDCounter].ToString().Contains(" "))
                            {
                                myWaferInfo = lstOCRID.Items[LstOCRIDCounter].ToString().Split(' ');
                                if (myWaferInfo.Length == 5)
                                {
                                    int SliceLen = myWaferInfo[1].ToString().Length;
                                    Slice = myWaferInfo[2].ToString();
                                }
                                else if (myWaferInfo.Length == 3)
                                {
                                    int SliceLen = myWaferInfo[0].ToString().Length;
                                    Slice = myWaferInfo[1].ToString();
                                }
                                else
                                {
                                    int SliceLen = 0;
                                    Slice = "";
                                }
                            }
                            else
                            {
                                myWaferInfo = lstOCRID.Items[LstOCRIDCounter].ToString().Split('-');
                                int SliceLen = myWaferInfo[0].ToString().Length;
                                Slice = myWaferInfo[1].ToString();
                            }
                            //not useful for NotInSAP case.
                            //  myWaferBatch = "W-" + myWaferInfo[0].ToString().Substring(0, 5) + "0-0" + Slice;                                          
                        }
                        if (fab == "ICN6" && waferSize.ToString() == "6")
                        {
                            myWaferInfo = lstOCRID.Items[LstOCRIDCounter].ToString().Split('-');
                            myWaferBatch = "W-" + myWaferInfo[0].ToString().Substring(0, 6) + "-0" + myWaferInfo[1].ToString().Substring(0, 2);
                            string[] WInfo = myWaferInfo[1].Split('.');
                            try
                            { 
                                Slice = WInfo[0].ToString(); 
                            }
                            catch
                            {
                            
                            }
                        }
                        if (ws1 == myWaferBatch || ws1.Substring(ws1.ToString().Length - 2, 2) == Slice)
                        {
                            bsFWM = lstOCRID.Items[LstOCRIDCounter].ToString().Trim();
                            lstOCRIDSuccess.Items.Add(lstOCRID.Items[LstOCRIDCounter].ToString());
                            lstOCRID.Items.RemoveAt(LstOCRIDCounter);// check later Lotfi
                            string xx = lstOCRID.Items.Count.ToString();
                            FlagICN6 = true;
                        }
                    }
                }
                else
                {
                    bs = fabname(fab, ws1, waferSize);
                    if (fab == "DMAN")
                    {
                        bsFWM = GenerateCheckChar(bs);
                        List<string> dmanXtal = getdmanCrystal();  //use this for dman
                        if (dmanXtal.Contains(cx) == true)
                        {
                            crystalDmanExclude.Add("True");
                        }
                        else
                        { 
                            crystalDmanExclude.Add("False");
                        }
                    }
                    else if (fab == "SILAN")
                    {
                        bsFWM = bs;
                    }
                    else
                    { 
                        bsFWM = GenerateCheckChar(bs); 
                    }
                }
                if ((FlagException == false) || (FlagException == true && FlagICN6 == true))
                {
                    if (fab.Trim() == "PHENITEC")
                    {
                        bsFWM = bsFWM.Substring(0, bsFWM.Length - 2);
                    }
                    string bs1 = bsFWM;  //lotfi
                    string myGroupFWM = "";
                    if (Extra == "NOTInSAP" && txt2DScan.Text.Length < 1)
                    {
                        //    Response.Write("<script>alert('OCRID inconsistent in SAP detected, Must Scan BarCode');</script>");
                        //    return;
                    }
                    if ((fab == "DHAM" || fab == "ICN6" || fab == "ICN8") && cx.Substring(0, 1).ToString() == "8" && Extra != "SWM")
                    {
                        myGroupFWM = "f";
                    }
                    //NOTInSAP use FWM
                    if (Extra == "NOTInSAP")
                    {
                        myGroupFWM = "f";
                    }
                    string dev = vDevice[k].ToString();
                    try
                    {
                        crystalDmanExcludeValue = crystalDmanExclude[k];
                    }
                    catch
                    { 
                    
                    }

                    swmfwm = createFWMSaw(cx, bs1, SITwin, txtSOID, cx, dev, myGroupFWM, fab, crystalDmanExcludeValue);

                    string stringToCheck = swmfwm.ToString();
                    string[] stringArray = { "]", "01", "S" };

                    SawPrefix = "";

                    foreach (string x in stringArray)
                    {
                        if (stringToCheck.Contains(x))
                        {
                            SawPrefix = x.ToString();
                        }
                    }
                    lPACKAGE.Add(package);
                    MyOCRID.Add(swmfwm);
                    lSOID.Add(vLotID[k].ToString());
                    lDDATE.Add(DDate);
                    lDEVICE.Add(vDevice[k].ToString());
                    string wb = vWaferBatch[k].ToString();
                    if (swmfwm.Substring(0, 1).ToString() == "S")
                    {
                        lOCRID.Add(SawPrefix + bs1);
                        //  lOCRID.Add("#$" + SawPrefix + bs1);    //refer to badrul to remove manual entry, 5-mar-2018
                        hOCRID.Add(SawPrefix + bs1);
                    }
                    else if (!swmfwm.ToUpper().Contains("noSWM") && ((swmfwm.Substring(0, 1).ToString() == "f") || (myGroupFWM == "f")))
                    {
                        lOCRID.Add(bsFWM);
                        hOCRID.Add(bsFWM);
                    }
                    else if (swmfwm == "TWIN")
                    {
                        lOCRID.Add(bs1 + "TWIN");  //lotfi, not sure yet
                        hOCRID.Add(bs1 + "TWIN");
                    }
                    else
                    {
                        lOCRID.Add("NO MAP");
                        hOCRID.Add("NO MAP" + "-" + wb);
                    }

                    if (fab == "DHAM" && swmfwm.Substring(0, 1).ToString() != SawPrefix) 
                    { 
                        myBin = "1-7"; 
                    }
                    else
                    { 
                        myBin = "1"; 
                    }
                    lBIM.Add(cbBimline.Text);
                    Bin.Add(myBin);
                    lQUANTITY.Add(vQTY[k].ToString());

                    //utility ut = new utility();
                    //string[] eqModel = ut.getAttrib("BIM-15B");
                    lCRYSTAL.Add(vCRYSTALDESC[k].ToString());
                    crystal.Add(vCRYSTALDESC[k].ToString());
                    lBADGEID.Add(txtBadgeID.ToString());
                    lTOTALWAFER.Add(vTOTALWAFER[k].ToString());
                    lDIETYPE.Add(SITwin);
                    lWAFERBATCH.Add(vWaferBatch[k].ToString());
                }
            }
            try
            { 
                int catcherror = Convert.ToInt16(lTOTALWAFER[0]); 
            }
            catch
            { 
                return;
            }

            filePath1 = filePathTemplate + "\\" + "saw.LBL";

            if (filePath1 == "") 
            { 
                return; 
            }

            int j = 0;
            string[] LabelDataTotal = new string[1000000];
            int tTotal = 0;
            j = lOCRID.Count - 1;   //asap
            // j = Convert.ToInt16(lTOTALWAFER[0]) - 1; dont use this since there is a glitch in MES whre the qty not tally.
            if (lstOCRIDSuccess.Items.Count > 0)
            { 
                j = lstOCRIDSuccess.Items.Count - 1;
            }
            int ilblScanQty = 0;
            int iTotalWafer = 0;
            try
            {
                ilblScanQty = Convert.ToInt32(lblScanQty.Text);
            }
            catch
            { 
            
            }
            if (ilblScanQty > 0)
            {
                // iTotalWafer = vCRYSTALDESC.Count();
                iTotalWafer = ilblScanQty;
            }
            else
            {
                // iTotalWafer = Convert.ToInt32(lTOTALWAFER[0]);  //do not use, it does not support wafer scrap too early before print
                iTotalWafer = vCRYSTALDESC.Count();
            }

            // for (int rx = j; j >= 0; j--)
            for (j = 0; j < iTotalWafer; j++)
            //  for (j = 0; j < vCRYSTALDESC.Count(); j++)   // do not use, it does not support single wafer load even total > 1, for DHAM
            {
                const Int32 BufferSize = 1024;
                string[] LabelData = new string[1000];
                int i = 0;

                bool chk = true;

                using (var fileStream = File.OpenRead(filePath1))

                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;
                    // int j = Convert.ToInt16(lTOTALWAFER[1].ToString());
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        try
                        {
                            LabelData[i] = line;
                            if (line.Contains("#10#") == true)
                            {
                                string sData = LabelData[i].Replace("#10#", "");
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#SOID#") == true)
                            {
                                string sData = LabelData[i].Replace("#SOID#", lSOID[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#OCRID#") == true && line.Contains("^B") == true)
                            {
                                string nmap0 = "";
                                if (lOCRID[j].Contains("NO MAP") == true)
                                {
                                    string[] nmap = lOCRID[j].Split('-');
                                    nmap0 = nmap[0].ToString();
                                }
                                else
                                {
                                    nmap0 = lOCRID[j];
                                }
                                string sData = LabelData[i].Replace("#OCRID#", nmap0);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#hOCRID#") == true && line.Contains("CI") == true)
                            {
                                string nmap0 = "";
                                if (hOCRID[j].Contains("NO MAP") == true)
                                {
                                    string[] separator = new[] { "-W-" };
                                    string[] nmap = hOCRID[j].ToString().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                    nmap0 = nmap[0].ToString() + "-" + nmap[1].ToString();
                                }
                                else
                                {
                                    nmap0 = hOCRID[j];
                                }
                                string sData = LabelData[i].Replace("#hOCRID#", nmap0);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#DDATE#") == true)
                            {
                                string sData = LabelData[i].Replace("#DDATE#", lDDATE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#CRYSTAL#") == true)
                            {
                                string sData = LabelData[i].Replace("#CRYSTAL#", lCRYSTAL[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            #region unused method
                            // if (line.Contains("#CRYSTAL#") == true && line.Contains("^B") == true) //20250312-JB-add rule TATC
                            //{
                            //    string sData = LabelData[i].Replace("#CRYSTAL#", lCRYSTAL[j]);
                            //    LabelData[i] = sData;
                            //    LabelDataTotal[tTotal] = sData;
                            //}
                            //if (line.Contains("#hCRYSTAL#") == true && line.Contains("CI") == true) //20250312-JB-add code for barcode TATC
                            //{
                            //    string sData = LabelData[i].Replace("#hCRYSTAL#", lCRYSTAL[j]);
                            //    LabelData[i] = sData;
                            //    LabelDataTotal[tTotal] = sData;
                            //}
                            #endregion

                            if (line.Contains("#BIM#") == true)
                            {
                                string sData = LabelData[i].Replace("#BIM#", lBIM[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#QUANTITY#") == true)
                            {
                                string sData = LabelData[i].Replace("#QUANTITY#", lQUANTITY[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#PRODUCT#") == true)
                            {
                                string sData = LabelData[i].Replace("#PRODUCT#", lDEVICE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BADGEID#") == true)
                            {
                                string sData = LabelData[i].Replace("#BADGEID#", txtBadgeID);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#TOTALWAFER#") == true)
                            {
                                string sData = LabelData[i].Replace("#TOTALWAFER#", lTOTALWAFER[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#DIETYPE#") == true)
                            {
                                string sData = LabelData[i].Replace("#DIETYPE#", lDIETYPE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BOX#") == true)
                            {
                                string sData = LabelData[i].Replace("#BOX#", "");
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BIN#") == true)
                            {
                                string sData = LabelData[i].Replace("#BIN#", Bin[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#PACKAGE#") == true)
                            {
                                string sData = LabelData[i].Replace("#PACKAGE#", lPACKAGE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }

                            i = i + 1;
                            tTotal = tTotal + 1;
                            if (chk == false)
                            {

                            }
                        }
                        catch
                        {

                        }
                    }
                    if (chk == true)
                    {
                        writeSettingSO3(DataPath, PrinterPath, txtQty, "aa", "bb", LabelData, lSOID[j], template.ToUpper(), lOCRID[j], lBIM[j], lQUANTITY[j], lCRYSTAL[j], crystal[j], lBADGEID[j], lTOTALWAFER[j], lDIETYPE[j], lWAFERBATCH[j], MyOCRID[j], hOCRID[j]);
                    }
                }

                tTotal = tTotal - 1;
            } // loop for labelling
            writeSettingTotal(DataPath, PrinterPath, txtQty, "aa", "bb", LabelDataTotal, lSOID[0], template.ToUpper(), lTOTALWAFER[0]);


        }

        public void readTemplateSO3CamstarDHAM(string DataPath, string PrinterPath, string txtQty, string filepath1, string txtBadgeID, string txtSOID, string template, string waferSize)
        {
            string[] separators1 = new[] { " " };
            string[] separators = new[] { "_" };
            string[] Records;
            string[] Records12NC;
            string bProduct;
            string swmfwm = "";
            string myBin = "";
            var filePath1 = "";
            string package = "";
            string filePathTemplate = System.Web.HttpContext.Current.Server.MapPath("~/Config");
            PrinterPath = System.Web.HttpContext.Current.Server.MapPath("~/image/DOSPrinter.exe");
            //  var filePath2 = Server.MapPath("~/Config/");

            int LstOCRIDCounter = 0;

            DateTime now = DateTime.Now;
            string DDate = now.ToString("dd/MMM/yyyy");

            string MWO = txtSOID;

            SawBarcode.camstarProd.Ad_HocTxn mycamstar = new SawBarcode.camstarProd.Ad_HocTxn();

            List<string> cname = new List<string>();

            var InfoByLotID = mycamstar.GetQueryResult("SBC-GetAllInfoByMWO_WaferPrint", "MWO", MWO);

            //20220902-JB-add variable for MWO to be display in GV1
            var vMWO = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/MFGORDERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vBIMLine = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/RESOURCENAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vCRYSTALDESC = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERDESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vWAFERSIZE = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERSIZE").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vWaferBatch = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERSCRIBEMUNBER").OfType<XmlNode>().Select(n => n.InnerText).ToArray(); // error no data
            var vLotID = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/MFGORDERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vQTY = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/QTY").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            var vDHAMOCRID = new[] 
            {
                lstOCRID.Items 
            };
            #region Commented by lotfi uncommented on 28-09-2020 by sri
            var InfoByPkg = mycamstar.GetQueryResult("AW-GetAllDataByMO", "MWO", MWO);
            var vpackage = InfoByPkg.SelectNodes("//AW-GetAllDataByMO/NXPSUBPACKAGEGROUP").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            package = vpackage[0].ToString();
            #region Unused Methods
            //MWO P130071528
            //InfoByLotID = mycamstar.GetQueryResult("AW-GetCrystalDescByMWO", "MWO", MWO);
            //var vCRYSTALDESC = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //// var vCRYSTALDESC0 = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //var vCRYSTALCONTAINERNAME = InfoByLotID.SelectNodes("//AW-GetCrystalDescByMWO/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            //string y = null;
            //var vCRYSTALDESC = y;

            //var selected = from s in vCRYSTALCONTAINERNAME
            //               select s;

            //List<string> lCRYSTALCONTAINERNAME = selected.ToList();

            //selected = from s in vCRYSTALDESC0
            //           select s;

            //List<string> lCRYSTALDESC0 = selected.ToList();


            //quick patch for camstar limitation due to non align pointers in common gateway webservices

            //List<string> lCRYSTALDESC0 = selected.ToList();  
            // List<string> vCRYSTALDESC1 = selected.ToList();
            //List<string> lFab0 = selected.ToList();
            //List<string> vFab1 = selected.ToList();
            //List<string> lDevice0 = selected.ToList();
            //List<string> vDevice1 = selected.ToList();
            //List<string> lBIMLine0 = selected.ToList();
            //List<string> vBIMLine1 = selected.ToList();


            //missing fab
            //var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //selected = from s in vFab
            //           select s;
            //List<string> lFab = selected.ToList();

            //var vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //selected = from s in vDevice
            //           select s;
            //List<string> lDevice = selected.ToList();



            //var vBIMLine = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/BIMLINE").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //selected = from s in vBIMLine
            //           select s;
            //List<string> lBIMLine = selected.ToList();



            //var vWAFERSIZE = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/WAFERSIZE").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //selected = from s in vWAFERSIZE
            //           select s;
            //List<string> lWAFERSIZE = selected.ToList();

            //   off this to support double marking 
            //    InfoByLotID = mycamstar.GetQueryResult("SBC-GetAllInfoByMWO", "MWO", MWO);

            // var vWaferBatch = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray(); // change back this the correct one
            // var vLotID = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //Use MWO (MFGORDERNAME) instead of LotID (CONTAINERNAME) due to request from saw team
            #endregion Unused Methods

            #endregion Commented by lotfi
            //init array length
            string[] arr = new string[vDHAMOCRID[0].Count];
            string[] arrQty = new string[vDHAMOCRID[0].Count];
            string[] arrWaferBatch = new string[vDHAMOCRID[0].Count];
            for (int i = 0; i < vDHAMOCRID[0].Count; i++)
            {
                arr[i] = lstOCRID.Items[i].ToString();
                arrQty[i] = getWafermapQty(arr[i].ToString());
                arrWaferBatch[i] = "DHAM";
            }
            if (vFab[0] == "DHAM")
            {
                vQTY = arrQty;
                vWaferBatch = arrWaferBatch;
            }
            var vTOTALWAFER = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/TOTALWAFER").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vCONTAINERNAME = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            DataTable dt = new DataTable();
            int u = 0;
            dt.Columns.Add("Id", typeof(string));
            dt.Columns.Add("MWO", typeof(string));
            dt.Columns.Add("Fab", typeof(string));
            dt.Columns.Add("WaferBatch", typeof(string));
            dt.Columns.Add("WaferDescription", typeof(string));
            dt.Columns.Add("Quantity", typeof(string));
            dt.Columns.Add("Quarter", typeof(string));
            dt.Columns.Add("Half", typeof(string));
            dt.Columns.Add("Print", typeof(string));

            //btnClear.Visible = Visible;
            //btnPrintAll.Visible = Visible;

            foreach (string data in arrQty)
            //foreach (string data in vCONTAINERNAME)
            {
                double dWaferSize = Convert.ToInt32(vWAFERSIZE[u].ToString()) / 25.4;
                int iWaferSize;
                if (dWaferSize < 0)
                {
                    iWaferSize = (int)(dWaferSize - 0.5);
                }
                else
                {
                    iWaferSize = (int)(dWaferSize + 0.5);
                }
                dt.Rows.Add(u + 1, vMWO[u].ToString(), vFab[u].ToString(), vCONTAINERNAME[u].ToString(), vCRYSTALDESC[u].ToString(), vQTY[u], "Q__" + vCONTAINERNAME[u].ToString(), "H__" + vCONTAINERNAME[u].ToString(), "Print__" + vCONTAINERNAME[u].ToString());
                u++;
            }
            Gv1.DataSource = dt;

            #region Commented by lotfi
            //////selected = from s in vCONTAINERNAME
            //////           select s;

            //////List<string> lCONTAINERNAME = selected.ToList();

            //////int index1, index2;
            //////string swm, ocrid, ocrid0, Mycrystal;
            //////int ipointer = 0;

            //////foreach (string r in vCRYSTALCONTAINERNAME.Intersect(vCONTAINERNAME))
            //////{

            //////    try
            //////    {

            //////        {
            //////            int mytest = lCRYSTALCONTAINERNAME.IndexOf(r);
            //////            int mytest1 = lCONTAINERNAME.IndexOf(r);
            //////            vCRYSTALDESC = lCRYSTALDESC0[mytest1].ToString();

            //////            // vFab1[i] = lFab0[mytest1].ToString();lblScanQty
            //////            // vDevice1[i] = lDevice0[mytest1].ToString();
            //////            //  vBIMLine1[i] = lBIMLine0[mytest1].ToString();
            //////            vCRYSTALDESC1[mytest1] = lCRYSTALDESC0[mytest].ToString();

            //////        }
            //////    }
            //////    catch
            //////    { }
            //////}
            #endregion Commented by lotfi

            InfoByLotID = mycamstar.GetQueryResult("AW-GetAllInfoTwinDieSI", "MWO", MWO);
            var vSITwin = InfoByLotID.SelectNodes("//AW-GetAllInfoTwinDieSI/TWINSI").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            // to check this - lotfi

            for (int k = 0; k < Convert.ToInt32(lblScanQty.Text); k++)
            {
                string ws1 = vWaferBatch[k].ToString();
                string fab = vFab[k].ToString();

                //AW-GetAllInfoTwinDieSI
                string SITwin = "";
                try
                {
                    SITwin = vSITwin[k].ToString();
                }
                catch
                {
                }
                string bs = "";
                string bsFWM = "";
                bool FlagICN6 = false;
                bool FlagException = false;
                string cx = vCRYSTALDESC[k].ToString();
                string Extra = getExtra(cx);

                if ((fab == "ICN6" && waferSize.ToString() == "6") || Extra == "NOTInSAP")
                {
                    FlagException = true;
                }
                if (FlagException == true)
                {
                    string[] myWaferInfo;
                    string myWaferBatch = "";
                    string Slice = "";
                    if (Extra == "NOTInSAP")
                    {
                        int xxx = lOCRIDInput.Count();
                        myWaferInfo = arr[k].ToString().Split('-');
                        int SliceLen = myWaferInfo[0].ToString().Length;
                        Slice = myWaferInfo[1].ToString();
                    }
                    if (fab == "ICN6" && waferSize.ToString() == "6")
                    {
                        myWaferInfo = lstOCRID.Items[k].ToString().Split('-');
                        myWaferBatch = "W-" + myWaferInfo[0].ToString().Substring(0, 6) + "-0" + myWaferInfo[1].ToString().Substring(0, 2);
                        string[] WInfo = myWaferInfo[1].Split('.');
                        try
                        {
                            Slice = WInfo[0].ToString();
                        }
                        catch
                        {

                        }
                    }
                    int QtyOcr = Convert.ToInt32(lblScanQty.Text);
                    if (fab == "DHAM" & QtyOcr > 0)
                    {
                        try
                        {
                            bsFWM = lstOCRID.Items[0].ToString();
                            lstOCRIDSuccess.Items.Add(lstOCRID.Items[0].ToString());
                            lstOCRID.Items.RemoveAt(0);// check later Lotfi  
                        }
                        catch
                        {
                        }

                    }
                }
                else
                {
                    bs = fabname(fab, ws1, waferSize);
                    if (fab == "SILAN")
                    { bsFWM = bs; }
                    else
                    {
                        bsFWM = GenerateCheckChar(bs);
                    }
                }
                if ((FlagException == false) || (FlagException == true && FlagICN6 == true) || fab == "DHAM")
                {
                    if (fab.Trim() == "PHENITEC")
                    {
                        bsFWM = bsFWM.Substring(0, bsFWM.Length - 2);
                    }
                    string bs1 = bsFWM;  //lotfi
                    string myGroupFWM = "";
                    if (Extra == "NOTInSAP" && txt2DScan.Text.Length < 1)
                    {
                    }
                    if ((fab == "DHAM" || fab == "ICN6" || fab == "ICN8") && cx.Substring(0, 1).ToString() == "8" && Extra != "SWM")
                    {
                        myGroupFWM = "f";
                    }
                    //NOTInSAP use FWM
                    if (Extra == "NOTInSAP")
                    {
                        myGroupFWM = "f";
                    }
                    string dev = vDevice[k].ToString();
                    string crystalDmanExcludeValue = "False"; // not use
                    swmfwm = createFWMSaw(cx, bs1, SITwin, txtSOID, cx, dev, myGroupFWM, fab, crystalDmanExcludeValue);
                    string stringToCheck = swmfwm.ToString();
                    string[] stringArray = { "]", "01", "S" };
                    SawPrefix = "";
                    foreach (string x in stringArray)
                    {
                        if (stringToCheck.Contains(x))
                        {
                            SawPrefix = x.ToString();
                        }
                    }
                    lPACKAGE.Add(package);
                    MyOCRID.Add(swmfwm);
                    lSOID.Add(vLotID[k].ToString());
                    lDDATE.Add(DDate);
                    lDEVICE.Add(vDevice[k].ToString());
                    string wb = vWaferBatch[k].ToString();
                    if (swmfwm.Substring(0, 1).ToString() == "S")
                    {
                        lOCRID.Add(SawPrefix + bs1);
                        //  lOCRID.Add("#$" + SawPrefix + bs1);    //refer to badrul to remove manual entry, 5-mar-2018
                        hOCRID.Add(SawPrefix + bs1);
                    }
                    else if (!swmfwm.ToUpper().Contains("NOSWM") && ((swmfwm.Substring(0, 1).ToString() == "f") || (myGroupFWM == "f")))
                    {
                        lOCRID.Add(bsFWM);
                        hOCRID.Add(bsFWM);
                    }
                    else if (swmfwm == "TWIN")
                    {
                        lOCRID.Add(bs1 + "TWIN");  //lotfi, not sure yet
                        hOCRID.Add(bs1 + "TWIN");
                    }
                    else
                    {
                        lOCRID.Add("NO MAP");
                        hOCRID.Add("NO MAP" + "-" + wb);
                    }

                    if (fab == "DHAM" && swmfwm.Substring(0, 1).ToString() != SawPrefix)
                    {
                        myBin = "1-7";
                        lTOTALWAFER.Add(vTOTALWAFER[k].ToString());
                    }
                    else
                    {
                        myBin = "1";
                    }
                    lBIM.Add(cbBimline.Text);
                    Bin.Add(myBin);
                    lQUANTITY.Add(vQTY[k].ToString());
                    lCRYSTAL.Add(vCRYSTALDESC[k].ToString());
                    crystal.Add(vCRYSTALDESC[k].ToString());
                    lBADGEID.Add(txtBadgeID.ToString());
                    lTOTALWAFER.Add(vTOTALWAFER[k].ToString());
                    lDIETYPE.Add(SITwin);
                    lWAFERBATCH.Add(vWaferBatch[k].ToString());
                }
            }
            try
            {
                int catcherror = Convert.ToInt16(lTOTALWAFER[0]);
            }
            catch
            {
                return;
            }
            filePath1 = filePathTemplate + "\\" + "saw.LBL";
            if (filePath1 == "")
            {
                return;
            }
            int j = 0;
            string[] LabelDataTotal = new string[1000000];
            int tTotal = 0;
            j = lOCRID.Count - 1;   //asap
            // j = Convert.ToInt16(lTOTALWAFER[0]) - 1; dont use this since there is a glitch in MES whre the qty not tally.
            if (lstOCRIDSuccess.Items.Count > 0)
            {
                j = lstOCRIDSuccess.Items.Count - 1;
            }
            int ilblScanQty = 0;
            int iTotalWafer = 0;
            try
            {
                ilblScanQty = Convert.ToInt32(lblScanQty.Text);
            }
            catch
            {
            }
            if (Convert.ToInt32(lblScanQty.Text) > 0)
            {
                iTotalWafer = ilblScanQty;
            }
            else
            {
                iTotalWafer = vCRYSTALDESC.Count();
            }
            for (j = 0; j < iTotalWafer; j++)
            {
                const Int32 BufferSize = 1024;
                string[] LabelData = new string[1000];
                int i = 0;

                bool chk = true;

                using (var fileStream = File.OpenRead(filePath1))

                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    string line;
                    // int j = Convert.ToInt16(lTOTALWAFER[1].ToString());
                    while ((line = streamReader.ReadLine()) != null)
                    {
                        try
                        {
                            LabelData[i] = line;
                            if (line.Contains("#10#") == true)
                            {
                                string sData = LabelData[i].Replace("#10#", "");
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#SOID#") == true)
                            {
                                string sData = LabelData[i].Replace("#SOID#", lSOID[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#OCRID#") == true && line.Contains("^B") == true)
                            {
                                string nmap0 = "";
                                if (lOCRID[j].Contains("NO MAP") == true)
                                {
                                    string[] nmap = lOCRID[j].Split('-');
                                    nmap0 = nmap[0].ToString();
                                }
                                else
                                {
                                    nmap0 = lOCRID[j];
                                }

                                string sData = LabelData[i].Replace("#OCRID#", nmap0);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#hOCRID#") == true && line.Contains("CI") == true)
                            {
                                string nmap0 = "";
                                if (hOCRID[j].Contains("NO MAP") == true)
                                {
                                    string[] separator = new[] { "-W-" };
                                    string[] nmap = hOCRID[j].ToString().Split(separator, StringSplitOptions.RemoveEmptyEntries);
                                    nmap0 = nmap[0].ToString() + "-" + nmap[1].ToString();
                                }
                                else
                                {
                                    nmap0 = hOCRID[j];
                                }

                                string sData = LabelData[i].Replace("#hOCRID#", nmap0);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#DDATE#") == true)
                            {
                                string sData = LabelData[i].Replace("#DDATE#", lDDATE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#CRYSTAL#") == true)
                            {
                                string sData = LabelData[i].Replace("#CRYSTAL#", lCRYSTAL[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            #region unused method
                            //if (line.Contains("#CRYSTAL#") == true && line.Contains("^B") == true) //20250312-JB-add rule
                            //{
                            //    string sData = LabelData[i].Replace("#CRYSTAL#", lCRYSTAL[j]);
                            //    LabelData[i] = sData;
                            //    LabelDataTotal[tTotal] = sData;
                            //}
                            //if (line.Contains("#hCRYSTAL#") == true && line.Contains("CI") == true) //20250312-JB-add code for barcode
                            //{
                            //    string sData = LabelData[i].Replace("#hCRYSTAL#", lCRYSTAL[j]);
                            //    LabelData[i] = sData;
                            //    LabelDataTotal[tTotal] = sData;
                            //}
                            #endregion
                            if (line.Contains("#BIM#") == true)
                            {
                                string sData = LabelData[i].Replace("#BIM#", lBIM[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#QUANTITY#") == true)
                            {
                                string sData = LabelData[i].Replace("#QUANTITY#", lQUANTITY[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#PRODUCT#") == true)
                            {
                                string sData = LabelData[i].Replace("#PRODUCT#", lDEVICE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BADGEID#") == true)
                            {
                                string sData = LabelData[i].Replace("#BADGEID#", txtBadgeID);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#TOTALWAFER#") == true)
                            {
                                string sData = LabelData[i].Replace("#TOTALWAFER#", lTOTALWAFER[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#DIETYPE#") == true)
                            {
                                string sData = LabelData[i].Replace("#DIETYPE#", lDIETYPE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BOX#") == true)
                            {
                                string sData = LabelData[i].Replace("#BOX#", "");
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#BIN#") == true)
                            {
                                string sData = LabelData[i].Replace("#BIN#", Bin[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }
                            if (line.Contains("#PACKAGE#") == true)
                            {
                                string sData = LabelData[i].Replace("#PACKAGE#", lPACKAGE[j]);
                                LabelData[i] = sData;
                                LabelDataTotal[tTotal] = sData;
                            }

                            i = i + 1;
                            tTotal = tTotal + 1;
                            if (chk == false)
                            {

                            }
                        }
                        catch
                        {

                        }
                    }
                    if (chk == true)
                    {
                        writeSettingSO3(DataPath, PrinterPath, txtQty, "aa", "bb", LabelData, lSOID[j], template.ToUpper(), lOCRID[j], lBIM[j], lQUANTITY[j], lCRYSTAL[j], crystal[j], lBADGEID[j], lTOTALWAFER[j], lDIETYPE[j], lWAFERBATCH[j], MyOCRID[j], hOCRID[j]);
                    }
                }
                tTotal = tTotal - 1;
            } // loop for labelling
            writeSettingTotal(DataPath, PrinterPath, txtQty, "aa", "bb", LabelDataTotal, lSOID[0], template.ToUpper(), lTOTALWAFER[0]);
        }

        public string getWafermapQty(string ocrid) //(string [] ocrid1)
        {
            //2020-Dec-04 JB change method to get Qty
            string urlFWM = @"http://myser01ms056/wafermap/" + ocrid + ".142";//http://myser01ms056/ Changed web URL //string urlFWM = @"http://myser01ms056/wafermap/" + item + ".txt";//http://myser01ms056/ Changed web URL, 20Oct23 JB change .142 to .txt is not work for Half and Quarter
            string content1 = "";
            if (IsAddressAvailable(urlFWM) == false) //202012231417-modified by JB
            {
                return "0";
            }
            else
            {
                using (WebResponse wr = WebRequest.Create(urlFWM).GetResponse())
                {
                    using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                    {
                        content1 = sr.ReadToEnd();
                        List<string> objbin = new List<string>();
                        objbin.Add("BinCode=\"01\"");
                        objbin.Add("BinCode=\"02\"");
                        objbin.Add("BinCode=\"03\"");
                        objbin.Add("BinCode=\"04\"");
                        objbin.Add("BinCode=\"05\"");
                        objbin.Add("BinCode=\"06\"");
                        objbin.Add("BinCode=\"07\"");
                        string s = content1;
                        QtySum = 0;
                        for (int i1 = 0; i1 < objbin.Count(); i1++)
                        {
                            bool val = s.Contains(objbin[i1]);
                            if (val == true)
                            {
                                int lenfind = s.IndexOf(objbin[i1]);
                                string sind = s.Substring(lenfind, 100);

                                int LenStart = sind.IndexOf(objbin[i1]);
                                int LenEnd = sind.IndexOf("\" Pick=\"true\"");
                                bool valbin = sind.Contains(" Pick=\"true\"");
                                if (valbin == true)
                                {
                                    QtySum = QtySum + int.Parse(sind.Substring(LenStart + 63, LenEnd - (LenStart + 63)));
                                }
                                else
                                {
                                    QtySum = 0;
                                }
                            }
                        }
                    }
                    #region Unused Methods
                    //string awacPathLogPath = readawacsPath();
                    //string[] AP = awacPathLogPath.Split(',');
                    //string awacsPath = AP[0].ToString();
                    //string LogPath = AP[1].ToString();

                    //string MyDate = DateTime.Now.ToString("dd-MMM-yyyy_HH:mm:ss");
                    //// string myhost = "localhost:8081";
                    //string myhost = awacsPath;

                    //string urlFWM = @"http://" + myhost + @"/template/awacs_cloudsearch.html?cloudsearcho=" + ocrid + "&cloudsearchp=*&cloudsearcht=*WMAP&cloudsearchwo=*&cloudsearchws=*&cloudsearchr=*&cloudsearchd=*";
                    //urlFWM = @"http://mygsermys1ms026/wafermap/" + ocrid + ".txt";
                    //string content = "";
                    //string Qty = "";

                    //var filePath1 = Server.MapPath("~/Config/");
                    //string myfile = filePath1 + "NP64206-16-B0.142";

                    //try
                    //{
                    //    using (WebResponse wr = WebRequest.Create(urlFWM).GetResponse())
                    //    {
                    //        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                    //        {
                    //            content = sr.ReadToEnd();

                    //            XDocument doc = XDocument.Load(myfile);
                    //            XNamespace ns = "http://ns.adobe.com/xfdf/";

                    //            var field = doc.Descendants(ns + "Layout").Where(x => (string)x.Attribute("name") == "Dimension").FirstOrDefault();

                    //            if (field != null)
                    //            {
                    //                string value = (string)field.Element("value");
                    //                // Use value here
                    //            }

                    //            var xDoc = XDocument.Parse(myfile);
                    //            var unitBalance = "";
                    //            var dailyConsumption = "";

                    //            var r = from x in xDoc.Descendants("Layouts")
                    //                    select new
                    //                    {
                    //                        unitBalance = x.Element("Dimension").Value,
                    //                        dailyConsumption = x.Element("DeviceSize").Value
                    //                    };

                    //            var Body = xDoc.Root.Element("Dimension");
                    //            var Lot = Body.Element("BinDescription");
                    //            var MWO = Body.Element("MWO");
                    //            var EqId = Body.Element("EQID");

                    //            string s = content;
                    //            string mySearch = "</GoodDevices>";
                    //            //      <GoodDevices>104751</GoodDevices>
                    //            bool val = s.Contains(mySearch);
                    //            if (val == true)
                    //            {
                    //                int LenStart = s.IndexOf("<GoodDevices>");
                    //                int LenEnd = s.IndexOf("</GoodDevices>");

                    //                Qty = s.Substring(LenStart + 13, LenEnd - (LenStart + 13));
                    //                return Qty;
                    //            }
                    //            else
                    //            {
                    //                return Qty;
                    //            }
                    //        }
                    //    }
                    //}
                    //catch
                    //{
                    //    return Qty;
                    //}
                    #endregion Unused Methods
                }
                return QtySum.ToString();
            }
        }

        public string fabname(string fab, string waferbatch, string wafersize)
        {
            string myWaferBatch = "none";
            string[] wf = waferbatch.Split('-');
            int ChrCnt = waferbatch.Split('-').Length - 1 ; //202011281206-modifired by JB
            if ((fab.Trim() == "ASMC-3") || (fab.Trim() == "ASMC3")) 
            { 
                myWaferBatch = wf[1].Substring(0, 6) + "-" + wf[2].Substring(1, 2); 
            }
            if (fab.Trim() == "Calamba") 
            { 
                myWaferBatch = wf[1].Substring(0, 5) + "W" + wf[2].Substring(1, 2) + "-"; 
            }
            if (fab.Trim() == "DHAM")
            {
                if (ChrCnt.ToString() == "1") //202011281206-modifired by JB
                {
                    myWaferBatch = wf[1].Substring(0, 5) + "W" + wf[1].Substring(6, 2) + "-";
                }
                else
                {
                    if ((wf[1].Length == 7 || wf[1].Length == 8) && !wf[1].Contains(".")) //20211112-modifired by JB
                    {
                        myWaferBatch = wf[1].Substring(0, 7) + "-" + wf[2].Substring(1, 2) + "-"; //20200814-modifired by JB
                    }
                    if ((wf[2].Length == 2) && !wf[2].Contains("."))
                    {
                        myWaferBatch = wf[1].Substring(0, 5) + "W" + wf[2].Substring(0, 2) + "-";
                    }
                    if ((wf[2].Length != 2) && (wf[1].Length != 7) && (wf[1].Length != 8) && !wf[2].Contains(".")) //20201220-modifired by JB
                    {
                        myWaferBatch = wf[1].Substring(0, 5) + "W" + wf[2].Substring(1, 2) + "-";
                    }
                    if (wf[1].Contains("."))
                    {
                        //myWaferBatch = wf[1].Substring(0, 6) + "-" + Convert.ToString(Convert.ToInt32(wf[2].ToString()));  //not use. 
                        myWaferBatch = wf[1].Substring(0, 6) + "-" + wf[2].Substring(1, 2);
                    }
                }
            }
            if (fab.Trim() == "DHZG") 
            {
                myWaferBatch = wf[1].Substring(0, 6) + "-" + wf[2].Substring(1, 2); 
            }
            if (fab == "DMAN")
            {
                string NoDashed = wf[1].Substring(0, 7) + "-" + wf[2].Substring(1, 2) + "-";
                myWaferBatch = NoDashed;
                #region Unused Methods 
                //202109101651-commented by JB
            // if (fab.Trim() == "DMAN") { myWaferBatch = wf[1].Substring(0, 7) + "  " + wf[2].Substring(1, 2) + "  "; }//{ myWaferBatch = wf[1].Substring(0, 7) + "-" + wf[2].Substring(1, 2); }
            //// if (fab.Trim() == "DMAN") { myWaferBatch = wf[1].Substring(0, 7) + "-" + wf[2].Substring(1, 2) + "-"; }
                //string msg = "";
                //string withDashed = myWaferBatch = wf[1].Substring(0, 7) + "  " + wf[2].Substring(1, 2) + "  ";
                //string NoDashed = wf[1].Substring(0, 7) + "-" + wf[2].Substring(1, 2) + "-";
                //msg = msg + waferbatch + " is DHAM wafer " + Environment.NewLine + Environment.NewLine;
                //msg = msg + Environment.NewLine + Environment.NewLine + "PLEASE CHECK PHYSICAL WAFER OCRID" + Environment.NewLine;
                //msg = msg + Environment.NewLine + "1. Select YES if OCRID has spaces " + "Example " + withDashed + "  XX";
                //msg = msg + Environment.NewLine + "2. Select NO if OCRID has not spaces " + "Example " + NoDashed + "XX";
                //DialogResult result = MessageBox.Show(msg, "Warning: Actual OCRID may not be same as in Camstar",
                //MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly);

                //if (result == DialogResult.Yes)
                //{
                //    myWaferBatch = withDashed;
                //}
                //else
                //{
                //    myWaferBatch = NoDashed;
                //}
                #endregion Unused Methods
            }
            if ((fab.Trim() == "GF-2") || (fab.Trim() == "GF2"))
            {
                string[] Param = wf[1].ToString().Split('.');
                myWaferBatch = Param[0].ToString() + "W" + wf[2].Substring(1, 2);
            }
            if (fab.Trim() == "ICH")
            {
                myWaferBatch = wf[1].Substring(0, 4) + "W" + wf[2].Substring(1, 2) + "-";
            }
            if (fab.Trim() == "ICN4")
            {
                myWaferBatch = wf[1].ToString() + "-" + wf[2].Substring(1, 2) + "-";
            }
            if (fab.Trim() == "ICN6")
            {
                myWaferBatch = wf[1].ToString() + "F" + "-" + wf[2].Substring(1, 2) + ".";
            }
            if (fab.Trim() == "ICN6" && wafersize == "6")
            {
                myWaferBatch = wf[1].ToString() + "F" + "-" + wf[2].Substring(1, 2) + ".";
            }
            if (fab.Trim() == "ICN6" && wafersize == "8")
            {
                myWaferBatch = wf[1].Substring(0, 6) + "W" + wf[2].Substring(1, 2);
            }
            if (fab.Trim() == "ICN8")
            {//C1K900W19D2, C1K178, W-C3K340-0-1
                if ((wf[1].Length == 8) && wf[1].Contains(".")) //202206021106-modified by JB
                {
                    myWaferBatch = wf[1].Substring(0, 6) + "-" + wf[2].Substring(1, 2); //202206021106-modified by JB
                }
                else if (wf[1].Length == 6 && wf[2].Length == 1 && wf[3].Length == 1) //W-C3K340-0-1, 202404291031-modified by JB
                {
                    myWaferBatch = wf[1].Substring(0, 6) + "W" + wf[2].Substring(0, 1) + wf[3].Substring(0, 1); //202404291031-modified by JB
                }
                else
                {
                    myWaferBatch = wf[1].Substring(0, 6) + "W" + wf[2].Substring(1, 2);
                }
            }
            if ((fab.Trim() == "M-MOS") || (fab.Trim() == "M-MOSHK"))
            {
                myWaferBatch = wf[1].ToString().Substring(0, 6) + "-" + wf[2].Substring(1, 2) + "-";
            }
            if (fab.Trim() == "MOSEL")
            {
                myWaferBatch = wf[1].Substring(0, 7) + "-" + wf[2].Substring(1, 2) + "-";
            }
            if (fab.Contains("MMS/GA") == true)
            {
                myWaferBatch = wf[1].Substring(0, 5) + "W" + wf[2].Substring(1, 2) + "-";
            }
            if (fab.Trim() == "PHENITEC")
            {
                myWaferBatch = wf[1].Substring(0, 4) + "-" + wf[1].Substring(4, 4) + "-" + wf[2].Substring(1, 2);
            }
            if (fab.Trim() == "SILAN")
            {
                myWaferBatch = wf[1].ToString() + "-" + wf[2].Substring(1, 2);
            }
            if (fab.Trim() == "SSMC")
            {
                char[] delimiters = new char[] { '.', '-' };
                string[] parts = wf[1].ToString().Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                myWaferBatch = parts[0].ToString() + "-" + wf[2].Substring(1, 2);
            }
            if ((fab.Trim() == "VANG") || (fab.Trim() == "VANGUARD"))
            {
                if (wf[1].ToString().Contains(".") == true)
                {
                    string[] Param = wf[1].ToString().Split('.');
                    myWaferBatch = Param[0].ToString() + "-" + wf[2].Substring(1, 2);
                }
                else
                {
                    string[] Param = new string[2];
                    Param[0] = wf[1].Substring(0, 6);
                    myWaferBatch = Param[0].ToString() + "-" + wf[2].Substring(1, 2);
                }
            }
            if (fab.Trim() == "WSS-DTJX")
            { //JB-20250805, MWO:P132805637, WAFERSCRIBEMUNBER:W-AE00749-009 to ocrid:AE00749-09-A0
                myWaferBatch = wf[1].Substring(0, 7) + "-" + wf[2].Substring(1, 2) + "-";
            }
            if (fab.Trim() == "ATBK")
            { //W-X2311100-001=X23111-01G4 W-C0H963-014=C0H963W14D6 20210927-modified by JB
                if (EnaDisATBK == "true")
                {
                    if (wf[1].Length == 8)
                    {
                        myWaferBatch = wf[1].Substring(0, wf[1].Length - 2) + "-" + wf[2].Substring(1, 2);
                    }
                    else if (wf[1].Length == 6)
                    {
                        myWaferBatch = wf[1].Substring(0, wf[1].Length) + "W" + wf[2].Substring(1, 2);
                    }
                    else if (wf[1].Length == 9)
                    {//C1D014W09B0
                        myWaferBatch = wf[1].Substring(0, wf[1].Length - 3) + "W" + wf[2].Substring(1, 2);
                    }
                }
                else
                {
                    Response.Write("<script>alert('Warning: Wafer FAB " + fab.Trim() + " invalid!');</script>");
                }
            }
            if (fab.Trim() == "UMC8C")
            { //20240717-modified by JB - new fabname:UMC8C, map format:DMRPL11-E2, CGI map slice:W-DMRPL.1-011
                if (wf[1].Length == 7)
                {
                    myWaferBatch = wf[1].Substring(0, wf[1].Length - 2) + wf[2].Substring(1, 2) + "-";
                }
                else
                {
                    Response.Write("<script>alert('Warning: Wafer FAB " + fab.Trim() + " invalid!');</script>");
                }
            }


            return myWaferBatch;
        }

        public string GenerateCheckChar(string InputStr)
        {
            int Checksum = 0;
            int Char1;
            int Char2;
            string CheckSumVal = "";

            Checksum = CalcChecksum(InputStr + "A0");
            if (Checksum == 0)
            {
                CheckSumVal = InputStr + "A0";
            }
            else
            {
                Checksum = 59 - Checksum;
                Char2 = 16 + (Checksum & 7);
                Char1 = 33 + (Checksum & 56) / 8;
                CheckSumVal = InputStr + (char)(32 + Char1) + (char)(32 + Char2);
            }

            return CheckSumVal;
        }

        public int CalcChecksum(String InputStr)
        {
            int Checksum;
            int i;
            Checksum = 0;
            for (i = 1; (i <= InputStr.Length); i++)
            {
                Checksum = (((8 * Checksum) + (Strings.Asc(InputStr.Substring((i - 1), 1)) - 32)) % 59);
            }
            return Checksum;
        }

        public string readawacsPath()
        {
            var filePath1 = Server.MapPath("~/Config/awacsPath.txt");
            const Int32 BufferSize = 1024;
            using (var fileStream = File.OpenRead(filePath1))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string line;
                string sAwacsPathLogPath = "";
                while ((line = streamReader.ReadLine()) != null)
                {
                    try
                    {
                        sAwacsPathLogPath = line;
                    }
                    catch
                    { 
                    
                    }
                }
                return sAwacsPathLogPath;
            }
        }

        string Qty1 = "";
        int QtySum;
        public string createFWMSaw(string CRYSTALDESC, string ocrid, string SITwin, string SOID, string cx, string device, string groupFWM, string fab, string crystalDmanExcludeValue)
        {
            // crystal

            string awacPathLogPath = readawacsPath();
            string[] AP = awacPathLogPath.Split(',');
            string awacsPath = AP[0].ToString();
            string LogPath = AP[1].ToString();

            string swm = "";
            string MyResult = "";
            string path = LogPath + "\\" + "SWMtoFWM.txt";
            string MyDate = DateTime.Now.ToString("dd-MMM-yyyy_HH:mm:ss");
            // string myhost = "localhost:8081";
            string myhost = awacsPath;
            // string myhost = "myser01ms056:3456";
            using (SqlConnection repDbConn = new SqlConnection(ConfigurationManager.ConnectionStrings["RECIPE"].ConnectionString))
            {
                repDbConn.Open();
                using (SqlCommand cmd = repDbConn.CreateCommand())
                {
                    //20210416-JB modified searching query step
                    //cmd.CommandText = "SELECT * FROM [WAFERRECIPE] TP ;";
                    //using (SqlDataReader reader = cmd.ExecuteReader())
                    //{
                    //    while (reader.Read())
                    //    {
                    //        crystaldescdB.Add(reader["WAFERNAME"].ToString());
                    //        RecipedB.Add(reader["RECIPE"].ToString());
                    //    }
                    //}
                    cmd.CommandText = "SELECT * FROM [WAFERRECIPE] WHERE WAFERNAME='" + CRYSTALDESC.ToString().ToUpper().Trim() + "'";
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            swm = reader["RECIPE"].ToString();
                        }
                    }
                }
                repDbConn.Close();
            }

            //for (int i = 0; i < crystaldescdB.Count; i++)
            //{
            //    if (crystaldescdB[i].ToString().ToUpper() == CRYSTALDESC.ToString().ToUpper().Trim())
            //    {
            //        swm = RecipedB[i].ToString();
            //    }

            //}

            string wafercode = "0";
            string statusFWM = "0";

            if (groupFWM != "") { statusFWM = "1"; }

            MySaw.saw sawPrefix = new MySaw.saw();
            if (cx.Contains("TA") == true || cx.Contains("TC")) { wafercode = "1"; }  //TATC

            if (wafercode == "1" & statusFWM == "1")
            {
                sawPrefix.Name = "]";
                SawPrefix = "]";
            }
            if (wafercode == "0" & statusFWM == "1")
            {
                sawPrefix.Name = "";
                SawPrefix = "";
            }
            if (wafercode == "1" & statusFWM == "0")
            {
                sawPrefix.Name = "0S";
                SawPrefix = "0S";
            }
            if (wafercode == "0" & statusFWM == "0")
            {
                sawPrefix.Name = "S";
                SawPrefix = "S";
            }
            if (groupFWM != "")
            {
                swm = "FWM";
            }
            if (swm != "" && swm != "FWM")
            {
                // string xx = GenerateCheckChar(ocrid);
                //  ocrid = SawPrefix + xx;
                if (SITwin == "TWIN")
                {
                    ocrid = SawPrefix + ocrid + "TWIN";
                    swm = swm + "_" + "TWIN";
                }
                else
                {

                }
                //string url = "http://" + myhost + "/template/createmapfromswm.html?auth=admin:admin&swm=" + swm + ".dm1&ocrid=" + SawPrefix + ocrid;
                //2026Mar12-JB-dm1 format
                string url = "http://" + myhost + "/template/createmapfromswm.html?auth=swm:swm54321&swm=" + swm + ".dm1&ocrid=" + SawPrefix + ocrid; //2021Jan22 modified awacs admin account
                string content = "";

                string[] awacsresponse = new string[] { "swm is not valid\n", "failed\n" };

                using (WebResponse wr = WebRequest.Create(url).GetResponse())
                {
                    using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                    {
                        content = sr.ReadToEnd();

                        if (awacsresponse.Contains(content) == false)
                        {
                            MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                            swm = SawPrefix + ocrid;
                        }
                    }
                }

                if (awacsresponse.Contains(content) == true) // if (content.Contains("swm is not valid") == true)
                {////2026Mar12-JB-dms format
                    url = "http://" + myhost + "/template/createmapfromswm.html?auth=swm:swm54321&swm=" + swm + ".dms&ocrid=" + SawPrefix + ocrid;

                    content = "";
                    using (WebResponse wr = WebRequest.Create(url).GetResponse())
                    {
                        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                            if (awacsresponse.Contains(content) == false)
                            {
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                                swm = SawPrefix + ocrid;
                            }
                        }
                    }
                }

                if (awacsresponse.Contains(content) == true) // if (content.Contains("swm is not valid") == true)
                {//2026Mar12-JB-xml format
                    url = "http://" + myhost + "/template/createmapfromswm.html?auth=swm:swm54321&swm=" + swm + ".xml&ocrid=" + SawPrefix + ocrid;

                    content = "";
                    using (WebResponse wr = WebRequest.Create(url).GetResponse())
                    {
                        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                            if (awacsresponse.Contains(content) == false)
                            {
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                                swm = SawPrefix + ocrid;
                            }
                        }
                    }
                }

                if (awacsresponse.Contains(content) == true) // if (content.Contains("swm is not valid") == true)
                {//2026Mar12-JB-142 format
                    url = "http://" + myhost + "/template/createmapfromswm.html?auth=swm:swm54321&swm=" + swm + ".142&ocrid=" + SawPrefix + ocrid;

                    content = "";
                    using (WebResponse wr = WebRequest.Create(url).GetResponse())
                    {
                        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                            if (awacsresponse.Contains(content) == false)
                            {
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                                swm = SawPrefix + ocrid;
                            }
                        }
                    }
                }

                if (awacsresponse.Contains(content) == true)  //if (content.Contains("swm is not valid") == true)
                {//20260313-JB-modified SWM path to config
                    string fp = @"c:\awacs\site\swm\" + swm + ".xml";
                    string fp1 = string.Format(ConfigurationManager.AppSettings["SourSWMPath"], swm);
                    //   string fpnew = @"c:\awacs\site\swm\" + ocrid + ".xml";  /remove for server
                    string fpnew = @"\\myser01ms056\swm\" + SawPrefix + ocrid + ".xml";
                    string fpnew1 = string.Format(ConfigurationManager.AppSettings["DestSWMPath"], SawPrefix, ocrid);
                    if (File.Exists(fp))
                    {
                        try
                        {
                            if (File.Exists(fpnew))
                            {
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + "file already exist";
                                swm = SawPrefix + ocrid;
                            }
                            else
                            {
                                File.Copy(fp, fpnew);
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + "copy xml success";
                                swm = SawPrefix + ocrid;
                            }

                        }
                        catch
                        {
                            MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + "copy xml fail";
                            swm = "noSWM";
                        }

                    }
                    else
                    {
                        MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + "copy xml fail";
                        swm = "noSWM";
                    }
                }
                File.AppendAllLines(path, new[] { MyResult });
                return swm;
            }
            else if (swm == "FWM" && (fab != "SILAN" && fab != "DMAN"))
            {
                //ocrid = ocrid.Substring(1, ocrid.Length - 1);
                ocrid = ocrid.Substring(0, ocrid.Length);
                string urlFWM = @"http://" + myhost + @"/template/awacs_cloudsearch.html?cloudsearcho=" + ocrid + "&cloudsearchp=*&cloudsearcht=*WMAP&cloudsearchwo=*&cloudsearchws=*&cloudsearchr=*&cloudsearchd=*";
                //urlFWM = @"http://mygsermys1ms026/wafermap/" + ocrid + ".xml";
                urlFWM = @"http://myser01ms056/wafermap/" + ocrid + ".xml";

                string content = "";

                if (SawPrefix == null)
                {
                    SawPrefix = "";
                }
                try
                {
                    using (WebResponse wr = WebRequest.Create(urlFWM).GetResponse())
                    {
                        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();

                            string s = content;
                            bool val = s.Contains(ocrid);
                            if (val == true)
                            {
                                var filePath1 = Server.MapPath("~/Config/");
                                System.IO.File.WriteAllText(filePath1 + SawPrefix + ocrid + ".xml", s);
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + ocrid + "," + swm + "," + "FWM aded";
                                File.AppendAllLines(path, new[] { MyResult });
                                swm = ocrid;
                                return "f" + swm;
                            }
                            else
                            {
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + ocrid + "," + swm + "," + "FWM not available";
                                File.AppendAllLines(path, new[] { MyResult });
                                return "noSWM";
                            }
                        }
                    }
                }
                catch
                {
                    MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + ocrid + "," + swm + "," + "FWM not available";
                    File.AppendAllLines(path, new[] { MyResult });
                    return "noSWM";
                }
            }
            else if (swm == "FWM" && (fab == "SILAN" || (fab == "DMAN" && crystalDmanExcludeValue == "True")))//fab == "DMAN"))
            {
                if (fab == "SILAN") { ocrid = ocrid.Substring(1, ocrid.Length - 1); }
                if (fab == "DMAN") { ocrid = ocrid.Substring(0, ocrid.Length); }
                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + ocrid + "," + swm + "," + "FWM added";
                File.AppendAllLines(path, new[] { MyResult });
                swm = ocrid;
                return "f" + swm;
            }

            else
            {
                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + ocrid + "," + swm + "," + "swm file not found";
                File.AppendAllLines(path, new[] { MyResult });
                return "noSWM";

            }
        }

        protected string writeSettingTotal(string DataPath, string PrinterPath, string txtQty, string fpath, string fvalue, string[] MyValue, string WOID, string template, string lTOTALWAFER)
        {
            try
            {
                string awacPathLogPath = readawacsPath();
                string[] AP = awacPathLogPath.Split(',');
                string awacsPath = AP[0].ToString();
                string LogPath = AP[1].ToString();

                string myBarcode = "";
                System.IO.File.Delete(LogPath + "\\" + WOID + ".tmp");
                using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(LogPath + "\\" + WOID + ".tmp"))
                {
                    int i = 0;
                    do
                    {
                        file.WriteLine(MyValue[i]);
                        myBarcode += MyValue[i];
                        i = i + 1;  //add here to ensure value is start at one and data start from zero
                        myTotalBarcode += MyValue[i];
                    }

                    while (!String.IsNullOrEmpty(MyValue[i]) && i < MyValue.Length);
                }
                try
                {
                    var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(myBarcode);
                    //HtmlGenericControl NewControl = new HtmlGenericControl("button");

                    //Session["jsonBarcode"] = json;

                    Gv1.DataBind();

                    string prt = "printStruk(" + json + ",\"" + btnPrintAll.ClientID + "\")";
                    btnPrintAll.Attributes.Add("value", "");
                    btnPrintAll.Attributes.Add("onclick", prt);
                    #region Commented By SRI
                    // Set the properties of the new HtmlGenericControl control.
                    //'<input type="button" onClick="gotoNode(\'' + result.name + '\')" />'

                    //string prt = "printStruk(" + json + ")";
                    //NewControl.ID = "MyButton";
                    //NewControl.InnerText = "Print All";
                    //NewControl.Attributes.Add("value", "");
                    //NewControl.Attributes.Add("onclick", prt);
                    ////PlaceHolder1.Controls.Add(new HtmlGenericControl("br"));
                    //PlaceHolder1.Controls.Add(new LiteralControl("<br/>"));
                    //PlaceHolder1.Controls.Add(NewControl);
                    //PlaceHolder1.Controls.Add(new LiteralControl("<br/>"));
                    #endregion Commented By SRI
                }
                catch (Exception ex)
                {
                    // lblStatus.Text = ex.ToString();
                }
            }
            catch
            {
            }
            return myTotalBarcode;
        }
        List<string> objjsonind = new List<string>();//Modifications done by SRI
        List<string> objmap = new List<string>();//Modifications done by SRI
        List<string> objQty = new List<string>();//Modifications done by SRI
        protected string writeSettingSO3(string DataPath, string PrinterPath, string txtQty, string fpath, string fvalue, string[] MyValue, string WOID, string template, string lOCRID, string lBIM, string lQUANTITY, string lCRYSTAL, string crystal, string lBADGEID, string lTOTALWAFER, string lDIETYPE, string lWAFERBATCH, string swmfwm, string hOCRID)
        {
            try
            {
                string awacPathLogPath = readawacsPath();
                string[] AP = awacPathLogPath.Split(',');
                string awacsPath = AP[0].ToString();
                string LogPath = AP[1].ToString();

                string myBarcode = "";
                int Qty = Convert.ToInt16(txtQty);
                System.IO.File.Delete(LogPath + "\\" + WOID + ".tmp");
                using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(LogPath + "\\" + WOID + ".tmp"))
                {
                    int i = 0;
                    do
                    {
                        file.WriteLine(MyValue[i]);
                        myBarcode += MyValue[i];
                        i = i + 1;  //add here to ensure value is start at one and data start from zero
                        //myTotalBarcode += MyValue[i];
                    }

                    while (!String.IsNullOrEmpty(MyValue[i]) && i < MyValue.Length);
                }
                try
                {
                    string map;
                    if (swmfwm.Substring(0, 1) == SawPrefix)
                    {
                        map = swmfwm;
                    }
                    else if (swmfwm.Substring(0, 1) == "f")
                    {
                        map = swmfwm.Substring(1, swmfwm.Length - 1);
                    }
                    else
                    {
                        map = "NoMap";
                    }

                    objmap.Add(map);//Modifications done by SRI
                    var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(myBarcode);

                    objjsonind.Add(json);//lOCRID.Substring(0, 8), //Modifications done by SRI

                    #region Commented by SRI
                    //objmap.Add(map);
                    //Session["workmap"] = map;
                    //HtmlGenericControl NewControl = new HtmlGenericControl("button");

                    // Set the properties of the new HtmlGenericControl control.
                    //'<input type="button" onClick="gotoNode(\'' + result.name + '\')" />'

                    //string prt = "printStruk(" + json + ")";
                    //NewControl.ID = "MyButton";
                    //NewControl.InnerText = lCRYSTAL + " " + lWAFERBATCH + " " + lQUANTITY + " " + map;
                    //NewControl.Attributes.Add("value", "");
                    //NewControl.Attributes.Add("onclick", prt);
                    ////PlaceHolder1.Controls.Add(new HtmlGenericControl("br"));
                    //PlaceHolder1.Controls.Add(NewControl);
                    //PlaceHolder1.Controls.Add(new LiteralControl("<br/>"));
                    #endregion Commented by SRI
                }
                catch (Exception ex)
                {
                    // lblStatus.Text = ex.ToString();
                }

            }
            catch
            {
            }
            return myTotalBarcode;
        }

        #endregion Button Submit

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            //string MyOCRScan = "";
            string[] separators = { "12NC", "BATCH", "QTY","OCR" };
            string line = txt2DScan.Text;
            var fs = line.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);
            try
            {
                String newValue = fs[4].ToString();
                var item = lstOCRID.Items.FindByValue(newValue); // or FindByText
                if (item == null)
                {
                    lstOCRID.Items.Add(newValue);
                    lstOCRIDAll.Items.Add(fs[1].ToString() + "," + fs[2].ToString() + "," + fs[3].ToString() + "," + fs[4].ToString());
                }
                txt2DScan.Text = "";
            }
            catch
            { 

            }
        }

        public void Read2DScan()
        {
            //string MyOCRScan = "";
            string line = "";
            string[] separators = { "12NC", "BATCH", "QTY", "OCR" };
             if (txt2DScan.Text.ToString().Contains("OCR") == true)
             {
                line = txt2DScan.Text.Trim();
             }
             else
            {
                //[)> 06 12NC342207140341 BATCHL2D8L1 QTY72000 OCRL2D8L1D-24.H1 
                line = "[)> 06 12NC99 " + "BATCH99 " + "QTY99 " + "OCR" + txt2DScan.Text;         
            }

            var fs = line.ToString().Split(separators, StringSplitOptions.RemoveEmptyEntries);

            try
            {
                String newValue = fs[4].ToString();
                var item = lstOCRID.Items.FindByValue(newValue); // or FindByText
                if (item == null)
                {
                    lstOCRID.Items.Add(newValue);
                    lstOCRIDAll.Items.Add(fs[1].ToString() + "," + fs[2].ToString() + "," + fs[3].ToString() + "," + fs[4].ToString());
                }

                //if (lstOCRID.Items.Cast<Object>().Any(x => x.ToString() == newValue))
                //{
                //    lstOCRID.Items.Add(fs[4].ToString());
                //  //  return;
                //}
                txt2DScan.Text = "";
            }
            catch
            {

            }
        
        }

        protected void btnDelete_Click(object sender, EventArgs e)
        {
            int SelIndex = this.lstOCRID.SelectedIndex;
            if (this.lstOCRID.SelectedIndex >= 0) 
            { this.lstOCRID.Items.RemoveAt(SelIndex);
            this.lstOCRIDAll.Items.RemoveAt(SelIndex);            
            }
            lblScanQty.Text = lstOCRID.Items.Count.ToString();
            lOCRIDInput.Clear();
            for (int i = 0; i < lstOCRID.Items.Count; i++)
            {
                lOCRIDInput.Add(lstOCRID.Items[i].ToString());
            }      
        }

        protected void txt2DScan_TextChanged(object sender, EventArgs e)
        {
            try
            {
                //   "12NC", "BATCH", "QTY", "OCR" 
                if (!IsPostBack)
                {
                    if (txtBadgeID.Text.ToString().Length > 1 && txtSOID.Text.ToString().Length > 1 && txt2DScan.Text.ToString().Length > 1)
                    {
                        Response.Write("<script>alert('Warning: press ADD button to proceed');</script>");
                        return;
                    }
                    {
                        Read2DScan();
                    }                               
                }
            }
            catch
            {
                Response.Write("<script>alert('Warning: Invalid waferid scan');</script>");
                return;
            }


        }

        protected void btnAddOCRID_Click(object sender, EventArgs e)
        {
            try
            {
                //   "12NC", "BATCH", "QTY", "OCR" 
                if (txt2DScan.Text.ToString().Contains("OCR") == true || txt2DScan.Text.ToString().Length > 0)
                {
                    Read2DScan();
                    lblScanQty.Text = lstOCRID.Items.Count.ToString();
                    lOCRIDInput.Clear();
                    for (int i = 0; i < lstOCRID.Items.Count; i++)
                    {
                        lOCRIDInput.Add(lstOCRID.Items[i].ToString());
                    }
                }
            }
            catch
            {
                Response.Write("<script>alert('Warning: Invalid waferid scan');</script>");
                return;

            }   
        }
        
        #region Changes made by SRI for Grid & Radio Buttons

        protected void Gv1_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType == DataControlRowType.DataRow)
            {
                System.Web.UI.WebControls.Button btnButton = (System.Web.UI.WebControls.Button)e.Row.FindControl("btnprint");
                btnButton.Text = "Print__" + objmap[e.Row.RowIndex];//e.Row.Cells[2].Text;

                //var json = Session["jsonBarcode1"];

                var json = objjsonind[e.Row.RowIndex];

                string prt = "printStruk(" + json + ",\"" + btnButton.ClientID + "\")";//btnButton.ClientID
                btnButton.Attributes.Add("value", "");
                btnButton.Attributes.Add("printStruk", prt);
                btnButton.Attributes.Add("vDevice", vDevice[e.Row.RowIndex]);
                btnButton.Attributes.Add("vCONTAINERNAME", vCONTAINERNAME[e.Row.RowIndex]);
                btnButton.Attributes.Add("vCRYSTALDESC", vCRYSTALDESC[e.Row.RowIndex]);
                btnButton.Attributes.Add("vQTY", vQTY[e.Row.RowIndex]);
                btnButton.Attributes.Add("vMap", objmap[e.Row.RowIndex]);
                btnButton.Attributes.Add("vMWO", vMWO[e.Row.RowIndex]);

                //btnButton.Attributes.Add("onclick", prt);
                //btnPrintAll.Attributes.Add("vDevice", vDevice;
                //btnPrintAll.Attributes.Add("vWaferBatch", vWaferBatch);
                //btnPrintAll.Attributes.Add("vQTY", vQTY);

                btnButton.Attributes.Add("Bmapcode", json);

                objQty = lQUANTITY;

                System.Web.UI.WebControls.RadioButton rdbQButton = (System.Web.UI.WebControls.RadioButton)e.Row.FindControl("rdbQ");
                rdbQButton.Attributes.Add("Qmapcode", objmap[e.Row.RowIndex]);
                rdbQButton.Attributes.Add("Qquantity", objQty[e.Row.RowIndex]);

                System.Web.UI.WebControls.RadioButton rdbHButton = (System.Web.UI.WebControls.RadioButton)e.Row.FindControl("rdbH");
                rdbHButton.Attributes.Add("Hmapcode", objmap[e.Row.RowIndex]);
                rdbHButton.Attributes.Add("Hquantity", objQty[e.Row.RowIndex]);

                if (objmap[e.Row.RowIndex] == "NoMap")
                {
                    rdbQButton.Enabled = false;
                    rdbHButton.Enabled = false;
                }
            }
        }
        protected void rdbQ_CheckedChanged(object sender, EventArgs e)
        {
            //foreach (GridViewRow row in Gv1.Rows)
            {
                //System.Web.UI.WebControls.RadioButton chk = (System.Web.UI.WebControls.RadioButton)row.FindControl("rdbQ");
                System.Web.UI.WebControls.RadioButton Qrad = sender as System.Web.UI.WebControls.RadioButton;

                if (Qrad.Checked == true)
                {
                    //var MAP = (new System.Linq.SystemCore_EnumerableDebugView(chk.Attributes.Keys as System.Collections.IEnumerable)).Items[1];//Session["workmap"];//objmap[row.RowIndex];
                    var MAP = Qrad.Attributes["Qmapcode"].ToString();
                    string res = "";
                    for (int i = 1; i < 5; i++)
                    {
                        string item = "Q" + i + "__" + MAP;
                        string urlFWM = @"http://myser01ms056/wafermap/" + item + ".142";//http://myser01ms056/ Changed web URL //string urlFWM = @"http://myser01ms056/wafermap/" + item + ".txt";//http://myser01ms056/ Changed web URL, 20Oct23 JB change .142 to .txt is not work for Half and Quarter
                        string content1 = "";
                        if (IsAddressAvailable(urlFWM) == false) //202008141241-modified by JB
                        {
                            res += item + ":" + "NoMap" + ";";
                        }
                        else
                        {
                            using (WebResponse wr = WebRequest.Create(urlFWM).GetResponse())
                            {
                                using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                                {
                                    content1 = sr.ReadToEnd();
                                    List<string> objbin = new List<string>();
                                    objbin.Add("BinCode=\"01\"");
                                    objbin.Add("BinCode=\"02\"");
                                    objbin.Add("BinCode=\"03\"");
                                    objbin.Add("BinCode=\"04\"");
                                    objbin.Add("BinCode=\"05\"");
                                    objbin.Add("BinCode=\"06\"");
                                    objbin.Add("BinCode=\"07\"");
                                    objbin.Add("BinCode=\"0001\"");
                                    string s = content1;
                                    QtySum = 0;
                                    for (int i1 = 0; i1 <  objbin.Count(); i1++) //2020Oct23 JB loop the BIN 1 to 7
                                    {
                                        bool val = s.Contains(objbin[i1]);
                                        if (val == true)
                                        {
                                            int lenfind = s.IndexOf(objbin[i1]);
                                            string sind = s.Substring(lenfind, 100);

                                            int LenStart = sind.IndexOf(objbin[i1]); 
                                            int LenEnd = sind.IndexOf("\" Pick=\"true\"");
                                            bool valbin = sind.Contains(" Pick=\"true\"");
                                            if (valbin == true)
                                            {
                                                //20210930 JB if BinCode=0001
                                                if (objbin[i1].Length >= 14)
                                                {
                                                    QtySum = QtySum + int.Parse(sind.Substring(LenStart + 65, LenEnd - (LenStart + 65)));
                                                }
                                                else
                                                {
                                                    QtySum = QtySum + int.Parse(sind.Substring(LenStart + 63, LenEnd - (LenStart + 63))); //s.Substring(LenStart + 28, LenEnd - (LenStart + 28));//s.Substring(LenStart + 13, LenEnd - (LenStart + 13));
                                                }
                                            }
                                            else
                                            {
                                                if (QtySum == 0)
                                                {
                                                    res += item + ":" + "NoMap" + ";";
                                                }
                                            }
                                        }
                                    }
                                    res += item + ":" + QtySum + ";";
                                    #region Unused Methods
                                    //string s = content1;
                                    //string mySearch = "\" Pick=\"true\""; //"BinQuality=\"Pass\" BinCount=\""; //"</BinDefinitions>";//<GoodDevices>104751</GoodDevices>
                                    //bool val = s.Contains(mySearch);
                                    //if (val == true)
                                    //{
                                    //    int LenStart = s.IndexOf("BinCount=\""); //s.IndexOf(mySearch);//("<BinDefinitions>");
                                    //    int LenEnd = s.IndexOf(mySearch); //s.IndexOf("\" Pick=\"true\"");//("</BinDefinitions>");
                                    //    Qty1 = s.Substring(LenStart + 10, LenEnd - (LenStart + 10)); //s.Substring(LenStart + 28, LenEnd - (LenStart + 28));//s.Substring(LenStart + 13, LenEnd - (LenStart + 13));
                                    
                                    
                                    //    res += item + ":" + Qty1 + ";";
                                    //}
                                    //else
                                    //{
                                    //    res += item + ":" + "NoMap" + ";";
                                    //}
                                    #endregion Unused Methods
                                }
                            }
                        }
                        Qrad.Attributes.Add("quaterqty", res);
                    }
                }
            }
        }

        protected void rdbH_CheckedChanged(object sender, EventArgs e)
        {
            //foreach (GridViewRow row in Gv1.Rows)
            {
                //System.Web.UI.WebControls.RadioButton chk = (System.Web.UI.WebControls.RadioButton)row.FindControl("rdbH");
                System.Web.UI.WebControls.RadioButton rad = sender as System.Web.UI.WebControls.RadioButton;
                if (rad.Checked == true)
                {
                    //var MAP = (new System.Linq.SystemCore_EnumerableDebugView(chk.Attributes.Keys as System.Collections.IEnumerable)).Items[1];//Session["workmap"];//objmap[row.RowIndex];
                    var MAP = rad.Attributes["Hmapcode"].ToString();
                    string res = "";
                    for (int i = 1; i < 3; i++)
                    {
                        string item = "H" + i + "__" + MAP;
                        //foreach (var item in qtr)
                        //{
                        string urlFWM = @"http://myser01ms056/wafermap/" + item + ".142";//http://myser01ms056/ Changed web URL
                        string content1 = "";
                        if (IsAddressAvailable(urlFWM) == false) //202008141241-modified by JB
                        {
                            res += item + ":" + "NoMap" + ";";
                        }
                        else
                        {
                            using (WebResponse wr = WebRequest.Create(urlFWM).GetResponse())
                            {
                                using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                                {
                                    content1 = sr.ReadToEnd();

                                    List<string> objbin = new List<string>();
                                    objbin.Add("BinCode=\"01\"");
                                    objbin.Add("BinCode=\"02\"");
                                    objbin.Add("BinCode=\"03\"");
                                    objbin.Add("BinCode=\"04\"");
                                    objbin.Add("BinCode=\"05\"");
                                    objbin.Add("BinCode=\"06\"");
                                    objbin.Add("BinCode=\"07\"");
                                    objbin.Add("BinCode=\"0001\"");
                                    string s = content1;
                                    QtySum = 0;
                                    for (int i1 = 0; i1 < objbin.Count(); i1++) //2020Oct23 JB loop the BIN 1 to 7
                                    {
                                        bool val = s.Contains(objbin[i1]);
                                        if (val == true)
                                        {
                                            int lenfind = s.IndexOf(objbin[i1]);
                                            string sind = s.Substring(lenfind, 100);

                                            int LenStart = sind.IndexOf(objbin[i1]);
                                            int LenEnd = sind.IndexOf("\" Pick=\"true\"");
                                            bool valbin = sind.Contains(" Pick=\"true\"");
                                            if (valbin == true)
                                            {
                                                //20210930 JB if BinCode=0001
                                                if (objbin[i1].Length >= 14)
                                                {
                                                    QtySum = QtySum + int.Parse(sind.Substring(LenStart + 65, LenEnd - (LenStart + 65)));
                                                }
                                                else
                                                {
                                                    QtySum = QtySum + int.Parse(sind.Substring(LenStart + 63, LenEnd - (LenStart + 63))); //s.Substring(LenStart + 28, LenEnd - (LenStart + 28));//s.Substring(LenStart + 13, LenEnd - (LenStart + 13));
                                                }
                                            }
                                            else
                                            {
                                                if (QtySum==0)
                                                {
                                                    res += item + ":" + "NoMap" + ";";
                                                }
                                            }
                                        }
                                    }
                                    res += item + ":" + QtySum + ";";
                                    #region Unused Methods
                                    //string mySearch = "BinQuality=\"Pass\" BinCount=\"";//"</BinDefinitions>";//<GoodDevices>104751</GoodDevices>
                                    //bool val = s.Contains(mySearch);
                                    //if (val == true)
                                    //{
                                    //    int LenStart = s.IndexOf(mySearch);//("<BinDefinitions>");
                                    //    int LenEnd = s.IndexOf("\" Pick=\"true\"");//("</BinDefinitions>");
                                    //    Qty1 = s.Substring(LenStart + 28, LenEnd - (LenStart + 28));//s.Substring(LenStart + 13, LenEnd - (LenStart + 13));
                                    //    res += item + ":" + Qty1 + ";";
                                    //}
                                    //else
                                    //{
                                    //    res += item + ":" + "NoMap" + ";";
                                    //}
                                    #endregion Unused Methods
                                }
                            }
                        }
                        rad.Attributes.Add("halfqty", res);
                    }
                }
            }
            #region Unused Methods
            //foreach (GridViewRow row in Gv1.Rows)
            //{
            //    System.Web.UI.WebControls.RadioButton chk = (System.Web.UI.WebControls.RadioButton)row.FindControl("rdbH");
            //    if (chk.Checked == true)
            //    {
            //        var MAP = Session["workmap"];//objmap[row.RowIndex];
            //        for (int i = 1; i < 3; i++)
            //        {
            //            testhalf = "H" + i + "__" + MAP;
            //            half.Add(testhalf);
            //        }
            //    }
            //}
            #endregion Unused Methods
        }

        public bool IsAddressAvailable(string address)
        {
            try
            {
                System.Net.WebClient client = new System.Net.WebClient();
                client.DownloadData(address);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion Changes made by SRI for Grid & Radio Buttons

        #region Unused Methods

        protected void btnConvert_Click(object sender, EventArgs e)
        {
            Label1.Text = "Start Processing";
            createFWM();
        }

        private void createFWM()
        {
            using (OracleConnection mesDbConn = new OracleConnection(ConfigurationManager.ConnectionStrings["MES"].ConnectionString))
            {

                mesDbConn.Open();
                using (OracleCommand cmd = mesDbConn.CreateCommand())
                {
                    cmd.CommandText = string.Format(@"select so.containername AS ShopOrder, w.containername AS MESWaferBatch, pb.PRODUCTNAME AS Wafer12NC, p.description CrystalDesc, 
                            w.DIFFUSIONBATCHCODE AS DiffusionBatch
                            from container so, container w, mes_wafersusedbyso wso,product p, productbase pb
                            where wso.mes_shopordercontainerid(+)= so.containerid
                            and wso.mes_wafercontainerid = w.containerid(+)
                            and w.productid=p.productid
                            and p.productbaseid=pb.productbaseid
                            and p.productid=pb.REVOFRCDID
                            and w.status = '1'
                            and so.status = '1'
                            order by 1,2");
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            crystal.Add(reader["MESWAFERBATCH"].ToString());


                        }
                        //  WebRequest webRequest = WebRequest.Create("http://ussbazesspre004:9002/DREADD?");
                        //  WebResponse webResp = webRequest.GetResponse();
                    }
                }

                mesDbConn.Close();
            }
            using (SqlConnection repDbConn = new SqlConnection(ConfigurationManager.ConnectionStrings["RECIPE"].ConnectionString))
            {
                repDbConn.Open();
                using (SqlCommand cmd = repDbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM [WAFERRECIPE] TP ;";
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            crystaldescdB.Add(reader["WAFERNAME"].ToString());
                            RecipedB.Add(reader["RECIPE"].ToString());
                            RecipedBExtra.Add(reader["EXTRA"].ToString());
                        }
                    }
                }
                repDbConn.Close();
            }

            int index1, index2;
            string swm, ocrid, ocrid0, Mycrystal;
            foreach (string r in crystaldesc.Intersect(crystaldescdB))
            {
                index1 = crystaldesc.IndexOf(r);
                index2 = crystaldescdB.IndexOf(r);
                Mycrystal = r;
                ocrid = crystal.ElementAt(index1);
                ocrid0 = ocrid;
                swm = RecipedB.ElementAt(index2);
                try
                {
                    string[] ocr = ocrid0.Split('-');
                    if (ocrid0.Split('-').Length >= 3)
                    { //ocrid = ocr[0] + "-" + ocr[1] + "-" + ocr[2]; 
                        ocrid = ocr[1].Substring(0, 5) + "W" + ocr[2].Substring(1, 2) + "-";


                    }
                }
                catch
                { }

                string xx = GenerateCheckChar(ocrid);
                ocrid = xx;
                string url = "http://" + "mygsermys1ms026" + "/template/createmapfromswm.html?swm=" + swm + ".dm1&ocrid=" + SawPrefix + ocrid;
                //WebRequest webRequest = WebRequest.Create(url);
                //WebResponse webResp = webRequest.GetResponse();
                string content = "";
                string MyDate = "";
                string path = "";
                string MyResult = "";

                using (WebResponse wr = WebRequest.Create(url).GetResponse())
                {
                    using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                    {
                        content = sr.ReadToEnd();
                        MyDate = DateTime.Now.ToString("dd-MMM-yyyy_HH:mm:ss");
                        path = @"c:\temp\SWMtoFWM.txt";
                        MyResult = MyDate + "," + r + "," + ocrid0 + "," + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                        //File.AppendAllLines(path, new[] { MyResult });
                    }
                }

                if (content.Contains("swm is not valid") == true)
                {
                    url = "http://" + "mygsermys1ms026" + "/template/createmapfromswm.html?swm=" + swm + ".dms&ocrid=" + ocrid;

                    content = "";
                    using (WebResponse wr = WebRequest.Create(url).GetResponse())
                    {
                        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                            MyDate = DateTime.Now.ToString("dd-MMM-yyyy_HH:mm:ss");
                            path = @"c:\temp\SWMtoFWM.txt";
                            MyResult = MyDate + "," + r + "," + ocrid0 + "," + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                            //File.AppendAllLines(path, new[] { MyResult });
                        }
                    }
                }
                if (content.Contains("swm is not valid") == true)
                {
                    if (content.Contains("swm is not valid"))
                    {
                        url = "http://" + "mygsermys1ms026" + "/template/createmapfromswm.html?swm=" + swm + ".wm&ocrid=" + ocrid;

                        content = "";
                        using (WebResponse wr = WebRequest.Create(url).GetResponse())
                        {
                            using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                            {
                                content = sr.ReadToEnd();
                                MyDate = DateTime.Now.ToString("dd-MMM-yyyy_HH:mm:ss");
                                path = @"c:\temp\SWMtoFWM.txt";
                                MyResult = MyDate + "," + r + "," + ocrid0 + "," + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                                // File.AppendAllLines(path, new[] { MyResult });
                            }
                        }
                    }

                }
                File.AppendAllLines(path, new[] { MyResult });


                // string aa = ((HttpWebResponse)webResp).StatusDescription;
                Label1.Text = "process:" + ocrid + " " + swm;

            }
            Label1.Text = "Conversion Complete";

        }

        protected void btnTest_Click(object sender, EventArgs e)
        {

            //string SawDirection = "";
            //string SawQtySplit = "";
            //string SawMapFile = "";
            utility ut = new utility();

            var myData = ut.getWaferInfoFromCamstar(txtSOID.Text.ToString());

            //   var CamstarList = new List<utility.CamstarInfo>();


            IList<string> myList = new List<string>();

            //WaferInfo[i] = vFab[j].ToString();
            //WaferInfo[i + 1] = vDevice[j].ToString();
            //WaferInfo[i + 2] = vBIMLine[j].ToString();
            //WaferInfo[i + 3] = vCRYSTALDESC[j].ToString();


            Table tb = new Table();
            tb.BorderWidth = 0;
            tb.BorderStyle = System.Web.UI.WebControls.BorderStyle.Solid;
            tb.ID = "myTable";


            TableRow ttr = new TableRow();

            TableCell ttc1 = new TableCell();  //fab
            TableCell ttc2 = new TableCell();  //batch
            TableCell ttc3 = new TableCell();  //waferdesc
            TableCell ttc4 = new TableCell();  //qty
            TableCell ttc5 = new TableCell();  //quarter
            TableCell ttc6 = new TableCell();  //half
            TableCell ttc7 = new TableCell();  //print

            ttc1.Text = "Fab";
            ttc1.Width = 100;
            ttc1.BorderWidth = 1;
            ttr.Cells.Add(ttc1);

            ttc2.Text = "WaferBatch";
            ttc2.Width = 300;
            ttc2.BorderWidth = 1;
            ttr.Cells.Add(ttc2);

            ttc3.Text = "WaferDescription";
            ttc3.Width = 300;
            ttc3.BorderWidth = 1;
            ttr.Cells.Add(ttc3);

            ttc4.Text = "Quantity";
            ttc4.Width = 100;
            ttc4.BorderWidth = 1;
            ttr.Cells.Add(ttc4);

            ttc5.Text = "Quarter";
            ttc5.Width = 100;
            ttc5.BorderWidth = 1;
            ttr.Cells.Add(ttc5);

            ttc6.Text = "Half";
            ttc6.Width = 100;
            ttc6.BorderWidth = 1;
            ttr.Cells.Add(ttc6);

            ttc7.Text = "Print";
            ttc7.Width = 100;
            ttc7.BorderWidth = 1;
            ttr.Cells.Add(ttc7);

            tb.Rows.Add(ttr);

            PlaceHolder1.Controls.Add(tb);



            // var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(myBarcode);

            foreach (var data in myData)
            {
                string b1 = "Q__" + data.CrystalBatch.ToString();
                string b2 = "H__" + data.CrystalBatch.ToString();
                string b3 = "P__" + data.CrystalBatch.ToString();

                HtmlInputCheckBox NewControl = new HtmlInputCheckBox();
                NewControl.ID = b1;  //"MyButton";
                NewControl.Checked = false;
                NewControl.Attributes.Add("value", b1);
                NewControl.Attributes.Add("onclick", "");

                HtmlInputCheckBox NewControl1 = new HtmlInputCheckBox();
                NewControl1.ID = b2;  //"MyButton1";
                NewControl1.Checked = false;
                NewControl1.Attributes.Add("value", b2);
                NewControl1.Attributes.Add("onclick", "");

                HtmlGenericControl NewControl2 = new HtmlGenericControl("button");
                NewControl2.ID = b3;  //"MyButton2";
                NewControl2.InnerText = "Print";
                NewControl2.Attributes.Add("value", b3);
                NewControl2.Attributes.Add("onclick", "");


                TableRow tr = new TableRow();

                TableCell tc1 = new TableCell();
                TableCell tc2 = new TableCell();
                TableCell tc3 = new TableCell();  //waferdesc
                TableCell tc4 = new TableCell();
                TableCell tc5 = new TableCell();
                TableCell tc6 = new TableCell();
                TableCell tc7 = new TableCell();

                tc1.Text = data.Fab.ToString();
                tc1.Width = 100;
                tc1.BorderWidth = 1;
                tr.Cells.Add(tc1);

                tc2.Text = data.CrystalBatch.ToString();
                tc2.Width = 300;
                tc2.BorderWidth = 1;
                tr.Cells.Add(tc2);

                tc3.Text = data.CrystalDesc.ToString();
                tc3.Width = 300;
                tc3.BorderWidth = 1;
                tr.Cells.Add(tc3);

                tc4.Text = data.CrystalQty.ToString();
                tc4.Width = 100;
                tc4.Controls.Add(NewControl);
                tc4.BorderWidth = 1;
                tr.Cells.Add(tc4);

                tc5.Text = "";
                tc5.Width = 100;
                tc5.Controls.Add(NewControl);
                tc5.BorderWidth = 1;
                tr.Cells.Add(tc5);

                tc6.Text = "";
                tc6.Width = 100;
                tc6.Controls.Add(NewControl1);
                tc6.BorderWidth = 1;
                tr.Cells.Add(tc6);

                tc7.Text = "";
                tc7.Width = 100;
                tc7.Controls.Add(NewControl2);
                tc7.BorderWidth = 1;
                tr.Cells.Add(tc7);

                tb.Rows.Add(tr);
                PlaceHolder1.Controls.Add(tb);
            }



        }

        protected void writeSettingNew(string fpath, string fvalue)
        {
            var filePath1 = Server.MapPath("~/Config/MyPrinterSetting.txt");

            StreamWriter stm = null;
            var fi = new FileInfo(filePath1);
            if (fi.Exists)
            {
                //   var stm1 = fi.OpenWrite();
            }

            const Int32 BufferSize = 1024;
            using (var fileStream = File.OpenRead(filePath1))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string line;

                while ((line = streamReader.ReadLine()) != null)
                {
                    try
                    {
                        PrinterSetting.Add(line);
                    }
                    catch
                    { }
                }
                for (int a = 0; a < PrinterSetting.Count - 1; a++)
                {
                    string[] DataInput = PrinterSetting[a].Split(',');
                    if (fpath == "PrinterPath")
                    {

                        if (Session["clientHostname"].ToString() == DataInput[0].ToString())
                        {
                            PrinterSetting[a] = DataInput[0] + "," + DataInput[1] + "," + fvalue;
                        }
                    }
                    if (fpath == "LabelFilePath" && (DataInput[1].ToString() == "DefaultLabelFilePath"))
                    {
                        // mysermys1ms001,DefaultLabelFilePath,\\165.114.94.72\data
                        DataInput = fvalue.Split('\\');
                        PrinterSetting[a] = DataInput[2] + "," + "DefaultLabelFilePath" + "," + fvalue;
                    }

                }
            }

            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filePath1))
            {
                for (int a = 0; a < PrinterSetting.Count; a++)
                {
                    file.WriteLine(PrinterSetting[a]);
                }
            }
        }

        protected void writeSetting(string fpath, string fvalue)
        {
            var filePath1 = Server.MapPath("~/Config/MyPrinterSetting.txt");


            StreamWriter stm = null;
            var fi = new FileInfo(filePath1);
            if (fi.Exists)
            {
                //   var stm1 = fi.OpenWrite();
            }

            //    FileStream stm = fi.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);


            const Int32 BufferSize = 1024;

            using (var fileStream = fi.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string line;

                while ((line = streamReader.ReadLine()) != null)
                {
                    try
                    {

                        string[] DataIn = line.Split(',');
                        string sArea = DataIn[0].ToString();
                        string sPath = DataIn[1].ToString();
                        string sValue = DataIn[2].ToString();

                        if (fpath == "LabelFilePath")
                        { //txtDataPath.Text = sValue; 

                            using (var streamWriter = new StreamWriter(fileStream))
                            {
                                streamWriter.WriteLine(sArea + "," + fpath + "," + fvalue);
                                //  txtDataPath.Text = fvalue;
                                //  lblStatus.Text = "Save Source for label data to " + fvalue;
                            }
                        }

                        if (fpath == "PrinterPath")
                        { //txtPrinterPath.Text = sValue;

                            using (var streamWriter = new StreamWriter(fileStream))
                            {
                                streamWriter.WriteLine(sArea + "," + fpath + "," + fvalue);
                                // txtPrinterPath.Text = fvalue;
                                //  lblStatus.Text = "Save Printer to " + fvalue;
                            }
                        }


                        //    if (sPath == "LabelFilePath") { txtDataPath.Text = sValue; }

                    }
                    catch
                    {
                        streamReader.Dispose();
                        return;
                    }


                }
                streamReader.Dispose();

            }
        }

        protected void readSetting()
        {
            var filePath1 = Server.MapPath("~/Config/MyPrinterSetting.txt");
            const Int32 BufferSize = 1024;
            using (var fileStream = File.OpenRead(filePath1))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string line;

                while ((line = streamReader.ReadLine()) != null)
                {
                    try
                    {
                        PrinterSetting.Add(line);
                        string[] DataIn = line.Split(',');
                        string sArea = DataIn[0].ToString();
                        string sPath = DataIn[1].ToString();
                        string sValue = DataIn[2].ToString();
                        //   lblIPaddress.Text = "Access from: " + Session["Ipaddress"] + " " + Session["clientHostname"];
                        //  if (sPath == "PrinterPath") { txtPrinterPath.Text = sValue; }
                        //  if (sPath == "LabelFilePath") { txtDataPath.Text = sValue; }
                    }
                    catch
                    { }
                }
                string[] DataInput = PrinterSetting[0].Split(',');
                string PrintAccessUsername = DataInput[1];
                string PrintAccessPassword = DataInput[2];

                DataInput = PrinterSetting[1].Split(',');
                string DefaultPrinterPath = DataInput[2];

                DataInput = PrinterSetting[2].Split(',');
                string DefaultLabelFilePath = DataInput[2];

                // txtPrinterPath.Text = "";
                for (int a = 0; a < PrinterSetting.Count - 1; a++)
                {

                    DataInput = PrinterSetting[a].Split(',');
                    if (Session["clientHostname"].ToString() == DataInput[0].ToString())
                    {
                        //    txtPrinterPath.Text = DataInput[2];
                        //    txtDataPath.Text = DefaultLabelFilePath;
                    }
                }
            }
        }

        public void readSI(string txtSOID, ref string SawDirection, ref string SawQtySplit, ref string SawMapFile)
        {
            TwinSI.Clear();

            using (OracleConnection mesDbConn = new OracleConnection(ConfigurationManager.ConnectionStrings["MES"].ConnectionString))
            {

                mesDbConn.Open();
                using (OracleCommand cmd = mesDbConn.CreateCommand())
                {
                    cmd.CommandText = string.Format(@"  select distinct so.containername AS ShopOrder, 
 fpb.productname FGRequest12NC, 
 fp.DESCRIPTION FGRequestDesc,
        w.containername AS MESWaferBatch, 
      wso.qtyreserved QtyIn,
       pb.PRODUCTNAME AS Wafer12NC, 
        p.DESCRIPTION CrystalDesc,
        o.mes_waferoriginname Fab,
       so.numberofwafer,
NVL(substr(cp.parametervalue,1,17),'-') PlanBIMLine,
  NVL (RD.RESOURCENAME,'-') BIMLine, 
        w.DIFFUSIONBATCHCODE AS DiffusionBatch,
       si.MES_SPECIALINSTRUCTIONNAME SI       
from container so, 
     container w, 
     mes_wafersusedbyso wso,
     product p, 
     productbase pb, 
     mes_waferorigin o,
     mes_containerparameter cp,
     RESOURCEDEF RD, 
     PRODUCTTYPE pt,
     containermes_fginso cf, 
     mes_fginso mf, 
     product fp, 
     productbase fpb,
     vw_mes_containersi si
where wso.mes_shopordercontainerid(+)= so.containerid
and so.containerid <> so.containername 
and so.containerid = cp.containerid
and cp.parametername = 'PRODLN'
and wso.mes_wafercontainerid = w.containerid(+)
and w.productid=p.productid
and p.productbaseid=pb.productbaseid
and p.productid=pb.REVOFRCDID
and o.mes_waferoriginid = p.mes_waferoriginid
and pt.PRODUCTTYPEID = p.PRODUCTTYPEID
and RD.RESOURCEID(+) = SO.MES_EQUIPMENTID
and cf.mes_fginsoid = mf.mes_fginsoid  -- new
and fpb.productbaseid = fp.productbaseid
and fp.productid = mf.MES_FINISHEDGOODID
and cf.instanceid = so.containerid
and si.containername = so.containername
and si.MES_SPECIALINSTRUCTIONNAME like '%TWIN%'
AND so.containername = :pSOid  
order by 1,4
");
                    OracleParameter pWO = new OracleParameter(":pSOid", txtSOID) { OracleType = OracleType.Char };
                    cmd.Parameters.Add(pWO);
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                string myTwinSI = reader["SI"].ToString().ToUpper();
                                TwinSI.Add(myTwinSI);
                            }
                            catch
                            { }
                        }
                    }
                }
                mesDbConn.Close();

            }

            if (TwinSI.Count > 0)
            {
                var MyTwinSI = TwinSI.Distinct();
                if (MyTwinSI.Contains("1 SAW SINGLE DIE") == true && MyTwinSI.Contains("1 SAW TWIN DIE") == true)
                {
                    SawQtySplit = "HALF";
                    if (MyTwinSI.Contains("TWIN DIE: MUST INK & USE SWM") == true)
                    { SawMapFile = "SWM"; }
                    if (MyTwinSI.Contains("1 SAW TWIN DIE X-DIRECTION") == true && MyTwinSI.Contains("1 SAW TWIN DIE Y-DIRECTION") == true)
                    { SawDirection = "XY"; }
                    else if (MyTwinSI.Contains("1 SAW TWIN DIE X-DIRECTION") == true)
                    { SawDirection = "X"; }
                    else if (MyTwinSI.Contains("1 SAW TWIN DIE Y-DIRECTION") == true)
                    { SawDirection = "Y"; }

                }
                else if (MyTwinSI.Contains("1 SAW TWIN DIE X-DIRECTION") == true || MyTwinSI.Contains("1 SAW TWIN DIE Y-DIRECTION") == true)
                {
                    SawQtySplit = "FULL";
                    if (MyTwinSI.Contains("TWIN DIE: MUST INK & USE SWM") == true)
                    { SawMapFile = "SWM"; }
                    if (MyTwinSI.Contains("1 SAW TWIN DIE X-DIRECTION") == true && MyTwinSI.Contains("1 SAW TWIN DIE Y-DIRECTION") == true)
                    { SawDirection = "XY"; }
                    else if (MyTwinSI.Contains("1 SAW TWIN DIE X-DIRECTION") == true)
                    { SawDirection = "X"; }
                    else if (MyTwinSI.Contains("1 SAW TWIN DIE Y-DIRECTION") == true)
                    { SawDirection = "Y"; }
                }
            }



        }

        protected string GetExtraInfo(string WOID)
        {
            string MyExtra = "";
            using (SqlConnection repDbConn = new SqlConnection(ConfigurationManager.ConnectionStrings["RECIPE"].ConnectionString))
            {
                repDbConn.Open();

                using (SqlCommand cmd = repDbConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM [WAFERRECIPE] TP WHERE WAFERNAME = '" + WOID + "' ;";
                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            MyExtra = reader["EXTRA"].ToString();
                        }
                    }
                }
                repDbConn.Close();
            }
            return MyExtra;
        }

        protected void btnTest1_Click(object sender, EventArgs e)
        {

        }

        protected void btnDHAMDashYes_Click(object sender, EventArgs e)
        {

        }

        protected void cbBimline_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        protected void txtScan_Click(object sender, EventArgs e)
        {

        }

        protected void lstOCRID_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        protected void lstOCRIDError_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public static void DeleteData()
        {
            //your code
        }

        #endregion Unused Methods

        protected void btnprint_Click1(object sender, EventArgs e)
        {//button to print single
            System.Web.UI.WebControls.Button obj = sender as System.Web.UI.WebControls.Button;

            DataTable dt = new DataTable();
            //dt.Columns.Add("RN",typeof(int));
            dt.Columns.Add("MyDate", typeof(DateTime));
            dt.Columns.Add("MWO", typeof(string));
            dt.Columns.Add("DEVICE", typeof(string));
            dt.Columns.Add("WAFERTYPE", typeof(string));
            dt.Columns.Add("OCRID", typeof(string));
            dt.Columns.Add("OCRID1", typeof(string));
            dt.Columns.Add("RESULT", typeof(string));
            dt.Columns.Add("REMARKS", typeof(string));
            dt.Columns.Add("WAFERID", typeof(string));
            dt.Columns.Add("AWACSQuantity", typeof(int));

            foreach (GridViewRow row in Gv1.Rows)
            {
                if ((System.Web.UI.WebControls.Button)row.FindControl("btnprint") == obj)
                {
                    if (((System.Web.UI.WebControls.RadioButton)row.FindControl("rdbQ")).Checked == true)
                    {
                        dt = dtConstruction(obj, row, "rdbQ", "quaterqty", dt);
                    }
                    else if (((System.Web.UI.WebControls.RadioButton)row.FindControl("rdbH")).Checked == true)
                    {
                        dt = dtConstruction(obj, row, "rdbH", "halfqty", dt);
                    }
                    else
                    {
                        DataRow dr = dt.NewRow();
                        //dr["RN"] = DateTime.Now;
                        dr["MyDate"] = DateTime.Now;
                        dr["MWO"] = obj.Attributes["vMWO"]; //txtSOID.Text.Trim(); 20220902-JB-modified
                        dr["DEVICE"] = obj.Attributes["vDevice"];
                        dr["WAFERTYPE"] = obj.Attributes["vCRYSTALDESC"];
                        dr["OCRID"] = obj.Attributes["vMap"];
                        dr["OCRID1"] = obj.Attributes["vMap"];
                        dr["RESULT"] = "FWM";
                        dr["REMARKS"] = "FWM Added";
                        dr["WAFERID"] = obj.Attributes["vCONTAINERNAME"];
                        //dr["AWACSQuantity"] = obj.Attributes["vQTY"];
                        int outres = 0;
                        dr["AWACSQuantity"] = (Int32.TryParse(obj.Attributes["vQTY"].Trim(), out outres)) ? Convert.ToInt32(obj.Attributes["vQTY"].Trim()) : 0;

                        if (dr["OCRID"].ToString() != "NoMap")
                        {
                            dt.Rows.Add(dr);
                            //to debug
                            //try
                            //{
                            //}
                            //catch(Exception er)
                            //{
                            //    lbErr.Text = er.ToString();
                            //}
                        }
                    }

                    break;
                }
            }
            //to debug
            //DataTable dt2 = new DataTable();
            ////dt.Columns.Add("RN",typeof(int));
            //dt2.Columns.Add("MyDate", typeof(DateTime));
            //dt2.Columns.Add("MWO", typeof(string));
            //dt2.Columns.Add("DEVICE", typeof(string));
            //dt2.Columns.Add("WAFERTYPE", typeof(string));
            //dt2.Columns.Add("OCRID", typeof(string));
            //dt2.Columns.Add("OCRID1", typeof(string));
            //dt2.Columns.Add("RESULT", typeof(string));
            //dt2.Columns.Add("REMARKS", typeof(string));
            //dt2.Columns.Add("WAFERID", typeof(string));
            //dt2.Columns.Add("AWACSQuantity", typeof(int));
            //dt2.Rows.Add(DateTime.Now,txtSOID.Text.Trim(),obj.Attributes["vDevice"],obj.Attributes["vCRYSTALDESC"],obj.Attributes["vMap"],obj.Attributes["vMap"],"FWM","FWM Added",obj.Attributes["vCONTAINERNAME"],Int32.Parse(obj.Attributes["vQTY"].Trim()));
            //Gv2.DataSource = dt;
            //Gv2.DataBind();

            if (dt.Rows.Count > 0)
            {
                //to debug
                //try
                //{
                    //using (SqlConnection con = new SqlConnection("Data Source=MYSER01NB0603;Initial Catalog=awacs;Integrated Security=True"))
                    using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["RECIPE"].ConnectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand("[dbo].[SPSawLabels]", con))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@SawLabelType", dt);//.SqlDbType = SqlDbType.Structured;
                            con.Open();
                            int res = cmd.ExecuteNonQuery();
                            con.Close();
                            //to debug
                            //SqlDataReader reader = cmd.ExecuteReader();
                            //if (reader.HasRows == true)
                            //{
                            //    DateTime s = DateTime.Parse(reader["MyDate"].ToString().Trim());
                            //    string sm = reader["MWO"].ToString().Trim();
                            //    string sd = reader["DEVICE"].ToString().Trim();
                            //    string sw = reader["WAFERTYPE"].ToString().Trim();
                            //    string so = reader["OCRID"].ToString().Trim();
                            //    string so1 = reader["OCRID1"].ToString().Trim();
                            //    string sr = reader["RESULT"].ToString().Trim();
                            //    string srk = reader["REMARKS"].ToString().Trim();
                            //    string swid = reader["WAFERID"].ToString().Trim();
                            //}

                        }
                    }
                //to debug
                //}
                //catch (Exception ex)
                //{
                //    lbErr.Text = ex.ToString();
                //}
            }

            //using (SqlConnection con = new SqlConnection("Data Source=MYSER01NB0603;Initial Catalog=awacs;Integrated Security=True"))
            //{
            //    using (SqlCommand cmd = new SqlCommand("[dbo].[SPSawLabels]", con))
            //    {
            //        cmd.CommandType = CommandType.StoredProcedure;
            //        cmd.Parameters.AddWithValue("@MyDate", DateTime.Now);
            //        cmd.Parameters.AddWithValue("@MWO", txtSOID.Text.Trim());
            //        cmd.Parameters.AddWithValue("@Device", obj.Attributes["vDevice"]);
            //        cmd.Parameters.AddWithValue("@WaferType", obj.Attributes["vCRYSTALDESC"]);
            //        cmd.Parameters.AddWithValue("@OCRID", obj.Attributes["vMap"]);
            //        cmd.Parameters.AddWithValue("@Result", "FWM");
            //        cmd.Parameters.AddWithValue("@Remarks", "FWM Added");
            //        cmd.Parameters.AddWithValue("@WaferId", obj.Attributes["vCONTAINERNAME"]);
            //        con.Open();
            //        cmd.ExecuteNonQuery();
            //    }
            //}

            //obj.Attributes.Add("onclick", obj.Attributes["printStruk"]);
            //obj.OnClientClick = obj.Attributes["printStruk"];

            ClientScript.RegisterStartupScript(this.GetType(), "printStruk", obj.Attributes["printStruk"], true);
            //obj.Attributes.Keys.
        }

        protected void btnPrintAll_Click(object sender, EventArgs e)
        {//button to print all
            System.Web.UI.WebControls.Button obj1 = sender as System.Web.UI.WebControls.Button;

            DataTable dt = new DataTable();
            //dt.Columns.Add("RN",typeof(int));
            dt.Columns.Add("MyDate", typeof(DateTime));
            dt.Columns.Add("MWO", typeof(string));
            dt.Columns.Add("DEVICE", typeof(string));
            dt.Columns.Add("WAFERTYPE", typeof(string));
            dt.Columns.Add("OCRID", typeof(string));
            dt.Columns.Add("OCRID1", typeof(string));
            dt.Columns.Add("RESULT", typeof(string));
            dt.Columns.Add("REMARKS", typeof(string));
            dt.Columns.Add("WAFERID", typeof(string));
            dt.Columns.Add("AWACSQuantity", typeof(int));

            foreach (GridViewRow row in Gv1.Rows)
            {
                if ((System.Web.UI.WebControls.Button)row.FindControl("btnprint") != null)
                {
                    System.Web.UI.WebControls.Button obj = (System.Web.UI.WebControls.Button)row.FindControl("btnprint");

                    if (((System.Web.UI.WebControls.RadioButton)row.FindControl("rdbQ")).Checked == true)
                    {
                        dt = dtConstruction(obj, row, "rdbQ", "quaterqty", dt);
                    }
                    else if (((System.Web.UI.WebControls.RadioButton)row.FindControl("rdbH")).Checked == true)
                    {
                        dt = dtConstruction(obj, row, "rdbH", "halfqty", dt);
                    }
                    else
                    {
                        DataRow dr = dt.NewRow();
                        //dr["RN"] = DateTime.Now;
                        dr["MyDate"] = DateTime.Now;
                        dr["MWO"] = obj.Attributes["vMWO"]; //txtSOID.Text.Trim(); 20220902-JB-Modified
                        dr["DEVICE"] = obj.Attributes["vDevice"];
                        dr["WAFERTYPE"] = obj.Attributes["vCRYSTALDESC"];
                        dr["OCRID"] = obj.Attributes["vMap"];
                        dr["OCRID1"] = obj.Attributes["vMap"];
                        dr["RESULT"] = "FWM";
                        dr["REMARKS"] = "FWM Added";
                        dr["WAFERID"] = obj.Attributes["vCONTAINERNAME"];
                        //dr["AWACSQuantity"] = obj.Attributes["vQTY"];
                        int outres = 0;
                        dr["AWACSQuantity"] = (Int32.TryParse(obj.Attributes["vQTY"].Trim(), out outres)) ? Convert.ToInt32(obj.Attributes["vQTY"].Trim()) : 0;

                        if (dr["OCRID"].ToString() != "NoMap")
                        {
                            dt.Rows.Add(dr);
                        }
                    }

                    //break;
                }
            }

            if (dt.Rows.Count > 0)
            {
                //using (SqlConnection con = new SqlConnection("Data Source=MYSER01NB0603;Initial Catalog=awacs;Integrated Security=True"))
                using (SqlConnection con = new SqlConnection(ConfigurationManager.ConnectionStrings["RECIPE"].ConnectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("[dbo].[SPSawLabels]", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@SawLabelType", dt);//.SqlDbType = SqlDbType.Structured;
                        con.Open();
                        int res = cmd.ExecuteNonQuery();
                        con.Close();
                    }
                }
            }

            ClientScript.RegisterStartupScript(this.GetType(), "printStruk", obj1.Attributes["printStruk"], true);
        }

        private void SendToPrinter(string zplCode)
        {
            string printerIpAddress = ZebraIPAddress; //"172.16.240.00"; // Replace with your printer's IP address
            int printerPort = int.Parse(ZebraDefaultPort); //9100; // Standard port for network printers

            try
            {
                using (TcpClient client = new TcpClient(printerIpAddress, printerPort))
                using (NetworkStream stream = client.GetStream())
                {
                    byte[] zplBytes = Encoding.UTF8.GetBytes(zplCode);
                    stream.Write(zplBytes, 0, zplBytes.Length);
                }
                lbErr.Text = "Label printing completed!";
            }
            catch (Exception ex)
            {
                // Handle exceptions (e.g., log the error, show a message to the user)
                //Console.WriteLine("Error sending to printer: " + ex.Message);
                lbErr.Text = "Error sending to printer: " + ex.Message;
            }
        }

        public DataTable dtConstruction(System.Web.UI.WebControls.Button obj, GridViewRow row, string rdbstr, string qty, DataTable dt)
        {//to construct quater or Half


            System.Web.UI.WebControls.RadioButton rdb = (System.Web.UI.WebControls.RadioButton)row.FindControl(rdbstr);
            string strqty = rdb.Attributes[qty];
            char[] chsep = { ';' };
            char[] chsep1 = { ':' };
            foreach (var item in strqty.Split(chsep, StringSplitOptions.RemoveEmptyEntries))
            {
                //if (item != "")
                {
                    DataRow drh = dt.NewRow();
                    //dr["RN"] = DateTime.Now;
                    drh["MyDate"] = DateTime.Now;
                    drh["MWO"] = obj.Attributes["vMWO"]; //txtSOID.Text.Trim(); 20220902-JB-Modified
                    drh["DEVICE"] = obj.Attributes["vDevice"];
                    drh["WAFERTYPE"] = obj.Attributes["vCRYSTALDESC"];
                    drh["OCRID"] = obj.Attributes["vMap"];
                    drh["OCRID1"] = item.Split(chsep1)[0];
                    drh["RESULT"] = "FWM";
                    drh["REMARKS"] = "FWM Added";
                    drh["WAFERID"] = obj.Attributes["vCONTAINERNAME"];
                    int outres = 0;
                    drh["AWACSQuantity"] = (Int32.TryParse(item.Split(chsep1)[1].Trim(), out outres)) ? Convert.ToInt32(item.Split(chsep1)[1]) : 0;

                    if (drh["OCRID"].ToString() != "NoMap")
                    {
                        dt.Rows.Add(drh);
                    }
                }
            }
            return dt;
        }
    }
}