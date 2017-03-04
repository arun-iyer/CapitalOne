using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using CapitalOne;

namespace LM_Tests
{
    [TestClass]
    public class LMOperationTests
    {
        private void LoadJSON()
        {
            try
            {
                //Load the raw json when the class intializes.
                const string parameters = "{\"args\": {\"uid\":1110590645,\"token\":\"D2C8B9EB0EF8611AD3AEAB642E1560C7\",\"api-token\":\"AppTokenForInterview\",\"json-strict-mode\":false,\"json-verbose-response\":false}}";
                const string url = "https://2016.api.levelmoney.com/api/v2/core/get-all-transactions";
                const string user_name = "interview@levelmoney.com";
                const string password = "password2";

                lm_operations = new LMTransactions();
                lm_operations.Get_Raw_Transactions_Json(url, parameters, user_name, password, out raw_json);
            }
            catch(Exception)
            {
                Assert.Fail("Could not retreive json on POST message");
            }
        }

        [TestMethod]
        public void CheckRawJSONForLength()
        {        
            LoadJSON();
            //ensure the loaded raw string has some length greater than the minimum we expect.
            Assert.IsTrue(raw_json.Length >= 405265);
        }

        [TestMethod]
        public void EnusreMinimumTransactionCount()
        {
            LoadJSON();

            List<Transaction> transactions = null;
            lm_operations.Get_All_Transactions(raw_json, "", out transactions);

            //There are at-least 1205 transactions that are committed and set in stone.
            //we should not see a number lesser than that.
            //If that happens then we have lost historical customer data or the
            //Get_All_Transactions method failed to retreive the right amount of transactions
            //both of which are bad.
            Assert.IsTrue(transactions.Count >= 1205);
        }

        [TestMethod]
        public void EnusreDebitAndCreditAreMutuallyExclusive()
        {
            LoadJSON();

            //We should never end up with both debit and credit.
            //A transaction is either a debit or a credit.
            List<Transaction> transactions = null;
            lm_operations.Get_All_Transactions(raw_json, "", out transactions);

            foreach (Transaction tx in transactions)
            {
                Assert.IsFalse(tx.credit > 0.0 && tx.debit > 0.0);
            }
        }

        [TestMethod]
        public void EnusreDebitAndCreditValuesAreCorrect()
        {
            LoadJSON();

            //We should never end up with both debit and credit.
            //A transaction is either a debit or a credit.
            List<Transaction> transactions = null;
            lm_operations.Get_All_Transactions(raw_json, "", out transactions);
            const double cento_cents_div = 10000;

            foreach (Transaction tx in transactions)
            {
                if(tx.amount < 0)
                {
                    Assert.IsTrue(tx.debit == tx.amount/cento_cents_div);
                    Assert.IsTrue(tx.credit == 0);
                }
                else
                {
                    Assert.IsTrue(tx.credit == tx.amount/cento_cents_div);
                    Assert.IsTrue(tx.debit == 0);            
                }
            }
        }

        [TestMethod]
        public void EnusreYearAndMonthAreValid()
        {
            LoadJSON();

            List<Transaction> transactions = null;
            List<Grouped_Transaction> grouped_transactions = null;
            
            lm_operations.Get_All_Transactions(raw_json, "", out transactions);
            
            lm_operations.Group_All_Transactions_By_Month_Year(transactions, out grouped_transactions);

            try
            {
                foreach (Grouped_Transaction gt in grouped_transactions)
                {
                    int year = Convert.ToInt32(gt.year_month.Substring(0, 4));
                    Assert.IsTrue(year <= DateTime.Now.Date.Year);

                    int month = Convert.ToInt32(gt.year_month.Substring(5, 2));
                    Assert.IsTrue(month >= 1 && month <= 12);
                }
            }
            catch (System.ArgumentOutOfRangeException)
            {
                //we have a bad year/month field
                Assert.Fail("Bad year / month field");
            }
        }

        [TestMethod]
        public void EnusrePriorMonthNumbersAreOK_No_Donuts()
        {
            //totals for prior months are set in stone, they should never change.
            LoadJSON();

            List<Transaction> transactions = null;
            List<Grouped_Transaction> grouped_transactions = null;

            lm_operations.Get_All_Transactions(raw_json, "", out transactions);

            lm_operations.Remove_Donut_Entries(transactions);

            lm_operations.Group_All_Transactions_By_Month_Year(transactions, out grouped_transactions);

            string output = null;
            lm_operations.Get_Grouped_Transactions_Json(grouped_transactions, out output);

            //the first 1602 characters (till 3/3/2017) of the output should be exactly the same as this.
            string result_without_donuts = "{\"2014-10\":{\"spent\":\"$1494.16\",\"income\":\"$3429.79\"},\n\"2014-11\":{\"spent\":\"$4639.91\",\"income\":\"$3949.51\"},\n\"2014-12\":{\"spent\":\"$4641.52\",\"income\":\"$3954.32\"},\n\"2015-01\":{\"spent\":\"$3756.73\",\"income\":\"$3925.98\"},\n\"2015-02\":{\"spent\":\"$4159.2\",\"income\":\"$3936.64\"},\n\"2015-03\":{\"spent\":\"$3392.36\",\"income\":\"$3942.73\"},\n\"2015-04\":{\"spent\":\"$2939.79\",\"income\":\"$3943.68\"},\n\"2015-05\":{\"spent\":\"$2682.31\",\"income\":\"$3416.4\"},\n\"2015-06\":{\"spent\":\"$3880.86\",\"income\":\"$3918.23\"},\n\"2015-07\":{\"spent\":\"$3871.91\",\"income\":\"$3917.18\"},\n\"2015-08\":{\"spent\":\"$2645.29\",\"income\":\"$3384.88\"},\n\"2015-09\":{\"spent\":\"$3086.4\",\"income\":\"$3922.72\"},\n\"2015-10\":{\"spent\":\"$3299.23\",\"income\":\"$1717.43\"},\n\"2015-11\":{\"spent\":\"$3087.27\",\"income\":\"$3977.78\"},\n\"2015-12\":{\"spent\":\"$3200.71\",\"income\":\"$1725.57\"},\n\"2016-01\":{\"spent\":\"$2758.53\",\"income\":\"$2242.89\"},\n\"2016-02\":{\"spent\":\"$3060.71\",\"income\":\"$3451.38\"},\n\"2016-03\":{\"spent\":\"$4214.72\",\"income\":\"$3386.61\"},\n\"2016-04\":{\"spent\":\"$3891.58\",\"income\":\"$3917.4\"},\n\"2016-05\":{\"spent\":\"$3508.83\",\"income\":\"$3991.18\"},\n\"2016-06\":{\"spent\":\"$3218.59\",\"income\":\"$3927.78\"},\n\"2016-07\":{\"spent\":\"$3202.71\",\"income\":\"$3928.23\"},\n\"2016-08\":{\"spent\":\"$3518.64\",\"income\":\"$3922.33\"},\n\"2016-09\":{\"spent\":\"$3173.79\",\"income\":\"$2229.63\"},\n\"2016-10\":{\"spent\":\"$3252.29\",\"income\":\"$2209.91\"},\n\"2016-11\":{\"spent\":\"$4825.59\",\"income\":\"$3441.83\"},\n\"2016-12\":{\"spent\":\"$3039.32\",\"income\":\"$3966.25\"},\n\"2017-01\":{\"spent\":\"$4027.46\",\"income\":\"$3486.48\"},\n\"2017-02\":{\"spent\":\"$3471.02\",\"income\":\"$1707.68\"},\n\"2017-03\":{\"spent\":\"$2245.11\",\"income\":\"$0\"},\n\"2014-11\":{\"spent\":\"$4639.91\",\"income\":\"$3949.51\"}}";

            //extract the first 1602 chars they should match.
            output = output.Substring(0,1602);

            Assert.AreEqual(result_without_donuts,output);
        }

        [TestMethod]
        public void EnusrePriorMonthNumbersAreOK()
        {
            //totals for prior months are set in stone, they should never change.
            LoadJSON();

            List<Transaction> transactions = null;
            List<Grouped_Transaction> grouped_transactions = null;

            lm_operations.Get_All_Transactions(raw_json, "", out transactions);

            lm_operations.Group_All_Transactions_By_Month_Year(transactions, out grouped_transactions);

            string output = null;
            lm_operations.Get_Grouped_Transactions_Json(grouped_transactions, out output);

            //the first 1602 characters (till 3/3/2017) of the output should be exactly the same as this.
            string result_without_donuts = "{\"2014-10\":{\"spent\":\"$1578.44\",\"income\":\"$3429.79\"},\n\"2014-11\":{\"spent\":\"$4674.47\",\"income\":\"$3949.51\"},\n\"2014-12\":{\"spent\":\"$4736.98\",\"income\":\"$3954.32\"},\n\"2015-01\":{\"spent\":\"$3811.42\",\"income\":\"$3925.98\"},\n\"2015-02\":{\"spent\":\"$4217.99\",\"income\":\"$3936.64\"},\n\"2015-03\":{\"spent\":\"$3460.17\",\"income\":\"$3942.73\"},\n\"2015-04\":{\"spent\":\"$2985.45\",\"income\":\"$3943.68\"},\n\"2015-05\":{\"spent\":\"$2704.42\",\"income\":\"$3416.4\"},\n\"2015-06\":{\"spent\":\"$3915.03\",\"income\":\"$3918.23\"},\n\"2015-07\":{\"spent\":\"$3894.82\",\"income\":\"$3917.18\"},\n\"2015-08\":{\"spent\":\"$2666.93\",\"income\":\"$3384.88\"},\n\"2015-09\":{\"spent\":\"$3105.88\",\"income\":\"$3922.72\"},\n\"2015-10\":{\"spent\":\"$3299.23\",\"income\":\"$1717.43\"},\n\"2015-11\":{\"spent\":\"$3106.28\",\"income\":\"$3977.78\"},\n\"2015-12\":{\"spent\":\"$3236.09\",\"income\":\"$1725.57\"},\n\"2016-01\":{\"spent\":\"$2828.18\",\"income\":\"$2242.89\"},\n\"2016-02\":{\"spent\":\"$3096.93\",\"income\":\"$3451.38\"},\n\"2016-03\":{\"spent\":\"$4223.42\",\"income\":\"$3386.61\"},\n\"2016-04\":{\"spent\":\"$3907.8\",\"income\":\"$3917.4\"},\n\"2016-05\":{\"spent\":\"$3538.46\",\"income\":\"$3991.18\"},\n\"2016-06\":{\"spent\":\"$3272.81\",\"income\":\"$3927.78\"},\n\"2016-07\":{\"spent\":\"$3262.42\",\"income\":\"$3928.23\"},\n\"2016-08\":{\"spent\":\"$3575.89\",\"income\":\"$3922.33\"},\n\"2016-09\":{\"spent\":\"$3216.84\",\"income\":\"$2229.63\"},\n\"2016-10\":{\"spent\":\"$3285.59\",\"income\":\"$2209.91\"},\n\"2016-11\":{\"spent\":\"$4858.45\",\"income\":\"$3441.83\"},\n\"2016-12\":{\"spent\":\"$3075.1\",\"income\":\"$3966.25\"},\n\"2017-01\":{\"spent\":\"$4111.48\",\"income\":\"$3486.48\"},\n\"2017-02\":{\"spent\":\"$3486.14\",\"income\":\"$1707.68\"},\n\"2017-03\":{\"spent\":\"$2245.11\",\"income\":\"$0\"},\n\"2014-11\":{\"spent\":\"$4674.47\",\"income\":\"$3949.51\"}}";

            //extract the first 1602 chars they should match.
            output = output.Substring(0, 1602);

            Assert.AreEqual(result_without_donuts, output);
        }

        [TestMethod]
        public void CheckCCPaymentsRemoval()
        {
            LoadJSON();

            List<Transaction> transactions = null;

            lm_operations.Get_All_Transactions(raw_json, "", out transactions);

            List<Tuple<Transaction,Transaction>> cc_payments = null;
            lm_operations.Remove_CreditCard_Payment_Entries(transactions, out cc_payments);

            //therer is already a cc payment in the tranactions.
            Assert.IsTrue(cc_payments.Count > 0);

            foreach(Tuple<Transaction,Transaction> tuple in cc_payments)
            {
                Assert.IsTrue(tuple.Item1.merchant == "Credit Card Payment");
                Assert.IsTrue(tuple.Item2.merchant == "CC Payment");

                Assert.IsTrue(tuple.Item2.amount == -tuple.Item1.amount);
            }

            foreach (Transaction tx in transactions)
            {
                if (tx.merchant == "Credit card payment" || tx.merchant == "CC Payment")
                {
                    //any remiaing cc entries are payments and must be debit. 
                    Assert.IsTrue(tx.amount < 0);
                }
            }
        }

        private string raw_json = "";
        private ILMOperations lm_operations = null;
    }
}