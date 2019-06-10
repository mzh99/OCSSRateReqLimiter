using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OCSS.Web.RateReqLimiter;

namespace OCSSRateReqLimiter.Tests {

   /// <summary>Integration test for CSV loader</summary>
   [TestClass]
   public class TestCsvLoader {

      private readonly string dataFolder;

      public TestCsvLoader() {
         dataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData");
      }

      private string GetTestDataFile(string basename) {
         return Path.Combine(dataFolder, basename);
      }

      [TestMethod]
      public void LoadRulesSucceeds() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv1.txt"), GetTestDataFile("whitelist1.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
         Assert.AreEqual(10, rules.Length, "Not 10 rules");
         var whitelist = loader.LoadWhitelist().ToArray();
         Assert.AreEqual(2, whitelist.Length, "Not 2 rules");
      }

      [TestMethod]
      public void LoadRulesIgnoresBlankLines() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv1-blanklines.txt"), GetTestDataFile("whitelist1-blanklines.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
         Assert.AreEqual(10, rules.Length, "Not 10 rules");
         var whitelist = loader.LoadWhitelist().ToArray();
         Assert.AreEqual(2, whitelist.Length, "Not 2 rules");
      }

      [TestMethod]
      public void LoadRulesIgnoresWhitespace() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv1-whitespace.txt"), GetTestDataFile("whitelist1-whitespace.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
         Assert.AreEqual(10, rules.Length, "Not 10 rules");
         var whitelist = loader.LoadWhitelist().ToArray();
         Assert.AreEqual(2, whitelist.Length, "Not 2 rules");
         Assert.AreEqual("192.168.1.1", whitelist[0]);
      }

      [TestMethod]
      public void LoadRulesCaseIsUnimportant() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv1-case.txt"), GetTestDataFile("whitelist1-case.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
         Assert.AreEqual(10, rules.Length, "Not 10 rules");
         Assert.AreEqual(HttpVerb.Post, rules[0].Method, "Rule[0] method not Post");
      }

      [TestMethod]
      public void LoadRulesMissingPerUnitsAreZero() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv2.txt"), GetTestDataFile("whitelist1.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
         Assert.AreEqual(3, rules.Length, "Not 3 rules");
         Assert.AreEqual(0, rules[0].RequestsPerMinute, "Rule[0] per min not 0");
         Assert.AreEqual(30, rules[0].RequestsPerHour, "Rule[0] per hour not 30");
         Assert.AreEqual(100, rules[0].RequestsPerDay, "Rule[0] per day not 100");

         Assert.AreEqual(2, rules[1].RequestsPerMinute, "Rule[1] per min not 2");
         Assert.AreEqual(0, rules[1].RequestsPerHour, "Rule[1] per hour not 0");
         Assert.AreEqual(50, rules[1].RequestsPerDay, "Rule[1] per day not 50");

         Assert.AreEqual(2, rules[2].RequestsPerMinute, "Rule[2] per min not 2");
         Assert.AreEqual(30, rules[2].RequestsPerHour, "Rule[2] per hour not 30");
         Assert.AreEqual(0, rules[2].RequestsPerDay, "Rule[2] per day not 0");
      }

      [TestMethod]
      public void LoadRulesCommentsAtEndAreOkay() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv3.txt"), GetTestDataFile("whitelist3.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
      }

      [TestMethod]
      [ExpectedException(typeof(ArgumentException), "too few columns did not raise exception")]
      public void LoadRulesWithTooFewColumnsRaisesArgumentException() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv4.txt"), GetTestDataFile("whitelist1.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
      }

      [TestMethod]
      [ExpectedException(typeof(ArgumentException), "bad method did not raise exception")]
      public void LoadRulesWithBadMethodRaisesArgumentException() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv5.txt"), GetTestDataFile("whitelist1.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
      }

      [TestMethod]
      [ExpectedException(typeof(ArgumentException), "Empty path did not raise exception")]
      public void LoadRulesWithEmptyPathRaisesArgumentException() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv6.txt"), GetTestDataFile("whitelist1.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
      }

      [TestMethod]
      [ExpectedException(typeof(ArgumentException), "Invalid integer did not raise exception")]
      public void LoadRulesWithInvalidIntegerForPerUnitRequestsRaisesArgumentException() {
         var loader = new CSVConfigLoader(GetTestDataFile("csv7.txt"), GetTestDataFile("whitelist1.txt"));
         var rules = loader.LoadRateLimitRules().ToArray();
      }

   }

}
