using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace WR5
{
    public class Program
    {
        static void Main(string[] args)
        {
            //XmlDocument myXML = new XmlDocument();

            //string strXML = "<Root>";
            //strXML += "<PraxisID>1002</PraxisID>";
            //strXML += "<MitarbeiterID>0</MitarbeiterID>";
            //strXML += "<RitmoSeriennummer>F88A5EA4BFAA</RitmoSeriennummer>";
            //strXML += "<PatientNachname>Behn Testfall</PatientNachname>";
            //strXML += "<PatientVorname>Ditmar</PatientVorname>";
            //strXML += "<PatientBirthdate>1965-05-30T00:00:00</PatientBirthdate>";
            //strXML += "<PatientNummer>4712</PatientNummer>";
            //strXML += "<PatientGeschlecht>1</PatientGeschlecht>";
            //strXML += "<PatientAbrechnungsart>1</PatientAbrechnungsart>";
            //strXML += "<Auswertungsdauer>1</Auswertungsdauer>";
            //strXML += "<BefundungsdatumGewuenscht>2022-05-16T00:00:00</BefundungsdatumGewuenscht>";
            //strXML += "</Root>";

            //myXML.LoadXml(strXML);
            //XmlNode myNode = myXML.DocumentElement;
            //string strResult = UploadZIPFile("https://localhost:44325/api/FileUpload", "C:\\Daten\\", "1.edf", myNode);
            string strResult = GetFallByRitmo("https://localhost:44325/api/FileUpload", "ih48UNQ4juzONf7jXAm25oMmgtR+a9gjf33tatxdj+Xhrl/pBUFAgA==", "praxis@dpv-intern.com", "DCB5-C07TA000566");

            FallClass myFall = new FallClass();
            myFall.PatientNachname = "Behn040";
            myFall.PatientVorname = "Ditmar";
            myFall.RitmoBluetoothID = "DCB5-C07TA000566";
            myFall.Status = 0;
            string strFall = JsonSerializer.Serialize(myFall);
            //string strResult = CreateFallOnServer("https://localhost:44325/api/FileUpload", "ih48UNQ4juzONf7jXAm25oMmgtR+a9gjf33tatxdj+Xhrl/pBUFAgA==", "praxis@dpv-intern.com", "DCB5-C07TA000566", strFall);
            //string strResult = Upload2("https://localhost:44325/api/FileUpload", "C:\\Daten\\", "_CDg2gfkHZD.tar.gz", myNode);

        }
        public static string UploadZIPFile(string strURL, string strPath, string strFileName, XmlNode myFields)
        {
            string strResult = "";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var myRequest = new HttpRequestMessage(new HttpMethod("POST"), strURL))
                    {
                        var multipartContent = new MultipartFormDataContent();

                        if (myFields.SelectNodes("/*") != null)
                        {
                            foreach (XmlNode myNode in myFields.ChildNodes)
                            {
                                multipartContent.Add(new StringContent(myNode.InnerText), myNode.Name);
                            }
                        }
                        FileStream myFile = new FileStream(Path.Combine(strPath, strFileName), FileMode.Open);
                        myFile.ReadByte();
                        multipartContent.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(strPath, strFileName))), "file", "@" + strFileName);
                        //multipartContent.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(strPath, strFileName))), "file", "@" + strFileName);
                        FileStream fileToUpload = File.OpenRead(Path.Combine(strPath, strFileName));
                        HttpContent content = new StreamContent(fileToUpload);
                        multipartContent.Add(content);

                        myRequest.Content = multipartContent;

                        //var response = httpClient.Send(myRequest);
                        //strResult = response.ToString();
                        
                        bool keepTracking = true; //to start and stop the tracking thread
                        new Task(new Action(() => { progressTracker(fileToUpload, ref keepTracking); })).Start();
                        var response = httpClient.Send(myRequest);
                        keepTracking = false; //stops the tracking thread}
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return strResult;
        }
        public static string GetFallByRitmo(string strURL, string strToken, string strEMail, string strDeviceSerial)
        {
            string strResult = "";

            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var myRequest = new HttpRequestMessage(new HttpMethod("GET"), strURL))
                    {
                        var multipartContent = new MultipartFormDataContent();
                        multipartContent.Add(new StringContent(strToken), "Token");
                        multipartContent.Add(new StringContent(strEMail), "EMail");
                        multipartContent.Add(new StringContent(strDeviceSerial), "DeviceSerial");

                        myRequest.Content = multipartContent;
                        HttpResponseMessage response = httpClient.Send(myRequest);

                        string strHeaders = response.Headers.ToString();
                        foreach (var item in strHeaders.Split(System.Environment.NewLine))
                        {
                            if (item.StartsWith("fall: "))
                            {
                                string strJSONString = item.Substring(6);
                                var JSONObj = System.Text.Json.JsonSerializer.Deserialize<FallClass>(strJSONString);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return strResult;
        }
        public static string CreateFallOnServer(string strURL, string strToken, string strEMail, string strDeviceSerial, string strFall)
        {
            string strResult = "";
            string strNewDeviceSerial = strDeviceSerial.Substring(strDeviceSerial.IndexOf("-") + 1);

            Console.Write("");
            try
            {
                using (var httpClient = new HttpClient())
                {
                    using (var myRequest = new HttpRequestMessage(new HttpMethod("PUT"), strURL))
                    {
                        var multipartContent = new MultipartFormDataContent();
                        multipartContent.Add(new StringContent(strToken), "Token");
                        multipartContent.Add(new StringContent(strEMail), "EMail");
                        multipartContent.Add(new StringContent(strNewDeviceSerial), "DeviceSerial");
                        multipartContent.Add(new StringContent(strFall), "Fall");

                        myRequest.Content = multipartContent;
                        HttpResponseMessage response = httpClient.Send(myRequest);

                        string strHeaders = response.Headers.ToString();
                        foreach (var item in strHeaders.Split(System.Environment.NewLine))
                        {
                            if (item.StartsWith("fallkennung: "))
                            {
                                string strFallkennung = item.Substring(13);
                                strResult = strFallkennung;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return strResult;
        }

        public static string Upload2(string strURL, string strPath, string strFileName, XmlNode myFields)
        {
            string strResult = "";
            FileStream fileToUpload = File.OpenRead(Path.Combine(strPath, strFileName));

            using (var httpClient = new HttpClient())
            {
                HttpContent content = new StreamContent(fileToUpload);
                //HttpRequestMessage msg = new HttpRequestMessage(new HttpMethod("POST"), strURL)
                //{
                //    Content = content,
                //    RequestUri = new Uri(strURL)
                //};
                
                using (var myRequest = new HttpRequestMessage(new HttpMethod("POST"), strURL))
                {
                    var multipartContent = new MultipartFormDataContent();

                    if (myFields.SelectNodes("/*") != null)
                    {
                        foreach (XmlNode myNode in myFields.ChildNodes)
                        {
                            multipartContent.Add(new StringContent(myNode.InnerText), myNode.Name);
                        }
                    }
                    //multipartContent.Add(new ByteArrayContent(File.ReadAllBytes(Path.Combine(strPath, strFileName))), "file", "@" + strFileName);
                    multipartContent.Add(content);
                    myRequest.Content = multipartContent;

                    bool keepTracking = true; //to start and stop the tracking thread
                    new Task(new Action(() => { progressTracker(fileToUpload, ref keepTracking); })).Start();
                    var response = httpClient.Send(myRequest);
                    //var response = httpClient.Send(myRequest);
                    keepTracking = false; //stops the tracking thread}
                }
            }
            return strResult;
        }
        static void progressTracker(FileStream streamToTrack, ref bool keepTracking)
        {
            int prevPos = -1;
            while (keepTracking)
            {
                int pos = (int)Math.Round(100 * (streamToTrack.Position / (double)streamToTrack.Length));
                if (pos != prevPos)
                {
                    Console.WriteLine(pos + "%");

                }
                prevPos = pos;

                Thread.Sleep(100); //update every 100ms
            }
        }
    }
}
