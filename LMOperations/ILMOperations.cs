using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CapitalOne
{
    //JSON serializable data class that holds information of a transaction
    //the debit and credit properties are not needed while serializing
    //and deserializing, they are used to store the amount 
    //as a debit or credit which is then used for LINQ queries.
    public class Transaction
    {
        [JsonProperty("amount")]
        public Int64 amount { get; set; }

        [JsonProperty("is-pending")]
        public bool is_pending { get; set; }

        [JsonProperty("aggregation-time")]
        public string aggregation_time { get; set; }

        [JsonProperty("account-id")]
        public string account_id { get; set; }

        [JsonProperty("clear-date")]
        public Int64 clear_date { get; set; }

        [JsonProperty("transaction-id")]
        public string transaction_id { get; set; }

        [JsonProperty("raw-merchant")]
        public string raw_merchant { get; set; }

        [JsonProperty("categorization")]
        public string categorization { get; set; }

        [JsonProperty("merchant")]
        public string merchant { get; set; }

        [JsonProperty("transaction-time")]
        public string transaction_time { get; set; }

        [JsonIgnore]
        public double debit { get; set; }

        [JsonIgnore]
        public double credit { get; set; }

        [JsonIgnore]
        public bool mark { get; set; }
    };

    //This class is a bit of a helper to store the monthly data.
    public class Grouped_Transaction
    {        
        public string year_month { get; set; }
        public double monthly_credit { get; set; }
        public double monthly_debit { get; set; }
    };

    //core interface that defines all operations from making the POST call to formatting the
    //final output.
    public interface ILMOperations
    {
        //retreive the raw JSON string.
        void Get_Raw_Transactions_Json(string url, string args, string username, string password, out string raw_json);
        
        //every transaction in the system.
        void Get_All_Transactions(string raw_json, string account_id, out List<Transaction> transactions);

        //cc payments show up as the same amount debit and credit within 24 hours. This method takes them off and returns them in a tuple list.
        void Remove_CreditCard_Payment_Entries(List<Transaction> transactions, out List<Tuple<Transaction, Transaction>> duplicate_list);

        //Merchant field for donut payments taken off.
        void Remove_Donut_Entries(List<Transaction> transactions);

        //transactions grouped by month/year. 
        void Group_All_Transactions_By_Month_Year(List<Transaction> transactions, out List<Grouped_Transaction> grouped_transactions);

        //construct the final output 
        void Get_Grouped_Transactions_Json(List<Grouped_Transaction> grouped_transactions, out string grouped_json);
    }
}
