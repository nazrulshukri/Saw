using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;


namespace ReadCrystal
{
  //  public partial class SawBarcode : System.Web.UI.Page
    public partial class SawBarcode : Conversion
    {
        public string myBarcode = "";

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

            // readTemplateSO3(DataPath, PrinterPath, "1", filepath1, hSOID, hDDATE, hCRYSTAL, hOCRID, hBIM, hQUANTITY, hPRODUCT, hBADGEID, hTOTALWAFER, hDIETYPE, template);
            readTemplateSO3(DataPath, PrinterPath, "1", filepath1, txtBadgeID.Text, txtSOID.Text, template);



            for (int i = 1; i <=2; i++)
            { 

            string myBarcode = @"^XA
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

            var jsonx = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(myBarcode);
            HtmlGenericControl NewControl = new HtmlGenericControl("button");

            // Set the properties of the new HtmlGenericControl control.
            //'<input type="button" onClick="gotoNode(\'' + result.name + '\')" />'

            string prt = "printStruk(" + jsonx + ")";
            NewControl.ID = "MyButton";
            NewControl.InnerText = "par" + i;
            NewControl.Attributes.Add("value", "");
            NewControl.Attributes.Add("onclick", prt);
            PlaceHolder1.Controls.Add(NewControl);


            }
        }

        protected void txtBarcode_TextChanged(object sender, EventArgs e)
        {

        }
        protected string[] readcode(string[] mycode, string[] mycodeDesc)
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
                        ctr = ctr + 1;
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
        protected void Page_Load(object sender, EventArgs e)
        {

        }



    }
}