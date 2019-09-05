using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AuditoriaAnubisTrades
{
    public partial class frmPrincipal : Form
    {
        public frmPrincipal()
        {
            InitializeComponent();
        }


        public static string getCache(String url, bool wait = true)
        {
            try
            {



                String r = "";
                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                var httpWebRequest = (HttpWebRequest)WebRequest.Create(new Uri(url));
                httpWebRequest.Method = "GET";
                var httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
                var responseStream = httpWebResponse.GetResponseStream();
                if (responseStream != null)
                {
                    var streamReader = new StreamReader(responseStream);
                    r = streamReader.ReadToEnd();
                }
                if (responseStream != null) responseStream.Close();




                return r;
            }
            catch (WebException ex)
            {
                return null;
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            run();
        }


        private void log(string value)
        {
            listBox1.Items.Add("[" + DateTime.Now.ToString() + "] - " + value);
            this.listBox1.SelectedIndex = this.listBox1.Items.Count - 1;
            this.Show();
            this.Refresh();
        }

        private void run()
        {

            listBox1.Items.Clear();
            dataGridView1.Rows.Clear();


            double volume = 0;
            int total = 0;

            button1.Enabled = false;
            log("Start Auditoria...");
            //Pegando dados da Binance de 1h em 1h limite operacional da  api da mesma
            log("Get API Anubis...");
            String jsonAnubis = getCache(txtApiAnubisTrade.Text);
            Newtonsoft.Json.Linq.JContainer jAnubis = (Newtonsoft.Json.Linq.JContainer)JsonConvert.DeserializeObject(jsonAnubis);

            System.Data.DataTable dt = new DataTable();
            dt.Columns.Add("id");
            dt.Columns.Add("orderId");
            dt.Columns.Add("price");
            dt.Columns.Add("volume");
            dt.Columns.Add("timestamp");
            dt.Columns.Add("date");
            dt.Columns.Add("isbuyer");
            dt.Columns.Add("commission");
            dt.Columns.Add("commissionAsset");

            TimeSpan ts = DateTime.Parse(txtEnd.Text) - DateTime.Parse(txtStart.Text);
            for (int i = 0; i < Math.Round(ts.TotalHours); i++)
            {

                

                double start = 0;
                double end = 0;
                start = DatetimeToUnix(DateTime.Parse(txtStart.Text).AddHours(i));
                end = DatetimeToUnix(DateTime.Parse(txtStart.Text).AddHours(i + 1));
                log("Get Binance item " + i.ToString() + " of " + Math.Round(ts.TotalHours) + " (" + UnixTimeStampToDateTime( start) + " at " + UnixTimeStampToDateTime(end )+ ") ...");

                if (start > DatetimeToUnix(DateTime.UtcNow))
                    break;

                String jsonBinance = getCache(txtApiBinance.Text.Replace("@start", start.ToString()).Replace("@end", end.ToString()));
                Newtonsoft.Json.Linq.JContainer jBinance = (Newtonsoft.Json.Linq.JContainer)JsonConvert.DeserializeObject(jsonBinance);


                log("Verify trades...");
                foreach (var itemAnubis in jAnubis)
                {
                    if (long.Parse(itemAnubis["time"].ToString()) >= start && long.Parse(itemAnubis["time"].ToString()) <= end)
                    {
                        foreach (var item in jBinance)
                        {
                            if ((long.Parse(itemAnubis["id"].ToString()) >= long.Parse(item["f"].ToString()) &&
                                long.Parse(itemAnubis["id"].ToString()) <= long.Parse(item["l"].ToString()))

                                )
                            {
                                dt.Rows.Add(itemAnubis["id"].ToString(), itemAnubis["orderId"].ToString(), item["p"], itemAnubis["qty"], itemAnubis["time"], UnixTimeStampToDateTime(double.Parse(itemAnubis["time"].ToString())), itemAnubis["isBuyer"],item["commission"],item["commissionAsset"]);
                                dataGridView1.DataSource = dt;
                                
                                total++;
                                volume += double.Parse(itemAnubis["qty"].ToString().Replace(".",","));

                                lblTotal.Text = total.ToString();
                                lblVolume.Text = volume.ToString() + " BTC";

                                log("Trade "+ itemAnubis["id"].ToString() + " found! OrderId: " + itemAnubis["orderId"].ToString());
                            }
                        }
                    }
                }

                log("Wait 1s...");
                System.Threading.Thread.Sleep(1000);
            }

            log("end!");
            button1.Enabled = true;
            MessageBox.Show("End!");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtStart.Text = DateTime.UtcNow.ToString("yyyy-MM-dd") + " 00:00:00";
            txtEnd.Text = DateTime.UtcNow.ToString("yyyy-MM-dd") + " 23:59:59";
        }


        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToUniversalTime();
            return dtDateTime;
        }

        public static Double DatetimeToUnix(DateTime date)
        {
            return (date.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds;
        }
    }
}
