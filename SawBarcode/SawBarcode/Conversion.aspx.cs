using System;
using System.Collections.Generic;
using System.Linq;
using System.Configuration;
using System.Web.Services;
using System.Web.Services.Protocols;
using System.Xml;
using System.Xml.Serialization;
using System.Data.OracleClient;
using System.Data.SqlClient;
using System.Data;
using System.Text.RegularExpressions;
using System.Net;
using System.IO;
using Microsoft.VisualBasic;





using System.ComponentModel;

using System.Threading;
using System.Web;
using System.Web.Security;
using System.Web.SessionState;
using System.Text;
using Tools;




namespace ReadCrystal
{

       public partial class Conversion : System.Web.UI.Page

    {
        public string myTotalBarcode = "";

   
        //for wafermap
        List<string> crystaldescdB = new List<string>();
        List<string> crystaldesc = new List<string>();
        List<string> crystal = new List<string>();     
        List<string> RecipedB = new List<string>();
        List<string> MyBarcode = new List<string>();

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
        List<string> lDEVICE = new List<string>();
        string MyPrefix = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                //TextBox1.Text = "localhost:8081";
               // createFWM();            
            }
            MySaw.saw sawPrefix = new MySaw.saw();
            MyPrefix = sawPrefix.Name;

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
                {   string [] ocr = ocrid0.Split('-');
                    if (ocrid0.Split('-').Length >= 3)
                    { //ocrid = ocr[0] + "-" + ocr[1] + "-" + ocr[2]; 
                        ocrid = ocr[1].Substring(0, 5) + "W"+ ocr[2].Substring(1,2) + "-";
                    
                    
                    }
                }
                catch
                { }

                string xx  = GenerateCheckChar(ocrid);
                ocrid =  xx;
                string url = "http://" + TextBox1.Text.Trim() + "/template/createmapfromswm.html?swm=" + swm + ".dm1&ocrid=" + MyPrefix + ocrid;
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
                         MyResult = MyDate + "," + r + "," + ocrid0 + "," + ocrid + "," + swm + "," + content.Substring(0,content.Length-1);
                        //File.AppendAllLines(path, new[] { MyResult });
                    }
                }

                if (content.Contains("swm is not valid")==true)
                {
                    url = "http://" + TextBox1.Text.Trim() + "/template/createmapfromswm.html?swm=" + swm + ".dms&ocrid=" + ocrid;

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
                if (content.Contains("swm is not valid")==true)
                {
                    if (content.Contains("swm is not valid"))
                    {
                        url = "http://" + TextBox1.Text.Trim() + "/template/createmapfromswm.html?swm=" + swm + ".wm&ocrid=" + ocrid;

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
                UpdatePanel1.Update();
            }
            Label1.Text = "Conversion Complete";
       
        }
        public string createFWMSaw(string CRYSTALDESC, string ocrid)
        {
           // crystal
            string swm = "";
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
                        }
                    }
                }
                repDbConn.Close();
            }



            for (int i = 0; i < crystaldescdB.Count; i++ )
            {
                if (crystaldescdB[i].ToString() == CRYSTALDESC)
                {

                    swm = RecipedB[i].ToString();                                                         
                }
                
            }
            if (swm == "") {


                return "noSWM";
            }



               // string xx = GenerateCheckChar(ocrid);
            //  ocrid = MyPrefix + xx;
            string url = "http://" + "localhost:8081" + "/template/createmapfromswm.html?swm=" + swm + ".dm1&ocrid=" + MyPrefix + ocrid;
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
                        MyResult = MyDate + ","  + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                        //File.AppendAllLines(path, new[] { MyResult });
                    }
                }

                if (content.Contains("swm is not valid") == true)
                {
                    url = "http://" + "localhost:8081" + "/template/createmapfromswm.html?swm=" + swm + ".dms&ocrid="  +ocrid;

                    content = "";
                    using (WebResponse wr = WebRequest.Create(url).GetResponse())
                    {
                        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                            MyDate = DateTime.Now.ToString("dd-MMM-yyyy_HH:mm:ss");
                            path = @"c:\temp\SWMtoFWM.txt";
                            MyResult = MyDate  + "," + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                            //File.AppendAllLines(path, new[] { MyResult });
                        }
                    }
                }
                if (content.Contains("swm is not valid") == true)
                {
                    if (content.Contains("swm is not valid"))
                    {
                        url = "http://" + TextBox1.Text.Trim() + "/template/createmapfromswm.html?swm=" + swm + ".wm&ocrid=" + ocrid;

                        content = "";
                        using (WebResponse wr = WebRequest.Create(url).GetResponse())
                        {
                            using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                            {
                                content = sr.ReadToEnd();
                                MyDate = DateTime.Now.ToString("dd-MMM-yyyy_HH:mm:ss");
                                path = @"c:\temp\SWMtoFWM.txt";
                                MyResult = MyDate  + "," + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                                // File.AppendAllLines(path, new[] { MyResult });
                            }
                        }
                    }

                }
                File.AppendAllLines(path, new[] { MyResult });
                // string aa = ((HttpWebResponse)webResp).StatusDescription;



                return MyResult;


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

        

        public string GenerateCheckChar(string InputStr ) 
        {
         int Checksum = 0;
         int Char1 ;
         int Char2 ;
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


        protected void btnConvert_Click(object sender, EventArgs e)
        {
            Label1.Text = "Start Processing";
            createFWM();
        }

        protected void TextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        protected void txtScan_Click(object sender, EventArgs e)
        {

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
        protected string [] readcode(string  [] mycode, string [] mycodeDesc)
        {
            var filePath1 = Server.MapPath("~/Config/code.txt");


            StreamWriter stm = null;
            var fi = new FileInfo(filePath1);
            if (fi.Exists)
            {
                //   var stm1 = fi.OpenWrite();
            }




            const Int32 BufferSize = 1024;

            using (var fileStream = fi.Open(FileMode.OpenOrCreate, FileAccess.Read, FileShare.None))
            using (var streamReader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
            {
                string line;
                int ctr = 0;
                while ((line = streamReader.ReadLine()) != null)
                {
                    try
                    {

                        string[] DataIn = line.Split(':');
                        mycode[ctr] = DataIn[0].ToString();
                        mycodeDesc[ctr] = DataIn[1].ToString();
                        ctr = ctr +1;
                    }
                    catch
                    {
                        streamReader.Dispose();
                        return mycode;
                    }


                }
                streamReader.Dispose();
                return mycode;

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
            var filePath1 =  Server.MapPath("~/Config/MyPrinterSetting.txt");
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
 //       public void readTemplateSO3(string DataPath, string PrinterPath, string txtQty, string filepath1, string hSOID, string hDDATE, string hCRYSTAL, string hOCRID, string hBIM, string hQUANTITY, string hPRODUCT, string hBADGEID, string hTOTALWAFER, string hDIETYPE, string template)
 
        public void readTemplateSO3(string DataPath,string PrinterPath, string txtQty, string filepath1, string txtBadgeID, string txtSOID, string template)
    {

            string[] separators1 = new[] { " " };
            string[] separators = new[] { "_" };
            string[] Records;
            string[] Records12NC;
            string bProduct;
            var filePath1 = "";
            string filePathTemplate = System.Web.HttpContext.Current.Server.MapPath("~/Config");
            PrinterPath = System.Web.HttpContext.Current.Server.MapPath("~/image/DOSPrinter.exe");
            //  var filePath2 = Server.MapPath("~/Config/");


            DateTime now = DateTime.Now;
            string DDate = now.ToString("dd/MMM/yyyy");

            using (OracleConnection mesDbConn = new OracleConnection(ConfigurationManager.ConnectionStrings["MES"].ConnectionString))
            {

                mesDbConn.Open();
                using (OracleCommand cmd = mesDbConn.CreateCommand())
                {
                    cmd.CommandText = string.Format(@"select so.containername AS ShopOrder, w.containername AS MESWaferBatch, wso.qtyreserved QtyIn,pb.PRODUCTNAME AS Wafer12NC, p.description CrystalDesc,
                            o.mes_waferoriginname Fab,so.numberofwafer TOTALWAFER,RD.RESOURCENAME BIMLine, 
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

                    OracleParameter pWO = new OracleParameter(":pSOid", txtSOID) { OracleType = OracleType.Char };
                    cmd.Parameters.Add(pWO);
                    using (OracleDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
 
                                string[] ws = reader["MESWaferBatch"].ToString().Split('-');
                                string slice = ws[2].ToString().Substring(1,2);
                                string batch = ws[1].ToString().Substring(0, 6);
                                string bs = batch + slice + "-";
                                string bs1 = MyPrefix + GenerateCheckChar(bs);
                                string cx = reader["CRYSTALDESC"].ToString();
                                string swm = createFWMSaw(cx,bs1);
                                lSOID.Add(reader["ShopOrder"].ToString());
                                lDDATE.Add(DDate);
                                lDEVICE.Add(reader["Device"].ToString());

                                lOCRID.Add(bs1);
                                lBIM.Add(reader["BIMLine"].ToString());
                                lQUANTITY.Add(reader["QtyIn"].ToString());
                                lCRYSTAL.Add(reader["CRYSTALDESC"].ToString());
                                crystal.Add(reader["CRYSTALDESC"].ToString());
                                lBADGEID.Add(txtBadgeID.ToString());
                                lTOTALWAFER.Add(reader["TOTALWAFER"].ToString());
                                lDIETYPE.Add("TWIN");
                                lWAFERBATCH.Add(reader["MESWaferBatch"].ToString());
                            }
                            catch
                            { 
                            }

                        }

                    }
                }

                mesDbConn.Close();
            }




            filePath1 = filePathTemplate  + "\\" + "saw.LBL";

            if (filePath1 == "") { return; }

            int j = 0;
            for (j = 0; j < Convert.ToInt16(lTOTALWAFER[0]); j++)
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
                            LabelData[i] = LabelData[i].Replace("#10#", "");
                        }
                        if (line.Contains("#SOID#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#SOID#", lSOID[j]);
                        }
                        if (line.Contains("#OCRID#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#OCRID#", lOCRID[j]);
                        }

                        if (line.Contains("#DDATE#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#DDATE#", lDDATE[j]);
                        }

                        if (line.Contains("#CRYSTAL#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#CRYSTAL#", lCRYSTAL[j]);
                        }

                        if (line.Contains("#BIM#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#BIM#", lBIM[j]);
                        }

                        if (line.Contains("#QUANTITY#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#QUANTITY#", lQUANTITY[j]);
                        }

                        if (line.Contains("#PRODUCT#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#PRODUCT#", lDEVICE[j]);
                        }

                        if (line.Contains("#BADGEID#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#BADGEID#", txtBadgeID);
                        }

                        if (line.Contains("#TOTALWAFER#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#TOTALWAFER#", lTOTALWAFER[j]);
                        }

                        if (line.Contains("#DIETYPE#") == true)
                        {
                            LabelData[i] = LabelData[i].Replace("#DIETYPE#", lDIETYPE[j]);
                        }
                        i = i + 1;
                        if (chk == false)
                        {

                        }


                    }
                    catch
                    { }
                }
                if (chk == true)
                {


                    writeSettingSO3(DataPath, PrinterPath, txtQty, "aa", "bb", LabelData, lSOID[j], template.ToUpper());
                 //   System.Threading.Thread.Sleep(1000);
                    // j = j + 1;
                }
            }
        } // loop for labelling


        }

        protected string  writeSettingSO3(string DataPath, string PrinterPath, string txtQty, string fpath, string fvalue, string[] MyValue, string WOID, string template)
        {
            try
            {
                string myBarcode = "";
                int Qty = Convert.ToInt16(txtQty);
                System.IO.File.Delete(@"c:\temp\" + WOID + ".tmp");
                using (System.IO.StreamWriter file =
                new System.IO.StreamWriter(@"c:\temp\" + WOID + ".tmp"))
                {
                    int i = 0;
                    do
                    {
                        file.WriteLine(MyValue[i]);
                        myBarcode +=  MyValue[i];
                        i = i + 1;
                        myTotalBarcode += MyValue[i]; 
                    }

                    while (!String.IsNullOrEmpty(MyValue[i]) && i < MyValue.Length);
                }
                try
                {
                   // System.Threading.Thread.Sleep(2000);
                   string  myBarcode1 = myBarcode;
                  string  Propx = @"^XA
^LH30,20
^FO150,10^CI0^A0,N,20,12^FDAH08363B^FS
^FO300,10^CI0^A0,N,20,12^FD12/Apr/2017^FS
^FO420,10^CI0^A0,N,20,12^FD74LVC2G34/AY/8-8006NS-150um^FS
^BY2,2.5,32,N
^FO150,30^B3,,26,N,^FDsA1414217-D0^FS
^FO150,60^CI0^A0,N,20,12^FDsA1414217-D0^FS
^FO350,60^CI0^A0,N,20,12^FD(M)MCDDB-13^FS
^FO550,60^CI0^A0,N,20,12^FD(Q)104657^FS
^FO150,80^CI0^A0,N,20,12^FD74LVC2G34GM^FS
^FO350,80^CI0^A0,N,20,12^FD(B)11012470^FS
^FO480,80^CI0^A0,N,20,12^FD(T)6^FS
^FO550,80^CI0^A0,N,20,12^FD(S)TWIN^FS
^XZ
";

                  var model = new
                  {

                      Prop1 = myBarcode
                      // Prop1 = @"some value that can contain ', \, "", anything you like to throw it in, etc..",
                      //  Prop2 = "some other value"
                  };
                  var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(model);
                  var script = string.Format("foo({0});", json);
                  ClientScript.RegisterStartupScript(GetType(), "foo", script, true);




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

    }
}