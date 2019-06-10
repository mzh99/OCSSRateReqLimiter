using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OCSS.Web.RateReqLimiter;

namespace OCSSRateReqLimiter.Tests {

   [TestClass]
   public class TestRateLimitRule {

      [TestMethod]
      public void AssignmentsSucceed() {
         var rule = new RateLimitRule(HttpVerb.Post, @"/Dummy", 1, 2, 3);
         Assert.AreEqual(HttpVerb.Post, rule.Method, "verb not post");
         Assert.AreEqual(@"/Dummy", rule.PagePath, "path not /Dummy");
         Assert.AreEqual(1, rule.RequestsPerMinute, "Req per-minute not 1");
         Assert.AreEqual(2, rule.RequestsPerHour, "Req per-hour not 2");
         Assert.AreEqual(3, rule.RequestsPerDay, "Req per-day not 3");
         Assert.AreEqual("Post", rule.MethodAsString, "method string not post");
      }

      [TestMethod]
      public void TimeUnitStringIsCorrect() {
         var rule = new RateLimitRule(HttpVerb.Post, @"/Dummy", 1, 2, 3);
         Assert.AreEqual("minute", rule.GetTimeUnitName(0), "gettimeunitname(0) not minute");
         Assert.AreEqual("hour", rule.GetTimeUnitName(1), "gettimeunitname(1) not hour");
         Assert.AreEqual("day", rule.GetTimeUnitName(2), "gettimeunitname(2) not day");
      }

      [TestMethod]
      public void RuleKeyGenIsCorrect() {
         var rule = new RateLimitRule(HttpVerb.Post, @"/Dummy", 1, 2, 3);
         Assert.AreEqual("post-/dummy", rule.RuleKey, "Rule key incorrect");
      }

      [TestMethod]
      [ExpectedException(typeof(ArgumentException), "Negative per-minute did not raise exception")]
      public void NegativePerMinuteUnitRaisesArgumentException() {
         var rule = new RateLimitRule(HttpVerb.Post, @"/Dummy", -1, 2, 3);
      }

      [TestMethod]
      [ExpectedException(typeof(ArgumentException), "Negative per-hour did not raise exception")]
      public void NegativePerHourUnitRaisesArgumentException() {
         var rule = new RateLimitRule(HttpVerb.Post, @"/Dummy", 1, -2, 3);
      }

      [TestMethod]
      [ExpectedException(typeof(ArgumentException), "Negative per-day did not raise exception")]
      public void NegativePerDayUnitRaisesArgumentException() {
         var rule = new RateLimitRule(HttpVerb.Post, @"/Dummy", 1, 2, -3);
      }
   }

}
