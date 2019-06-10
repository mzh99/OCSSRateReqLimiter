using System;
using System.Collections.Generic;

namespace OCSS.Web.RateReqLimiter {

   public class RateLimitCacheEntry {
      public int ReqCnt { get; set; }
      public TimeSpan TimeAllowed { get; private set; }
      public DateTime FirstReqTime { get; private set; }
      public DateTime ExpirationDt { get; private set; }

      public RateLimitCacheEntry(TimeSpan timeAllowed) {
         this.ReqCnt = 1;
         this.TimeAllowed = timeAllowed;
         this.FirstReqTime = DateTime.UtcNow;
         this.ExpirationDt = this.FirstReqTime.Add(this.TimeAllowed);
      }

      public bool IsExpired() {
         return IsExpired(DateTime.UtcNow);
      }

      public bool IsExpired(DateTime compareDt) {
         return DateTime.Compare(ExpirationDt, compareDt) <= 0;
      }

      public TimeSpan CalcRemainingTime() {
         return CalcRemainingTime(DateTime.UtcNow);
      }

      public TimeSpan CalcRemainingTime(DateTime dt) {
         return IsExpired(dt) ? new TimeSpan(0) : ExpirationDt - dt;
      }

      //public TimeSpan CalcTimeDiff(DateTime dt1, DateTime dt2) {
      //   return (dt1 - dt2).Duration();
      //}

      public string BuildTimeCountdownStringFromSpan(TimeSpan span) {
         List<string> parts = new List<string>();
         if (span.Hours > 0)
            parts.Add($"{span.Hours} hours");
         if (span.Minutes> 0)
            parts.Add($"{span.Minutes} minutes");
         parts.Add($"{span.Seconds} seconds");
         return string.Join(", ", parts);
      }

   }

}
