using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Xml;
using System.Xml.Linq;

namespace SawBarcode
{
    public class utility
    {
        public string testresponse;

        public string checkMESCAMSTAR(string woid)
        {
            string myresult = "";
            try
            {

                string[] arrMWO1D = { "P", "E", "L", "V", "K", "R" };
                string[] arrMWO2D = { "1", "6", "7" };

                string[] arrLotID1D = { "T", "W", "M" };
                string[] arrLotID3D = { "P", "E", "R", "L", "V" };



                if (arrMWO1D.Contains(woid.Substring(0, 1)) && arrMWO2D.Contains(woid.Substring(1, 1))) 
                {
                    myresult = "CAMSTAR";
                }
                else if (arrLotID1D.Contains(woid.Substring(0, 1)) && arrLotID3D.Contains(woid.Substring(2, 1)))
                {
                    myresult = "CAMSTAR";
                }
                else
                {
                    myresult = "MES";
                }
                return myresult;
            }
            catch
            { return myresult; }

        }

        public string getMWO(string LotID)
        {
            string result = "none";
            camstarProd.Ad_HocTxn mycamstar = new camstarProd.Ad_HocTxn();
            //MWO P130105721
            try
            {
                if (LotID.Substring(0, 1) == "P")
                {
                    result = LotID;
                }
                if (LotID.Substring(0, 1) == "T")
                {
                    var InfoByLotID = mycamstar.GetQueryResult("AW-GetAllDataByLot", "LotID", LotID);
                    var vMFGORDERNAME = InfoByLotID.SelectNodes("//AW-GetAllDataByLot/MFGORDERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
                    result = vMFGORDERNAME[0].ToString();
                }
            }
            catch
            { }
            return result;
        }

        public string getSI(string MWO)
        {
            string result = "none";
            camstarProd.Ad_HocTxn mycamstar = new camstarProd.Ad_HocTxn();
            //MWO P130105721
            try
            {
                if (MWO.Substring(0, 1) == "P")
                {
                    var InfoByLotID = mycamstar.GetQueryResult("AW-GetAllInfoSI", "MWO", MWO);
                    var vMFGORDERNAME = InfoByLotID.SelectNodes("//AW-GetAllInfoSI/ORDERSTATUSNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
                    result = vMFGORDERNAME[0].ToString();
                }
            }
            catch
            { }
            return result;
        }

        public string getMaterialCheckStatus(string wsid)
        {
            string WSID_MaterialCheckStatus = "";
            string sql_awacs_attrib = "";


            using (System.Data.SqlClient.SqlConnection repDbConn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["RECIPE"].ConnectionString))
            {
                repDbConn.Open();

                System.Data.SqlClient.SqlParameter pWsid = new System.Data.SqlClient.SqlParameter("@wsid", wsid) { SqlDbType = System.Data.SqlDbType.VarChar };


                using (System.Data.SqlClient.SqlCommand cmd = repDbConn.CreateCommand())
                {

                    sql_awacs_attrib = " SELECT * FROM AWACSATTRIB WHERE upper(WSID) =@WSID;"; // upper(wstype) =@wstype AND package = @package AND leadframe12nc = @lf12nc;";
                    cmd.CommandText = sql_awacs_attrib;
                    cmd.Parameters.Add(pWsid);
                    using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
                    {

                        if (reader.Read())
                        {
                            try
                            {
                                WSID_MaterialCheckStatus = reader["MaterialCheck"].ToString().Trim();
                            }
                            catch
                            {
                            }
                        }

                    }

                }
                repDbConn.Close();
            }
            return WSID_MaterialCheckStatus;

        }
        public string geteRMSStatus(string wsid)
        {

            string WSID_eRMSStatus = "";
            string sql_awacs_attrib = "";


            using (System.Data.SqlClient.SqlConnection repDbConn = new System.Data.SqlClient.SqlConnection(System.Configuration.ConfigurationManager.ConnectionStrings["RECIPE"].ConnectionString))
            {
                repDbConn.Open();

                System.Data.SqlClient.SqlParameter pWsid = new System.Data.SqlClient.SqlParameter("@wsid", wsid) { SqlDbType = System.Data.SqlDbType.VarChar };


                using (System.Data.SqlClient.SqlCommand cmd = repDbConn.CreateCommand())
                {

                    sql_awacs_attrib = " SELECT * FROM AWACSATTRIB WHERE upper(WSID) =@WSID;"; // upper(wstype) =@wstype AND package = @package AND leadframe12nc = @lf12nc;";
                    cmd.CommandText = sql_awacs_attrib;
                    cmd.Parameters.Add(pWsid);
                    using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
                    {

                        if (reader.Read())
                        {
                            try
                            {
                                WSID_eRMSStatus = reader["eRMS"].ToString().Trim();
                            }
                            catch
                            {
                            }
                        }

                    }

                }
                repDbConn.Close();
                WSID_eRMSStatus = "Y";
            }
            return WSID_eRMSStatus;

        }

        public class CamstarInfo
        {
            public int RecordID { get; set; }
            public string Fab { get; set; }
            public string Device { get; set; }
            public string BIMLine { get; set; }
            public string CrystalDesc { get; set; }
            public string CrystalBatch { get; set; }
            public string CrystalSize { get; set; }
            public string CrystalQty { get; set; }
        }

        public dynamic  getWaferInfoFromCamstar(string MWO)
        {
            
            var CamstarList = new List<utility.CamstarInfo>();

            List<string> result = new List<string> { };


            SawBarcode.camstarProd.Ad_HocTxn mycamstar = new SawBarcode.camstarProd.Ad_HocTxn();
            //MWO P130071528

            var InfoByLotID = mycamstar.GetQueryResult("AW-GetCrystalDescByMWO", "MWO", MWO);
            InfoByLotID = mycamstar.GetQueryResult("SBC-GetAllInfoByMWO_WaferPrint", "MWO", MWO);

            
            var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vBIMLine = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/RESOURCENAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vWAFERSIZE = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERSIZE").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vCRYSTALDESC = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERDESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            //  var vFab = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/NXPDIFFUSIONCENTERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vWaferBatch = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/WAFERSCRIBENUMBER").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            // var vLotID = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            //Use MWO (MFGORDERNAME) instead of LotID (CONTAINERNAME) due to request from saw team
            var vLotID = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/MFGORDERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vQTY = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/QTY").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vTOTALWAFER = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/TOTALWAFER").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            // var vDevice = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO/DESCRIPTION").OfType<XmlNode>().Select(n => n.InnerText).ToArray();
            var vCONTAINERNAME = InfoByLotID.SelectNodes("//SBC-GetAllInfoByMWO_WaferPrint/CONTAINERNAME").OfType<XmlNode>().Select(n => n.InnerText).ToArray();

            int i = 0;
            foreach (string data in vCONTAINERNAME)
            {
                        double dWaferSize = Convert.ToInt32(vWAFERSIZE[i].ToString()) / 25.4;
                        int iWaferSize;
                        if (dWaferSize < 0)
                        { iWaferSize = (int)(dWaferSize - 0.5); }
                        else
                        { iWaferSize = (int)(dWaferSize + 0.5); }
                    CamstarList.Add(new utility.CamstarInfo
                    {
                        RecordID = 0,
                        Fab = vFab[i].ToString(),
                        Device =vDevice[i].ToString(),
                        BIMLine = vBIMLine[i].ToString(),
                        CrystalDesc = vCRYSTALDESC[i].ToString(),
                        CrystalSize = iWaferSize.ToString(),
                        CrystalBatch = vCONTAINERNAME[i].ToString(),
                        CrystalQty = vQTY[i]

                        
                    });

                    i++;
            }

            var vColumnName = CamstarList.ToArray();

            return vColumnName;
        }

        public string [] getAttrib(string bim)
        {
            string[] wsidAttrib = new string [6];
            //BIM-39A=>AD39A1
            string wsid = "AD" + bim.ToUpper().Replace("BIM-","") + "1";
            //string url = "http://mygsermys1ms028:2345/template/wsdata.xml?ws=" + wsid;
            string url = "http://mygsermys1ms028:8083/template/wsdata.xml?LINE=" + bim;
            string content = "";
            
            try
            {

                using (WebResponse wr = WebRequest.Create(url).GetResponse())
                {
                    using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                    {
                        content = sr.ReadToEnd();
                        var xDoc = XDocument.Parse(content);
                        var Body = xDoc.Root.Element("ws");
                        var Equipment = Body.Element("EQUIPMENT");
                        var Line = Body.Element("LINE");
                        var LineEQ = Body.Element("LINEEQ");
                        var Location = Body.Element("LOCATION");
                        var Model = Body.Element("MODEL");
                      //  wsidAttrib[0] = Model.Value;
                 
                        wsidAttrib[0] = wsid;
                        wsidAttrib[1] = Equipment.Value;
                        wsidAttrib[2] = Line.Value;
                        wsidAttrib[3] = LineEQ.Value;
                        wsidAttrib[4] = Location.Value;
                        wsidAttrib[5] = Model.Value;
                        
                    }
                }
            }
            catch
            {
               
            }

            return wsidAttrib;
        }

        public string getQuartermapQty(string ocrid)
        {

            //string urlFWM = @"http://mygsermys1ms026/wafermap/" + ocrid + ".txt";
            string urlFWM = @"http://myser01ms056/wafermap/" + ocrid + ".txt";

            string content = "";
            string Qty = "";

            try
            {

                using (WebResponse wr = WebRequest.Create(urlFWM).GetResponse())
                {
                    using (System.IO.StreamReader sr = new StreamReader(wr.GetResponseStream()))
                    {
                        content = sr.ReadToEnd();

                        string s = content;
                        string mySearch = "</GoodDevices>";
                        //      <GoodDevices>104751</GoodDevices>
                        bool val = s.Contains(mySearch);
                        if (val == true)
                        {
                            int LenStart = s.IndexOf("<GoodDevices>");
                            int LenEnd = s.IndexOf("</GoodDevices>");

                            Qty = s.Substring(LenStart + 13, LenEnd - (LenStart + 13));
                            return Qty;
                        }
                        else
                        {
                            return Qty;
                        }
                    }
                }
            }
            catch
            {
                return Qty;
            }

        }
        //public static void DelayAction(int millisecond, Action action)
        //{
        //    var timer = new DispatcherTimer();
        //    timer.Tick += delegate
        //    {
        //        action.Invoke();
        //        timer.Stop();
        //    };

        //    timer.Interval = TimeSpan.FromMilliseconds(millisecond);
        //    timer.Start();
        //}

    }
}