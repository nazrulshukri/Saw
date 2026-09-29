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
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;

namespace SawSpecial
{
    public partial class Saw : System.Web.UI.Page
    {
        public string myTotalBarcode = "";

        //for wafermap
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
        string SawPrefix = "";

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                cbBimline.Items.Clear();
                readBimLine();
                //string awacPathLogPath = readawacsPath();
                //string[] AP = awacPathLogPath.Split(',');
                //string awacsPath = AP[0].ToString();
                //string LogPath = AP[1].ToString(); 
                //TextBox1.Text = awacsPath;
                // createFWM();     

                //MySaw.saw sawPrefix = new MySaw.saw();
                //sawPrefix.Name = "S";
                //SawPrefix = sawPrefix.Name;
            }

            //MySaw.saw sawPrefix = new MySaw.saw();
            //sawPrefix.Name = "S";
            //SawPrefix = sawPrefix.Name;
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
        public string createFWMSaw(string CRYSTALDESC, string ocrid, string SITwin, string SOID, string cx , string device, string groupFWM, string fab)
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
            // string myhost = "mygsermys1ms025:3456";
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



            for (int i = 0; i < crystaldescdB.Count; i++)
            {
                if (crystaldescdB[i].ToString().ToUpper() == CRYSTALDESC.ToString().ToUpper().Trim())
                {

                    swm = RecipedB[i].ToString();
                }

            }


            string wafercode = "0";
            string statusFWM = "0";
 
            if (groupFWM != "") { statusFWM = "1"; }

            MySaw.saw sawPrefix = new MySaw.saw();
            if (cx.Contains("TA") == true || cx.Contains("TC")) { wafercode = "1"; }  //TATC

            //initialize sawPrefix
            //sawPrefix.Name = "";
            //SawPrefix = "";
            //--

            if (wafercode == "1" & statusFWM == "1")
            {
                sawPrefix.Name = "]";
                SawPrefix = "]";
            }

            //--
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


            if (groupFWM != "") { swm = "FWM"; }
            if (swm != "" && swm !="FWM")
            {

                // string xx = GenerateCheckChar(ocrid);
                //  ocrid = SawPrefix + xx;
                if (SITwin == "TWIN")
                {
                    ocrid = SawPrefix + ocrid + "TWIN";
                    swm = swm +"_" +"TWIN";
                }
                else
                { }
                string url = "http://" + myhost + "/template/createmapfromswm.html?auth=admin:admin&swm=" + swm + ".dm1&ocrid=" + SawPrefix + ocrid;
                //WebRequest webRequest = WebRequest.Create(url);
                //WebResponse webResp = webRequest.GetResponse();
                string content = "";


                //  string MyResult = "";
                string[] awacsresponse = new string[] { "swm is not valid\n", "failed\n" };

                using (WebResponse wr = WebRequest.Create(url).GetResponse())
                {
                    using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                    {
                        content = sr.ReadToEnd();
                        
                        if (awacsresponse.Contains(content) == false)
                        {
                            MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                          //  if (content.Contains("swm is not valid") == false)
                          //  {
                                swm = SawPrefix + ocrid;
                          //  }                       
                        }


                    }
                }

                if (awacsresponse.Contains(content) == true) // if (content.Contains("swm is not valid") == true)
                {
                    url = "http://" + myhost + "/template/createmapfromswm.html?auth=admin:admin&swm=" + swm + ".dms&ocrid=" + SawPrefix + ocrid;

                    content = "";
                    using (WebResponse wr = WebRequest.Create(url).GetResponse())
                    {
                        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                            if (awacsresponse.Contains(content) == false)
                            {
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                             //   if (content.Contains("swm is not valid") == false)
                             //   {
                                    swm = SawPrefix + ocrid;
                            //    }
                            }
                        }
                    }
                }

                if (awacsresponse.Contains(content) == true) // if (content.Contains("swm is not valid") == true)
                {
                    url = "http://" + myhost + "/template/createmapfromswm.html?auth=admin:admin&swm=" + swm + ".xml&ocrid=" + SawPrefix + ocrid;

                    content = "";
                    using (WebResponse wr = WebRequest.Create(url).GetResponse())
                    {
                        using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                            if (awacsresponse.Contains(content) == false)
                            {
                                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + SawPrefix + ocrid + "," + swm + "," + content.Substring(0, content.Length - 1);
                                //   if (content.Contains("swm is not valid") == false)
                                //   {
                                swm = SawPrefix + ocrid;
                                //    }
                            }
                        }
                    }
                }

                if (awacsresponse.Contains(content) == true)  //if (content.Contains("swm is not valid") == true)
                {
                    string fp = @"c:\awacs\site\swm\" + swm + ".xml";
                 //   string fpnew = @"c:\awacs\site\swm\" + ocrid + ".xml";  /remove for server
                    string fpnew = @"\\mygsermys1ms025\swm\" + SawPrefix + ocrid + ".xml";
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
            } // end for swm != ""
            else if (swm == "FWM" && fab != "SILAN")
            {
                //ocrid = ocrid.Substring(1, ocrid.Length - 1);
                ocrid = ocrid.Substring(0, ocrid.Length );
                string urlFWM = @"http://" + myhost + @"/template/awacs_cloudsearch.html?cloudsearcho=" + ocrid + "&cloudsearchp=*&cloudsearcht=*WMAP&cloudsearchwo=*&cloudsearchws=*&cloudsearchr=*&cloudsearchd=*";
                urlFWM = @"http://mygsermys1ms026/wafermap/" + ocrid + ".xml";    
                //WebRequest webRequest = WebRequest.Create(url);
                //WebResponse webResp = webRequest.GetResponse();
                string content = "";

                //MySaw.saw sawPrefix = new MySaw.saw();
                //SawPrefix = sawPrefix.Name;
                if (SawPrefix == null) { SawPrefix = ""; }

                //  string MyResult = "";
                try
                { 
                using (WebResponse wr = WebRequest.Create(urlFWM).GetResponse())
                {
                    using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                    {
                        content = sr.ReadToEnd();

                        string s = content;
                        //int count1 = 0;

                        //foreach (Match m in Regex.Matches(s, ocrid))
                        //    count1++;
                        //if (count1 > 2)
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
            else if (swm == "FWM" && fab == "SILAN")
            {
                ocrid = ocrid.Substring(1, ocrid.Length - 1);
                MyResult = MyDate + "," + SOID + "," + device + "," + cx + "," + ocrid + "," + swm + "," + "FWM aded";
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
                    { }
                }
                return sAwacsPathLogPath;
            }
        }
        public List<string> getdmanCrystal()
        {
            //List<string> dmanCrystal = dmanCrystal();  //use this for dman
            List<string> dmanCrystal = new List<string>();
            var filePath1 = Server.MapPath("~/Config/dman_exclude.txt");
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
                    { }
                }
                return dmanCrystal;
            }
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
                    { }
                }

            }
        }
        //       public void readTemplateSO3(string DataPath, string PrinterPath, string txtQty, string filepath1, string hSOID, string hDDATE, string hCRYSTAL, string hOCRID, string hBIM, string hQUANTITY, string hPRODUCT, string hBADGEID, string hTOTALWAFER, string hDIETYPE, string template)

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
//                    cmd.CommandText = string.Format(@"select so.containername AS ShopOrder, w.containername AS MESWaferBatch, wso.qtyreserved QtyIn,pb.PRODUCTNAME AS Wafer12NC, p.description CrystalDesc,
//                            o.mes_waferoriginname Fab,so.numberofwafer TOTALWAFER,RD.RESOURCENAME BIMLine, 
//                            w.DIFFUSIONBATCHCODE AS DiffusionBatch,pf.productfamilyname Device
//                            from container so, container w, mes_wafersusedbyso wso,product p, productbase pb, mes_waferorigin o,RESOURCEDEF RD, PRODUCTTYPE pt,PRODUCTFAMILY pf
//                            where wso.mes_shopordercontainerid(+)= so.containerid
//                            and wso.mes_wafercontainerid = w.containerid(+)
//                            and w.productid=p.productid
//                            and p.productbaseid=pb.productbaseid
//                            and p.productid=pb.REVOFRCDID
//                            and SO.PRODUCTFAMILYID = pf.PRODUCTFAMILYID
//                            and o.mes_waferoriginid = p.mes_waferoriginid
//                            and pt.PRODUCTTYPEID = p.PRODUCTTYPEID
//                            and RD.RESOURCEID(+) = SO.MES_EQUIPMENTID
//                            -- and so.containername = :pWOid
//                            and so.containername = :pSOid
//                            order by 1,2 ");

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
and so.containername = :pSOid                  
                            ");


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

                                if ((fab == "ICN6" && waferSize.ToString() =="6") || Extra == "NOTInSAP") { FlagException = true; }
                                if (FlagException == true) 
                                { 
                                  // lotfi temp off 8-8-2017
                                  //  if (lstOCRID.Items.Count < 1) {
                                  //      Response.Write("<script>alert('ICN6, Must use OCRID');</script>");
                                  //      return;
                                  //  }
                                    for (int i = 0; i < lstOCRID.Items.Count; i ++)
                                    {

                                        string[] myWaferInfo;
                                        string myWaferBatch = "";
                                        string Slice = "";
                                        if (Extra == "NOTInSAP")

                                        {  //for non
                                            myWaferInfo = lstOCRID.Items[LstOCRIDCounter].ToString().Split('-');
                                            int SliceLen = myWaferInfo[0].ToString().Length;
                                            Slice = myWaferInfo[1].ToString();
                                           //not useful for NotInSAP case.
                                          //  myWaferBatch = "W-" + myWaferInfo[0].ToString().Substring(0, 5) + "0-0" + Slice;                                          
                                        }
                                        if (fab == "ICN6" && waferSize.ToString() =="6")
                                        {  
                                            myWaferInfo = lstOCRID.Items[LstOCRIDCounter].ToString().Split('-');
                                            myWaferBatch = "W-" + myWaferInfo[0].ToString().Substring(0, 6) + "-0" + myWaferInfo[1].ToString().Substring(0, 2);
                                            string[] WInfo = myWaferInfo[1].Split('.');
                                            try
                                            { Slice = WInfo[0].ToString(); }
                                            catch
                                            { }                                                                                                                           
                                        }

                                        if (ws1 == myWaferBatch || ws1.Substring(ws1.ToString().Length -2,2)==Slice)
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

                                     bs = fabname(fab, ws1,waferSize);
                                     if (fab == "DMAN")
                                     {
                                         List<string> dmanXtal = getdmanCrystal();  //use this for dman
                                         if (dmanXtal.Contains("testonly") == true)
                                         { bsFWM = bs; }
                                     }
                                     else if (fab == "SILAN")
                                     {
                                         bsFWM = bs; }
                                     else
                                     { bsFWM = GenerateCheckChar(bs); }
                                }
                            //    LstOCRIDCounter = LstOCRIDCounter + 1;

                            //    bs = fabname(fab, ws1);
                           //     bsFWM = GenerateCheckChar(bs);   
                                if ((FlagException == false) || (FlagException == true && FlagICN6 == true))
                                {
                                    if (fab.Trim() == "PHENITEC")
                                    {
                                        bsFWM = bsFWM.Substring(0, bsFWM.Length - 2);
                                    }
                                     string bs1 =  bsFWM;  //lotfi


                                    //string cx = reader["CRYSTALDESC"].ToString();
                                    string myGroupFWM = "";
                                    //string Extra = getExtra(cx);
                                    if (Extra == "NOTInSAP" && txt2DScan.Text.Length < 1)
                                    {
                                    //    Response.Write("<script>alert('OCRID inconsistent in SAP detected, Must Scan BarCode');</script>");
                                    //    return;

                                    }

                                    if ((fab == "DHAM" || fab == "ICN6" || fab == "ICN8") && cx.Substring(0, 1).ToString() == "8" && Extra !="SWM")
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
                                    swmfwm = createFWMSaw(cx, bs1, SITwin, txtSOID, cx, dev, myGroupFWM, fab);

                                   


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


                                    MyOCRID.Add(swmfwm);
                                    lSOID.Add(reader["ShopOrder"].ToString());
                                    lDDATE.Add(DDate);
                                    lDEVICE.Add(reader["Device"].ToString());
                                    string wb = reader["MESWaferBatch"].ToString();
                                    if (swmfwm.Substring(0, 1).ToString() == "S")
                                    {                                         
                                        lOCRID.Add(SawPrefix + bs1);
                                        hOCRID.Add(bs1);
                                    }
                                    else if  (!swmfwm.ToUpper().Contains("NOSWM") &&((swmfwm.Substring(0, 1).ToString() == "f") || (myGroupFWM == "f")))
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

                                    if (fab == "DHAM" && swmfwm.Substring(0, 1).ToString() != SawPrefix) { myBin = "1-7"; }
                                    else
                                    { myBin = "1"; }
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
            { int catcherror = Convert.ToInt16(lTOTALWAFER[0]); }
            catch
            { return; }

            filePath1 = filePathTemplate + "\\" + "saw.LBL";

            if (filePath1 == "") { return; }

            int j = 0;
            string[] LabelDataTotal = new string[1000000];
            int tTotal = 0;
            j = lOCRID.Count - 1;   //asap
           // j = Convert.ToInt16(lTOTALWAFER[0]) - 1; dont use this since there is a glitch in MES whre the qty not tally.
            if (lstOCRIDSuccess.Items.Count > 0)
            { j = lstOCRIDSuccess.Items.Count - 1; }

           
             for ( int rx = j; j >= 0; j--)
          //  for (j = 0; j < Convert.ToInt16(lTOTALWAFER[0]); j++)
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
                            if (line.Contains("#OCRID#") == true  && line.Contains("^B")==true)
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
                                    nmap0 = nmap[0].ToString() + "-" + nmap[1].ToString() ;
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
                            




                            i = i + 1;
                            tTotal = tTotal + 1;
                            if (chk == false)
                            {

                            }


                        }
                        catch
                        { }
                    }
                    if (chk == true)
                    {


                        writeSettingSO3(DataPath, PrinterPath, txtQty, "aa", "bb", LabelData, lSOID[j], template.ToUpper(), lOCRID[j], lBIM[j], lQUANTITY[j], lCRYSTAL[j], crystal[j], lBADGEID[j], lTOTALWAFER[j], lDIETYPE[j], lWAFERBATCH[j], MyOCRID[j], hOCRID[j]);
                        //   System.Threading.Thread.Sleep(1000);
                        // j = j + 1;
                    }
                }
                tTotal = tTotal - 1;
            } // loop for labelling
            writeSettingTotal(DataPath, PrinterPath, txtQty, "aa", "bb", LabelDataTotal, lSOID[0], template.ToUpper(), lTOTALWAFER[0]);


        }
        protected string writeSettingTotal(string DataPath, string PrinterPath, string txtQty, string fpath, string fvalue, string[] MyValue, string WOID, string template,string lTOTALWAFER)
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
                    HtmlGenericControl NewControl = new HtmlGenericControl("button");

                    // Set the properties of the new HtmlGenericControl control.
                    //'<input type="button" onClick="gotoNode(\'' + result.name + '\')" />'

                    string prt = "printStruk(" + json + ")";
                    NewControl.ID = "MyButton";
                    NewControl.InnerText = "Print All";
                    NewControl.Attributes.Add("value", "");
                    NewControl.Attributes.Add("onclick", prt);
                    //PlaceHolder1.Controls.Add(new HtmlGenericControl("br"));
                    PlaceHolder1.Controls.Add(new LiteralControl("<br/>"));
                    PlaceHolder1.Controls.Add(NewControl);
                    PlaceHolder1.Controls.Add(new LiteralControl("<br/>"));
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
                        myTotalBarcode += MyValue[i];
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
                        map = swmfwm.Substring(1,swmfwm.Length -1);                     
                    }
                    else
                    {map = "NoMap";}
                    var json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(myBarcode);
                   
                    HtmlGenericControl NewControl = new HtmlGenericControl("button");

                    // Set the properties of the new HtmlGenericControl control.
                    //'<input type="button" onClick="gotoNode(\'' + result.name + '\')" />'

                    string prt = "printStruk(" + json + ")";
                    NewControl.ID = "MyButton";
                    NewControl.InnerText = lCRYSTAL + " " + lWAFERBATCH + " " + lQUANTITY + " " + map;
                    NewControl.Attributes.Add("value", "");
                    NewControl.Attributes.Add("onclick", prt);
                    //PlaceHolder1.Controls.Add(new HtmlGenericControl("br"));
                    PlaceHolder1.Controls.Add(NewControl);
                    PlaceHolder1.Controls.Add(new LiteralControl("<br/>"));
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

        protected void btnPrint_Click(object sender, EventArgs e)
        {
        }

        protected void btnPrint_Click3(object sender, EventArgs e)
        {
            string DataPath = "";
            string PrinterPath = "";
            string txtQty = "1";
            string filepath1 = "";
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
            string template = "saw.LBL";
            string RecipeExtra = "";

            lstOCRIDError.Items.Clear();
            lstOCRIDSuccess.Items.Clear();

            string [] FabInfo = getWaferInfo(txtSOID.Text.ToString());
            string Fab = FabInfo[0];
            string WaferDesc = FabInfo[3];
            string WaferSize = FabInfo[4];
            string Extra = getExtra(WaferDesc);
           // if (txtBadgeID.Text.ToString().Length > 1 && txtSOID.Text.ToString().Length > 1 && txt2DScan.Text.ToString().Length > 1)
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

            // readTemplateSO3(DataPath, PrinterPath, "1", filepath1, hSOID, hDDATE, hCRYSTAL, hOCRID, hBIM, hQUANTITY, hPRODUCT, hBADGEID, hTOTALWAFER, hDIETYPE, template);


            readTemplateSO3(DataPath, PrinterPath, "1", filepath1, txtBadgeID.Text, txtSOID.Text, template, WaferSize.ToString().Trim());
            for (int i = 0; i < lstOCRID.Items.Count; i++)
            {
                lstOCRIDError.Items.Add(lstOCRID.Items[i].ToString());
            }
            lstOCRID.Items.Clear();
        }
        public string fabname(string fab, string waferbatch, string wafersize)
        {
            string myWaferBatch = "none";
            string[] wf = waferbatch.Split('-');
            if (fab.Trim() == "ASMC-3") { myWaferBatch = wf[1].Substring(0, 6) + "-" + wf[2].Substring(1, 2); }
            if (fab.Trim() == "Calamba") { myWaferBatch = wf[1].Substring(0, 5) + "W" + wf[2].Substring(1, 2) + "-"; }
            if (fab.Trim() == "DHAM") { myWaferBatch = wf[1].Substring(0, 5) + "W" + wf[2].Substring(1, 2) + "-"; }
            if (fab.Trim() == "DHZG") { myWaferBatch = wf[1].Substring(0, 6) + "-" + wf[2].Substring(1, 2); }
            if (fab.Trim() == "DMAN") { myWaferBatch = wf[1].Substring(0, 7) + "  " + wf[2].Substring(1, 2) + "  "; }//{ myWaferBatch = wf[1].Substring(0, 7) + "-" + wf[2].Substring(1, 2); }
            if (fab.Trim() == "GF-2") {
                string[] Param = wf[1].ToString().Split('.');
                myWaferBatch = Param[0].ToString() + "W" + wf[2].Substring(1, 2);                                               
            } 

            if (fab.Trim() == "ICH") { myWaferBatch = wf[1].Substring(0, 4) + "W" + wf[2].Substring(1, 2) + "-"; }
            if (fab.Trim() == "ICN4") { myWaferBatch = wf[1].ToString() + "-" + wf[2].Substring(1, 2) + "-"; }
            if (fab.Trim() == "ICN6") { myWaferBatch = wf[1].ToString() + "F" + "-" + wf[2].Substring(1, 2) + "."; }
            if (fab.Trim() == "ICN6" && wafersize == "6") { myWaferBatch = wf[1].ToString() + "F" + "-" + wf[2].Substring(1, 2) + "."; }
            if (fab.Trim() == "ICN6" && wafersize == "8") { myWaferBatch = wf[1].Substring(0, 6) + "W" + wf[2].Substring(1, 2); }
            if (fab.Trim() == "ICN8") { myWaferBatch = wf[1].Substring(0, 6) + "W" + wf[2].Substring(1, 2); }
            if (fab.Trim() == "M-MOS") { myWaferBatch = wf[1].ToString() + "-" + wf[2].Substring(1, 2) + "-"; }
            if (fab.Trim() == "MOSEL") { myWaferBatch = wf[1].ToString() + "-" + wf[2].Substring(1, 2) + "-"; }
            if (fab.Contains("MMS/GA") == true) { myWaferBatch = wf[1].Substring(0, 5) + "W" + wf[2].Substring(1, 2) + "-"; }
            if (fab.Trim() == "PHENITEC") { myWaferBatch = wf[1].Substring(0, 4) + "-" + wf[1].Substring(4, 4) + "-" + wf[2].Substring(1, 2); }
            if (fab.Trim() == "SILAN") { 
                myWaferBatch = wf[1].ToString() + "-" + wf[2].Substring(1, 2); }
            if (fab.Trim() == "SSMC") {
                char[] delimiters = new char[] { '.', '-' };
                string[] parts = wf[1].ToString().Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
                myWaferBatch = parts[0].ToString() + "-" + wf[2].Substring(1, 2); 
            }
            if (fab.Trim() == "VANG") {
                string[] Param = wf[1].ToString().Split('.');
                myWaferBatch = Param[0].ToString() + "-" + wf[2].Substring(1, 2);            
            }

            return myWaferBatch;     
        }

        protected void cbBimline_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        protected void btnTest_Click(object sender, EventArgs e)
        {
            string SawDirection = "";
            string SawQtySplit = "";
            string SawMapFile = "";
            readSI(txtSOID.Text.ToString(), ref SawDirection, ref SawQtySplit, ref SawMapFile);

        }

        protected void btnAdd_Click(object sender, EventArgs e)
        {
            string MyOCRScan = "";
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



                //if (lstOCRID.Items.Cast<Object>().Any(x => x.ToString() == newValue))
                //{
                //    lstOCRID.Items.Add(fs[4].ToString());
                //  //  return;
                //}
                txt2DScan.Text = "";
            }
            catch
            { }
        }
        public void Read2DScan()
        {
            string MyOCRScan = "";
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
            { }
        
        }
        protected void btnDelete_Click(object sender, EventArgs e)
        {

            int SelIndex = this.lstOCRID.SelectedIndex;
            if (this.lstOCRID.SelectedIndex >= 0) 
            { this.lstOCRID.Items.RemoveAt(SelIndex);
            this.lstOCRIDAll.Items.RemoveAt(SelIndex);
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

        protected void btnAddOCRID_Click(object sender, EventArgs e)
        {
            try
            {

                //   "12NC", "BATCH", "QTY", "OCR" 
                if (txt2DScan.Text.ToString().Contains("OCR") == true || txt2DScan.Text.ToString().Length > 0)
                {
                    Read2DScan();
                }
            }
            catch
            {
                Response.Write("<script>alert('Warning: Invalid waferid scan');</script>");
                return;

            }   
        }
        protected string [] getWaferInfo(string SOID)
        {
            string  MyFab,Device,BimLine,Crystal;
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
                               WaferInfo[i + 1]  = reader["Device"].ToString();
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
        protected string[,] getWaferInfo2D(string SOID)
        {
            string MyFab, Device, BimLine, Crystal;

            string[,] WaferInfo2D = new string[1000, 7];

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



                                WaferInfo2D[i, 0] = reader["FAB"].ToString();
                                WaferInfo2D[i, 1] = reader["SHOPORDER"].ToString();    
                                WaferInfo2D[i, 2] = reader["CRYSTALDESC"].ToString();
                                WaferInfo2D[i, 3] = reader["DEVICE"].ToString();
                                WaferInfo2D[i, 4] = reader["WAFERSIZE"].ToString();
                                WaferInfo2D[i, 5] = reader["QTYIN"].ToString();
                                WaferInfo2D[i, 6] = reader["MESWAFERBATCH"].ToString();

                                i = i + 1;
                            }
                            catch
                            {
                            }

                        }


                    }
                }

                mesDbConn.Close();
            }
            return WaferInfo2D;
        }
        protected void Button1_Click(object sender, EventArgs e)
        {


            string[,] FabInfo = getWaferInfo2D(txtSOID.Text.ToString());
            //WaferInfo2D[i, 0] = reader["FAB"].ToString();
            //WaferInfo2D[i, 1] = reader["SHOPORDER"].ToString();
            //WaferInfo2D[i, 2] = reader["CRYSTALDESC"].ToString();
            //WaferInfo2D[i, 3] = reader["DEVICE"].ToString();
            //WaferInfo2D[i, 4] = reader["WAFERSIZE"].ToString();
            //WaferInfo2D[i, 5] = reader["QTYIN"].ToString();
            //int rowlen = FabInfo.GetLength(0);
            int collen = FabInfo.GetLength(1);
            int rowlenL = FabInfo.GetUpperBound(0);
            int collenU = FabInfo.GetUpperBound(1);
            int rowlen = 0;
            try
            {
                while (FabInfo[rowlen, 0].ToString() != null)
                {
                    rowlen = rowlen + 1;
                }
            }
            catch
            { }





            // Create a new HtmlTable object.        
            HtmlTable table1 = new HtmlTable();
            // Set the table's formatting-related properties.        
            table1.Border = 1;
            table1.CellPadding = 3;
            table1.CellSpacing = 3;
            table1.BorderColor = "red";
            // Start adding content to the table.        
            HtmlTableRow row;
            HtmlTableCell cell;
            for (int i = 0; i < rowlen; i++)
            {
                // Create a new row and set its background color.                
                row = new HtmlTableRow();
                row.BgColor = (i % 2 == 0 ? "lightyellow" : "lightcyan");
                for (int j = 0; j < 8 ; j++)
                {
                    // Create a cell and set its text.                        
                    //cell = new HtmlTableCell();
                    //cell.InnerHtml = "Row: " + i.ToString() + "<br />Cell: " + j.ToString();
                    //// Add the cell to the current row.                        
                    //row.Cells.Add(cell);  
                    cell = new HtmlTableCell();
                    if (i == 0 & j == 0) {cell.InnerHtml = "FAB"; }
                    else if (i == 0 & j == 1) {cell.InnerHtml = "SHOPORDER";}
                    else if (i == 0 & j == 2) {cell.InnerHtml = "CRYSTAL";}
                    else if (i == 0 & j == 3) { cell.InnerHtml = "DEVICE"; }
                    else if (i == 0 & j == 4) {cell.InnerHtml = "WAFERSIZE";}
                    else if (i == 0 & j == 5) { cell.InnerHtml = "QUANTITY"; }
                    else if (i == 0 & j == 6) { cell.InnerHtml = "MES BATCH"; }
                    else if (i == 0 & j == 7) { cell.InnerHtml = "WAFER BATCH"; }
                    else
                    { if (j < 7) { cell.InnerHtml = FabInfo[i, j].ToString(); }
                    if (j == 7) { cell.InnerHtml = ""; } 
                    }
                    

                    
                    // Add the cell to the current row.                        
                    row.Cells.Add(cell);


                }
                // Add the row to the table.                
                table1.Rows.Add(row);
            }
            // Add the table to the page.        
            this.Controls.Add(table1);
         }








    }
}