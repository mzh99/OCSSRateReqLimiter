using System.Net;
using Microsoft.AspNetCore.Mvc.Filters;

namespace OCSS.Web.RateReqLimiter {

   /// <summary>Implementation of IFilteringKey that uses IPv4 addresses</summary>
   public class IPv4FilteringKey: IFilteringKey {

      public string BuildKey(PageHandlerExecutingContext context) {
         return GetRemoteIp(context.HttpContext.Connection.RemoteIpAddress);
      }

      public string GetFilterKeyName() {
         return "IPv4";
      }

      private string GetRemoteIp(IPAddress clientIP) {
         // string ip = ;
         var ipStr = string.Empty;
         if (clientIP != null) {
            // if it's a v4 mapped to v6, get the v4. see: https://docs.microsoft.com/en-us/dotnet/api/system.net.ipaddress.maptoipv4?view=netframework-4.8
            if (clientIP.IsIPv4MappedToIPv6)
               clientIP = clientIP.MapToIPv4();
            ipStr = clientIP.ToString();
            if (ipStr == "::1" || ipStr == "0.0.0.1")
               ipStr = "127.0.0.1";
         }
         return ipStr;
      }

   }

}
