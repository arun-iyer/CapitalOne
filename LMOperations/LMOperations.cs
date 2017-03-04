using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace CapitalOne
{
    public class LMTransactions : ILMOperations
    {
        //This method makes the post call and retreives the raw JSON string.
        public void Get_Raw_Transactions_Json(string url, string args, string username, string password, out string raw_json)
        {
            StringBuilder response = new StringBuilder();
            HttpWebRequest http_web_request = null;
            HttpWebResponse http_web_response = null;

            //need to explictily set this since the LM servers are on TLS 1.1
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;

            http_web_request = (HttpWebRequest)WebRequest.Create(url);
            http_web_request.Credentials = new NetworkCredential(username, password);

            try
            {
                byte[] bytes;
                System.Text.UTF8Encoding encoding = new System.Text.UTF8Encoding();
                bytes = encoding.GetBytes(args);

                http_web_request.Method = "POST";
                http_web_request.ContentType = "application/json";
                http_web_request.Accept = "application/json";
                http_web_request.ContentLength = bytes.Length;

                //write the arguments as raw bytes into the request stream, this will get to the server on the POST.
                using (Stream requestStream = http_web_request.GetRequestStream())
                {
                    requestStream.Write(bytes, 0, bytes.Length);
                    requestStream.Close();
                }

                http_web_response = (HttpWebResponse)http_web_request.GetResponse();

                if (http_web_response.StatusCode == HttpStatusCode.OK)
                {
                    //Get response stream into StreamReader
                    using (Stream responseStream = http_web_response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                            response.Append(reader.ReadToEnd());
                    }
                }
                http_web_response.Close();
            }
            catch (WebException we)
            {
                throw new Exception(we.Message);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
            finally
            {
                http_web_response.Close();
                http_web_response = null;
                http_web_request = null;
            }

            Extract_Raw_Json(response, out raw_json);
        }

        //deserializes the raw json string to transaction objects.
        public void Get_All_Transactions(string raw_json, string account_id, out List<Transaction> transactions)
        {
            transactions = JsonConvert.DeserializeObject<List<Transaction>>(raw_json);

            //store the debit and credit information at this point.
            //we would need that when doing the grouping by month/year.
            const double cento_cents_div = 10000;
            foreach (Transaction tx in transactions)
            {
                if (tx.amount < 0)
                    tx.debit = tx.amount / cento_cents_div;
                else
                    tx.credit = tx.amount / cento_cents_div;
            }            
        }

        //walk through the list and check if there are entires of amount with opposing sign within 24 hours.
        //collect them in tupe and remove from transaction list.
        //this will not be O(N*N) the breaks will get called much before the inner loop goes through all its elements.
        public void Remove_CreditCard_Payment_Entries(List<Transaction> transactions, out List<Tuple<Transaction,Transaction>> duplicate_list)
        {
            duplicate_list = new List<Tuple<Transaction, Transaction>>();
            
            //find the duplicate cc transaction items and "mark" them.
            foreach (Transaction tx1 in transactions)
            {
                DateTime dt1 = DateTime.Parse(tx1.transaction_time);
                foreach (Transaction tx2 in transactions)
                {
                    DateTime dt2 = DateTime.Parse(tx2.transaction_time);
                    var hours = (dt2 - dt1).TotalHours;

                    if(hours > 24.0 || tx2.mark == true)
                    {
                        break;
                    }
                    else if(tx1.amount == -tx2.amount)
                    {
                        //this is a cc transaction. mark it and collect it.
                        tx1.mark = true;
                        tx2.mark = true;
                        duplicate_list.Add(new Tuple<Transaction, Transaction>(tx1, tx2));
                        break;
                    }
                }
            }

            //remove the marked ones from list.
            transactions.RemoveAll(t => t.mark == true);
        }

        //Merchant field for donut payments that says Krispy Kreme Donuts or DUNKIN #336784 are taken off.
        public void Remove_Donut_Entries(List<Transaction> transactions)
        {
            transactions.RemoveAll(x => x.merchant == "Krispy Kreme Donuts");
            transactions.RemoveAll(x => x.merchant == "DUNKIN #336784");
        }

        //group the entire tranaction list by month/year and get the monthly credit / debit
        public void Group_All_Transactions_By_Month_Year(List<Transaction> transactions, out List<Grouped_Transaction> grouped_transactions)
        {
            var group = from t in transactions
                    where t.is_pending == false
                    group t by new { DateTime.Parse(t.transaction_time).Year, DateTime.Parse(t.transaction_time).Month }
                        into g
                        select new
                        {
                            year = g.Key.Year,
                            month = g.Key.Month,
                            total_debit = g.Sum(t => t.debit),
                            total_credit = g.Sum(t => t.credit)
                        };

            grouped_transactions = new List<Grouped_Transaction>();
            foreach (var c in group)
            {
                Append_Grouped_Transaction(ref grouped_transactions, c.month, c.year, c.total_credit, c.total_debit);
            }
        }

        //this method formats the output Json string based on the transactions grouped by month/year.
        public void Get_Grouped_Transactions_Json(List<Grouped_Transaction> grouped_transactions, out string grouped_json)
        {
            StringBuilder sb = new StringBuilder();

            //Bail out early. If list is empty return empty string.
            if (grouped_transactions.Count == 0)
            {
                grouped_json = "";
                return;
            }

            //build the output JSON based upon the requirements.
            //the format for each line similar to --> "2014-10": {"spent": "$200.00", "income": "$500.00"},
            //the JSON serializer/deserializer library that I am using does not have functionality to 
            //customize the fields to the level that is required so I wrote this.
            sb.Append("{");
            foreach (Grouped_Transaction gt in grouped_transactions)
            {
                //format for each line.
                //"2014-10": {"spent": "$200.00", "income": "$500.00"},
                double monthly_credit = Math.Round(gt.monthly_credit, 2);
                double monthly_debit = Math.Round(gt.monthly_debit, 2);
                sb.Append("\"" + gt.year_month + "\":" + "{\"spent\":\"$" + monthly_debit.ToString() + "\",\"income\":" + "\"$" + monthly_credit.ToString() + "\"}");

                //if it's the very last transaction, append the average month.
                if(grouped_transactions.IndexOf(gt) == grouped_transactions.Count - 1)
                {
                    sb.Append(",\n");
                    //append the averge month.
                    Grouped_Transaction average_month = GetAverageMonth(grouped_transactions);
                    double avg_monthly_credit = Math.Round(average_month.monthly_credit, 2);
                    double avg_monthly_debit = Math.Round(average_month.monthly_debit, 2);
                    sb.Append("\"" + average_month.year_month + "\":" + "{\"spent\":\"$" + avg_monthly_debit.ToString() + "\",\"income\":" + "\"$" + avg_monthly_credit.ToString() + "\"}");

                    sb.Append("}");
                }
                else
                {
                    sb.Append(",\n");                    
                }
            }            
            grouped_json = sb.ToString();
        }

        //I defined average month as the spending month that had the closest debit (spending) when compared to the 
        //avegare over the entire transaction hsitory.
        private Grouped_Transaction GetAverageMonth(List<Grouped_Transaction> grouped_transactions)
        {
            double average = 0.0;
            foreach (Grouped_Transaction gt in grouped_transactions)
            {
                average += gt.monthly_debit;
            }
            average = average / grouped_transactions.Count();

            return grouped_transactions.Where(numbers => numbers.monthly_debit > average).First();
        }

        //helper to reduce duplicate code and make sure month is in MM format.
        private void Append_Grouped_Transaction(ref List<Grouped_Transaction> list, int month, int year, double total_credit, double total_debit)
        {
            Grouped_Transaction gt = new Grouped_Transaction();
            //ensure month is always MM
            string str_month = month.ToString();
            str_month = str_month.PadLeft(2, '0');
            gt.year_month = year.ToString() + "-" + str_month.ToString();

            gt.monthly_credit = total_credit;
            gt.monthly_debit = Math.Abs(total_debit);

            list.Add(gt);
        }

        //helper, clean the string builder to extract only the serialized json objects.
        private void Extract_Raw_Json(StringBuilder sb, out string extracted_string)
        {
            int index_bracket = sb.ToString().IndexOf("[");
            sb.Remove(0, index_bracket);
            sb.Remove(sb.Length - 1, 1);
            extracted_string = sb.ToString();
        }
    }
}
