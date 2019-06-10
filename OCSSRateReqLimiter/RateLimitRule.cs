using System;

namespace OCSS.Web.RateReqLimiter {

   public class RateLimitRule {

      private static readonly string[] TimeUnits = { "minute", "hour", "day" };
      private readonly int[] reqPerUnit = new int[3];

      public HttpVerb Method { get; private set; }
      public string PagePath { get; private set; }
      public int RequestsPerMinute { get { return reqPerUnit[0]; } }
      public int RequestsPerHour { get { return reqPerUnit[1]; } }
      public int RequestsPerDay { get { return reqPerUnit[2]; } }
      public string MethodAsString {
         get { return Enum.GetName(typeof(HttpVerb), Method); }
      }
      /// <summary>Generate general key for rule lookup (existance check)</summary>
      public string RuleKey {
         get { return MakeRuleKey(PagePath, MethodAsString); }
      }
      public int[] RequestLimits {
         get { return reqPerUnit; }
      }

      public RateLimitRule(HttpVerb method, string path, int maxRequestsPerMinute, int maxRequestsPerHour, int maxRequestsPerDay) {
         if (maxRequestsPerMinute < 0)
            throw new ArgumentException("Requests per minute must be 0 or more.");
         if (maxRequestsPerHour < 0)
            throw new ArgumentException("Requests per hour must be 0 or more.");
         if (maxRequestsPerDay < 0)
            throw new ArgumentException("Requests per day must be 0 or more.");
         if (Enum.IsDefined(typeof(HttpVerb), method) == false)
            throw new ArgumentException("HttpVerb is an invalid method.");
         //if (maxRequestsPerMinute > maxRequestsPerHour || maxRequestsPerMinute > maxRequestsPerDay)
         //   throw new ArgumentException("Granularity is incorrect. Per minute is more than per hour or per day.");
         //if (maxRequestsPerHour > maxRequestsPerDay)
         //   throw new ArgumentException("Granularity is incorrect. Per hour is more than per hour.");
         this.Method = method;
         this.PagePath = path;
         reqPerUnit[0] = maxRequestsPerMinute;
         reqPerUnit[1] = maxRequestsPerHour;
         reqPerUnit[2] = maxRequestsPerDay;
      }

      public string GetTimeUnitName(int ndx) {
         if (ndx < 0 || ndx > 2)
            throw new ArgumentException("index must be 0-2");
         return TimeUnits[ndx];
      }

      public string GetRuleDesc(int ndx) {
         if (ndx < 0 || ndx > 2)
            throw new ArgumentException("index must be 0-2");
         return (reqPerUnit[ndx] == 1) ? $"{reqPerUnit[ndx]} request per {TimeUnits[ndx]}" : $"{reqPerUnit[ndx]} requests per {TimeUnits[ndx]}";
      }

      /// <summary>Generate general key for rule lookup (existance check)</summary>
      /// <param name="path">page path</param>
      /// <param name="method">method string (get, put, etc.)</param>
      /// <returns></returns>
      public static string MakeRuleKey(string path, string method) {
         return string.Join("-", method, path).ToLower();
      }

   }

}
