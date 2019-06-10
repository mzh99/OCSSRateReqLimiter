using Microsoft.AspNetCore.Mvc.Filters;

namespace OCSS.Web.RateReqLimiter {

   public interface IFilteringKey {
      string BuildKey(PageHandlerExecutingContext context);
      string GetFilterKeyName();
   }

}
