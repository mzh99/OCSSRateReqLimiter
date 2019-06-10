using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;

namespace OCSS.Web.RateReqLimiter {

   internal class CfgLimitRule {
      public string Method { get; set; }
      public string Path { get; set; }
      public int ReqPerMinute { get; set; }
      public int ReqPerHour { get; set; }
      public int ReqPerDay { get; set; }

      public CfgLimitRule() {
         this.Method = string.Empty;
         this.Path = string.Empty;
         this.ReqPerMinute = 0;
         this.ReqPerHour = 0;
         this.ReqPerDay = 0;
      }
   }

   internal class CfgRateLimitSection {
      public int Version { get; set; }
      public CfgLimitRule[] Rules { get; set; }
   }

   internal class CfgRateWhitelistSection {
      public int Version { get; set; }
      public string[] Entries { get; set; }
   }

   /// <summary>Rate-limit rule loader based on IConfiguration settings</summary>
   /// <remarks>
   /// Sample configuration in appsettings.json:
      /*
      "OCSSRateLimitSection": {
         "Version": 1,
         "Rules": [
            { "Method": "Get", "Path": "/Index", "ReqPerMinute": 5, "ReqPerHour": 10, "ReqPerDay": 25 },
            { "Method": "Post", "Path": "/Account/Login", "ReqPerMinute": 5, "ReqPerHour": 10, "ReqPerDay": 25 }
         ]
      },
      "OCSSWhitelistSection": {
         "Version": 1,
         "Entries": [ "192.168.1.100", "192.168.1.101" ]
      }
      */
   /// </remarks>
   public class AppConfigLoader: IRateLimitConfigLoader {

      private static readonly string[] ValidMethods = Enum.GetNames(typeof(HttpVerb)).Select(s => s.ToLower()).ToArray();

      private static readonly string SectionKeyRateLimit = "OCSSRateLimitSection";
      private static readonly string SectionKeyWhitelist = "OCSSWhitelistSection";
      private static readonly string SectionKeyVersion = "Version";

      private int lastRuleVer = 0;
      private int lastWhitelistVer = 0;

      private readonly IConfiguration config;

      public AppConfigLoader(IConfiguration config) {
         this.config = config;
      }

      public IEnumerable<RateLimitRule> LoadRateLimitRules() {
         var cfg = config.GetSection(SectionKeyRateLimit).Get<CfgRateLimitSection>();
         lastRuleVer = cfg.Version; // update version which is our trigger for changes
         foreach (var cfgRule in cfg.Rules) {
            int methodNdx = Array.IndexOf(ValidMethods, cfgRule.Method.ToLower());
            if (methodNdx < 0)
               throw new ArgumentException($"Method {cfgRule.Method} is not a valid choice");
            HttpVerb verb = (HttpVerb) methodNdx;
            yield return new RateLimitRule(verb, cfgRule.Path, cfgRule.ReqPerMinute, cfgRule.ReqPerHour, cfgRule.ReqPerDay);
         }
      }

      public IEnumerable<string> LoadWhitelist() {
         var cfg = config.GetSection(SectionKeyWhitelist).Get<CfgRateWhitelistSection>();
         lastWhitelistVer = cfg.Version; // update version which is our trigger for changes
         return cfg.Entries.Select(e => e.ToLower());
      }


      public bool RateLimitConfigHasChanged() {
         int ver = config.GetSection(SectionKeyRateLimit).GetValue<int>(SectionKeyVersion);
         return ver != lastRuleVer;
      }

      public bool WhitelistHasChanged() {
         int ver = config.GetSection(SectionKeyWhitelist).GetValue<int>(SectionKeyVersion);
         return ver != lastWhitelistVer;
      }
   }

}
