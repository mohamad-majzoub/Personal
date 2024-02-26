using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Net;
using System.IO;
using System.Data.SQLite;
using Newtonsoft.Json.Linq;
using System.Data;

namespace BitcoinAPI
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    public class Service1 : IService1
    {
        public string GetData(int value)
        {
            return string.Format("You entered: {0}", value);
        }

        public CompositeType GetDataUsingDataContract(CompositeType composite)
        {
            if (composite == null)
            {
                throw new ArgumentNullException("composite");
            }
            if (composite.BoolValue)
            {
                composite.StringValue += "Suffix";
            }
            return composite;
        }

        public string bitcoinprice_Bitstamp()
        {
            try
            {
                HttpWebRequest request;
                HttpWebResponse response = null/* TODO Change to default(_) if this is not a reference type */;
                StreamReader reader;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                request = (HttpWebRequest)WebRequest.Create("https://www.bitstamp.net/api/v2/ticker/btcusd/");

                response = (HttpWebResponse)request.GetResponse();
                reader = new StreamReader(response.GetResponseStream());

                string rawresp;
                string price = "";
                rawresp = reader.ReadToEnd();

                //JArray array = JArray.Parse(rawresp);
                JObject item = JObject.Parse(rawresp);
                price = item["last"] == null ? "" : item["last"].ToString();

                return price;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "An exception: " + ex.Message;
            }
        }

        public string bitcoinprice_Bitfinex()
        {
            try
            {
                HttpWebRequest request;
                HttpWebResponse response = null/* TODO Change to default(_) if this is not a reference type */;
                StreamReader reader;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                request = (HttpWebRequest)WebRequest.Create("https://api.bitfinex.com/v1/pubticker/btcusd/");

                response = (HttpWebResponse)request.GetResponse();
                reader = new StreamReader(response.GetResponseStream());

                string rawresp;
                string price = "";
                rawresp = reader.ReadToEnd();

                JObject item = JObject.Parse(rawresp);
                price = item["last_price"] == null ? "" : item["last_price"].ToString();

                return price;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return "An exception: " + ex.Message;
            }
        }

        public string getAndSaveBtcPrice(string exchangeName)
        {
            string btcPrice = "";
            string cs = @"URI=file:.\test.db";

            if (exchangeName != "bitstamp" && exchangeName != "bitfinex")
                return btcPrice;

            var con = new SQLiteConnection(cs);
            con.Open();

            var cmd = new SQLiteCommand(con);

            cmd.CommandText = @"CREATE TABLE IF NOT EXISTS btcPrices(id INTEGER PRIMARY KEY,
            curDate DATETIME, curTime TIME, price TEXT, exchange TEXT)";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "INSERT INTO btcPrices(curDate, curTime, price, exchange) VALUES(@cDate, @cTime, @price, @exchange)";

            cmd.Parameters.AddWithValue("@cDate", DateTime.Now);
            cmd.Parameters.AddWithValue("@cTime", DateTime.Now);
            if (exchangeName == "bitstamp")
            {
                btcPrice = bitcoinprice_Bitstamp();
                cmd.Parameters.AddWithValue("@price", btcPrice);
            }
            else if (exchangeName == "bitfinex")
            {
                btcPrice = bitcoinprice_Bitfinex();
                cmd.Parameters.AddWithValue("@price", btcPrice);
            }
            cmd.Parameters.AddWithValue("@exchange", exchangeName);

            cmd.Prepare();

            cmd.ExecuteNonQuery();

            return btcPrice;
        }

        public DataSet getBtcPriceHistory(string exchangeName)
        {
            // Create a dataset to hold the result
            DataSet dataSet = new DataSet();
            string cs = @"URI=file:.\test.db";

            if (exchangeName != "bitstamp" && exchangeName != "bitfinex")
                return dataSet;

            string stm = "select strftime('%Y-%m-%d', curDate) priceDate, strftime('%H:%M:%S', curTime) priceTime, " +
                "price from btcPrices where exchange = '" + exchangeName + "'";

            var con = new SQLiteConnection(cs);
            con.Open();

            using (SQLiteCommand command = new SQLiteCommand(stm, con))
            {
                // Create a data adapter to fill the dataset
                using (SQLiteDataAdapter dataAdapter = new SQLiteDataAdapter(command))
                {
                    // Fill the dataset with the results of the query
                    dataAdapter.Fill(dataSet);
                }
            }
            return dataSet;
        }

    }
}
