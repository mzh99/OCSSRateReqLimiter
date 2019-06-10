using System.Collections.Generic;

namespace OCSS.Web.RateReqLimiter {

   public interface IRateLimitConfigLoader {

      IEnumerable<RateLimitRule> LoadRateLimitRules();
      bool RateLimitConfigHasChanged();

      IEnumerable<string> LoadWhitelist();
      bool WhitelistHasChanged();

   }

}
