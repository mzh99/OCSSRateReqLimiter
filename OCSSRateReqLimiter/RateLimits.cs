using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OCSS.Web.RateReqLimiter {

   public enum HttpVerb { Get, Put, Post, Delete, Path, Head, Options };
   public enum ConfigEventType { RateLimitRules, Whitelist };

   // todo: Possible future enhancements:
   //    - RegExp support for page/path matching
   //
   public class RateLimits: IDisposable {

      private static readonly object lookupsLocker = new object();    // lock object for lookup building/reloading

      // some convenience constants for the ultra-lazy :)
      public static readonly TimeSpan OneMinute = new TimeSpan(0, 1, 0);
      public static readonly TimeSpan FiveMinutes = new TimeSpan(0, 5, 0);
      public static readonly TimeSpan TenMinutes = new TimeSpan(0, 10, 0);
      public static readonly TimeSpan FifteenMinutes = new TimeSpan(0, 15, 0);
      public static readonly TimeSpan ThirtyMinutes = new TimeSpan(0, 30, 0);
      public static readonly TimeSpan OneHour = new TimeSpan(1, 0, 0);
      public static readonly TimeSpan TwoHours = new TimeSpan(2, 0, 0);
      public static readonly TimeSpan FourHours = new TimeSpan(4, 0, 0);
      public static readonly TimeSpan EightHours = new TimeSpan(8, 0, 0);
      public static readonly TimeSpan OneDay = new TimeSpan(1, 0, 0, 0);

      private Timer timer = null;

      private readonly Action<ConfigEventType> configChangedEvent = null;
      private readonly Action<ConfigEventType> configCheckedEvent = null;
      private readonly IRateLimitConfigLoader configLoader;
      private readonly TimeSpan checkChangeFrequency;

      private HashSet<string> whiteList;
      private Dictionary<string, RateLimitRule> ruleLookup;

      public IFilteringKey FilterKey { get; private set; }

      /// <summary>constructor for loading rate rules and whitelist</summary>
      public RateLimits(IRateLimitConfigLoader loader, IFilteringKey filterKey, TimeSpan checkChangeFrequency, Action<ConfigEventType> configChangedEvent = null, Action<ConfigEventType> configCheckedEvent = null) {
         this.configLoader = loader;
         this.FilterKey = filterKey;
         this.checkChangeFrequency = checkChangeFrequency;
         this.configChangedEvent = configChangedEvent;
         this.configCheckedEvent = configCheckedEvent;
         SetRateLimits(configLoader.LoadRateLimitRules());
         SetWhitelist(configLoader.LoadWhitelist());
         StartTimer();
      }

      private void StartTimer() {
         timer = new Timer(CheckForChange, null, checkChangeFrequency, checkChangeFrequency);
      }

      private void StopTimer() {
         timer?.Change(Timeout.Infinite, Timeout.Infinite);
      }

      private void DisposeTimer() {
         timer?.Dispose();
      }

      private void CheckForChange(object state) {
         configCheckedEvent?.Invoke(ConfigEventType.RateLimitRules);
         if (configLoader.RateLimitConfigHasChanged()) {
            SetRateLimits(configLoader.LoadRateLimitRules());
            configChangedEvent?.Invoke(ConfigEventType.RateLimitRules);
         }
         configCheckedEvent?.Invoke(ConfigEventType.Whitelist);
         if (configLoader.WhitelistHasChanged()) {
            SetWhitelist(configLoader.LoadWhitelist());
            configChangedEvent?.Invoke(ConfigEventType.Whitelist);
         }
      }

      private void SetRateLimits(IEnumerable<RateLimitRule> rates) {
         lock (lookupsLocker) {
            // setup fast rule lookup dictionary
            this.ruleLookup = rates.ToDictionary(k => k.RuleKey, v => v);
         }
      }

      private void SetWhitelist(IEnumerable<string> whitelist) {
         lock (lookupsLocker) {
            // setup fast whitelist hashset
            this.whiteList = whitelist == null ? new HashSet<string>() : new HashSet<string>(whitelist);
         }
      }

      public bool IsWhitelisted(string entry) {
         lock (lookupsLocker) {
            return whiteList.Contains(entry);
         }
      }

      public bool RateLimitRuleMatches(string path, string method) {
         string key = RateLimitRule.MakeRuleKey(path, method);
         return RateLimitRuleMatches(key);
      }

      public bool RateLimitRuleMatches(string key) {
         lock (lookupsLocker) {
            return ruleLookup.ContainsKey(key);
         }
      }

      public RateLimitRule GetRateLimitByKey(string key) {
         lock (lookupsLocker) {
            return ruleLookup.ContainsKey(key) ? ruleLookup[key] : null;
         }
      }

      #region IDisposable Support
      private bool disposedValue = false; // To detect redundant calls

      protected virtual void Dispose(bool disposing) {
         if (disposedValue == false) {
            if (disposing) {
               // TODO: dispose managed state (managed objects).
            }
            DisposeTimer();
            whiteList = null;
            ruleLookup = null;
            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.
            disposedValue = true;
         }
      }

      // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
      ~RateLimits() {
         // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
         Dispose(false);
      }

      // This code added to correctly implement the disposable pattern.
      public void Dispose() {
         // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
         Dispose(true);
         // TODO: uncomment the following line if the finalizer is overridden above.
         GC.SuppressFinalize(this);
      }
      #endregion

   }

}
