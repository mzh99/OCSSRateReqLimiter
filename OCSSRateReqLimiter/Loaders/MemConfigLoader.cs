using System.Collections.Generic;
using System.Linq;

namespace OCSS.Web.RateReqLimiter {

   public class MemConfigLoader: IRateLimitConfigLoader {

      private readonly RateLimitRule[] rules;
      private readonly string[] whitelistIPv4;

      public MemConfigLoader(IEnumerable<RateLimitRule> rules, IEnumerable<string> whitelistIPv4) {
         this.rules = rules.ToArray();
         this.whitelistIPv4 = whitelistIPv4.ToArray();
      }

      public IEnumerable<string> LoadWhitelist() {
         return whitelistIPv4;
      }

      public IEnumerable<RateLimitRule> LoadRateLimitRules() {
         return rules;
      }

      public bool RateLimitConfigHasChanged() {
         return false;  // memloader never changes
      }

      public bool WhitelistHasChanged() {
         return false;  // memloader never changes
      }
   }

}
