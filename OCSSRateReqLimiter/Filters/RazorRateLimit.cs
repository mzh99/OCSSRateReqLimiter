using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace OCSS.Web.RateReqLimiter.Filters {

   /// <summary>Razor Page Filter that throttles access for specific pages by IP address, user ID, etc.</summary>
   /// <remarks>See README.md for details</remarks>
   public class RazorRateLimit: IAsyncPageFilter {

      // timespans for 1 minute, 1 hour, 1 day
      private static readonly TimeSpan[] spans = { new TimeSpan(0, 1, 0), new TimeSpan(1, 0, 0), new TimeSpan(1, 0, 0, 0) };

      private readonly ILogger logger;
      private readonly IMemoryCache memoryCache;
      private readonly RateLimits rateLimits;

      public RazorRateLimit(RateLimits rateLimits, IMemoryCache memoryCache, ILogger<RazorRateLimit> logger) {
         this.memoryCache = memoryCache;
         this.logger = logger;
         this.rateLimits = rateLimits;
      }

     public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next) {
         RateLimitCacheEntry cacheRec = null;

         string path = context.ActionDescriptor.ViewEnginePath;
         string method = context.HttpContext.Request.Method;
         // string method = context.HandlerMethod.HttpMethod;
         string key = RateLimitRule.MakeRuleKey(path, method);
         if (rateLimits.RateLimitRuleMatches(key)) {
            logger?.LogDebug($"Limit rule for page: {path} and method: {method} matched");
            string filterKey = rateLimits.FilterKey.BuildKey(context);
            string filterKeyName = rateLimits.FilterKey.GetFilterKeyName();
            if (rateLimits.IsWhitelisted(filterKey)) {
               logger?.LogDebug($"Rate ignored for whitelisted {filterKeyName}: {filterKey}");
            }
            else {
               var rule = rateLimits.GetRateLimitByKey(key);
               var ruleLimits = rule.RequestLimits;
               var cacheKeys = GetTimeUnitCacheKeys(path, method, filterKey, filterKeyName).ToArray();
               DateTime now = DateTime.UtcNow;
               // limit for this page and method is in effect, check cache entries for each time unit (minute, hour, day)
               for (int z = 0; z < 3; z++) {
                  if (ruleLimits[z] == 0) {
                     // zero means don't do limiting for this time unit
                     logger?.LogDebug($"Rate-limiting disabled for per-{rule.GetTimeUnitName(z)}");
                  }
                  else {
                     bool exists = memoryCache.TryGetValue<RateLimitCacheEntry>(cacheKeys[z], out cacheRec);
                     if (exists) {
                        if (cacheRec.IsExpired(now)) {
                           // there's no guarantee the system will auto-expire entries on time, so we have to manually check them
                           logger?.LogDebug($"Manual delete of expired cache key for {rule.GetRuleDesc(z)} for {filterKeyName} {filterKey}");
                           memoryCache.Remove(cacheKeys[z]);   // dump the old cache entry
                           // create a new cache entry
                           cacheRec = new RateLimitCacheEntry(spans[z]);
                           CreateOrUpdateCacheEntry(cacheKeys[z], cacheRec, spans[z]);
                        }
                        else {
                           TimeSpan timeRemaining = cacheRec.CalcRemainingTime(now);
                           int totSecs =  Convert.ToInt32(timeRemaining.TotalSeconds) + 1;
                           string timeRemainingStr = cacheRec.BuildTimeCountdownStringFromSpan(timeRemaining);
                           if (cacheRec.ReqCnt + 1 > ruleLimits[z]) {
                              // cache limit has been exceeded - return 429
                              logger?.LogDebug($"Rule limit ({cacheRec.ReqCnt}) exceeded for {rule.GetRuleDesc(z)} for Filter: {filterKeyName}, Key: {filterKey}");
                              context.HttpContext.Response.Headers.Add("Retry-After", totSecs.ToString());
                              context.Result = new ContentResult {
                                 // StatusCode = (int) HttpStatusCode.TooManyRequests,
                                 StatusCode = 429,
                                 ContentType = "text/html",
                                 Content = $"Request quota ({rule.GetRuleDesc(z)}) exceeded. Try again in {timeRemainingStr}."
                              };
                              return;
                           }
                           else {
                              logger?.LogDebug($"Time remaining on cache entry per {rule.GetTimeUnitName(z)}: {timeRemainingStr} for Filter: {filterKeyName}, Key: {filterKey}");
                              logger?.LogDebug($"Adding 1 to count ({cacheRec.ReqCnt}) per {rule.GetTimeUnitName(z)} for Filter: {filterKeyName}, Key: {filterKey}");
                              cacheRec.ReqCnt += 1;
                              // pass in new time left based on original starting time and span
                              CreateOrUpdateCacheEntry(cacheKeys[z], cacheRec, cacheRec.CalcRemainingTime(now));
                           }
                        }
                     }
                     else {
                        // doesn't exist in cache, so we'll create it fresh
                        cacheRec = new RateLimitCacheEntry(spans[z]);
                        CreateOrUpdateCacheEntry(cacheKeys[z], cacheRec, spans[z]);
                     }
                  }
               }
            }
         }
         else {
            logger?.LogDebug($"No limit rule for page: {path} and method: {method}");
         }

         await next.Invoke();
      }

      public async Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) {
         // cannot set Result in SelectionAsync(), so we are using ExecutionAsync() above
         await Task.CompletedTask;
      }

      private void CreateOrUpdateCacheEntry(string key, RateLimitCacheEntry entry, TimeSpan span) {
         var cacheOpts = new MemoryCacheEntryOptions().SetPriority(CacheItemPriority.High).SetAbsoluteExpiration(span).RegisterPostEvictionCallback(EvictionNotice);
         memoryCache.Set<RateLimitCacheEntry>(key, entry, cacheOpts);
      }

      private void EvictionNotice(object key, object value, EvictionReason reason, object state) {
         if (reason != EvictionReason.Replaced && reason != EvictionReason.Removed) {
            logger?.LogDebug($"Cache Key {key} evicted for reason: {reason} ");
         }
      }

      private IEnumerable<string> GetTimeUnitCacheKeys(string path, string method, string filterKey, string filterKeyName) {
         // one key per time unit (minute, hour, day)
         for (int z = 0; z < 3; z++) {
            yield return string.Join("-", filterKey, filterKeyName, z.ToString(), path, method).ToLower();
         }
      }

      //private void DumpContextForDebug(PageHandlerSelectedContext context) {
         //string desc1 = $"Area: {context.ActionDescriptor.AreaName}\r\nDisplayname: {context.ActionDescriptor.DisplayName}\r\nRel Path: {context.ActionDescriptor.RelativePath} ViewEnginePath: {context.ActionDescriptor.ViewEnginePath}";
         //string desc2 = $"HandlerMethod: {context.HandlerMethod.HttpMethod}\r\nHttpContext Method: {context.HttpContext.Request.Method}\r\n Context Path: {context.HttpContext.Request.Path.Value}";
         //logger?.LogDebug(desc1);
         //logger?.LogDebug(desc2);
      //}

   }

}
