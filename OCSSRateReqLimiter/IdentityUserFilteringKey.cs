using Microsoft.AspNetCore.Mvc.Filters;

namespace OCSS.Web.RateReqLimiter {

   public class IdentityUserFilteringKey: IFilteringKey {

      public string BuildKey(PageHandlerExecutingContext context) {
         var ident = context.HttpContext.User.Identity;
         return (ident.IsAuthenticated) ? ident.Name.ToLower() : string.Empty;
      }

      public string GetFilterKeyName() {
         return "IDUser";
      }

   }

}
