using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using CapitalOne;

namespace CapitalOne
{
    class Program
    {
        
        //Implements the functionality to call the GetAllTransactions API.

        //1) Loads a user's transactions from the GetAllTransactions endpoint
        //2) Determines how much money the user spends and makes in each of the months for which we have data, and in the "average" month.
        //3) Output the numbers in a specified format.
        //4) Support -ignore-donuts switch to exclude donut transactions.
        //5) Support -ignore-cc-payments switch to exclude credit card payoff transactions.

        static void Main(string[] args)
        {
            bool ignore_donut_payments = Array.IndexOf(args, "-ignore-donuts") >= 0;
            bool ignore_cc_payments = Array.IndexOf(args, "-ignore-cc-payments") >= 0;

            try
            {
                const string parameters = "{\"args\": {\"uid\":1110590645,\"token\":\"D2C8B9EB0EF8611AD3AEAB642E1560C7\",\"api-token\":\"AppTokenForInterview\",\"json-strict-mode\":false,\"json-verbose-response\":false}}";
                const string url = "https://2016.api.levelmoney.com/api/v2/core/get-all-transactions";
                const string user_name = "interview@levelmoney.com";
                const string password = "password2";
                string raw_json = null;
                
                ILMOperations lm_operations = new LMTransactions();

                lm_operations.Get_Raw_Transactions_Json(url, parameters, user_name, password, out raw_json);

                List<Transaction> transactions = null;
                lm_operations.Get_All_Transactions(raw_json, "", out transactions);

                List<Tuple<Transaction,Transaction>> cc_payments = null;
                if(ignore_cc_payments)
                {
                    lm_operations.Remove_CreditCard_Payment_Entries(transactions, out cc_payments);
                }

                if(ignore_donut_payments)
                {
                    lm_operations.Remove_Donut_Entries(transactions);
                }

                List<Grouped_Transaction> grouped_transactions = null;

                lm_operations.Group_All_Transactions_By_Month_Year(transactions, out grouped_transactions);

                string grouped_json = null;
                lm_operations.Get_Grouped_Transactions_Json(grouped_transactions, out grouped_json);

                Console.WriteLine(grouped_json);

                if(ignore_cc_payments)
                {
                    Console.WriteLine("\nThe following credit card payment transactions have been excluded:\n");

                    foreach (Tuple<Transaction, Transaction> tuple in cc_payments)
                    {
                        Console.WriteLine(tuple.Item1.transaction_time + "," + tuple.Item1.amount.ToString());
                        Console.WriteLine(tuple.Item2.transaction_time + "," + tuple.Item2.amount.ToString());

                        Console.WriteLine("\n");
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine("Unhandled generic exception:" + e.Message);
            }

            Console.ReadKey();                               
        }
    }
}