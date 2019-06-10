## OCSSRateReqLimiter
Web Request Rate-limiter (Throttling) classes with a Filter for Razor Pages

Purpose:
Create a basic web request throttling engine that filters by simple items like IPv4 address, UserID, etc. and is usable by Razor Pages.

Limitations:
 1) The Ipv4 filtering implementation does not support IP Ranges.
 2) Time-units supported for throttling are per-minute, per-hour, and per-day.
     If you don't want to use all three, use zero for the MaxRequest count for a specific time-unit.
     For example, if you only care about per-minute tracking, use zero for per-hour and per-day units.
 3) http and https calls are not separated. All calls to an action count together regardless of scheme. (see rule #4)
 4) Granularity for tracking is method + Page/Path. 
    Different url parameters for the same action are considered identical.

Requirements:
 1) `IMemoryCache` is required, so add this line to the `ConfigureServices()` method early in the pipeline in Startup.cs:
    ```
    services.AddMemoryCache()
    ```

 2) Include these Using statements in Startup.cs:
 ```
      using OCSS.Web.RateReqLimiter;
      using OCSS.Web.RateReqLimiter.Filters;
```      

 3) Create an IRateLimitConfigLoader implementation to pass to the RateLimits() constructor (or use of the three provided).
    There are three implementations included:
      a) a CSV file loader (CSVConfigLoader)
      b) an IConfiguration loader (AppConfigLoader)
      c) an in-memory loader (MemConfigLoader); this loader never triggers changes. App must be rebooted to take effect.
    
   I prefer the CSV loader as the data formats are very simple.
   Contrast this:
```   
      Get,/Index,5,10,25
      Post,/Account/Login,5,10,25
```      
   To this (and this is compact JSON):
```   
      "OCSSRateLimitSection": {
          "Version": 1,
          "Rules": [
             { "Method": "Get", "Path": "/Index", "ReqPerMinute": 5, "ReqPerHour": 10, "ReqPerDay": 25 },
             { "Method": "Post", "Path": "/Account/Login", "ReqPerMinute": 5, "ReqPerHour": 10, "ReqPerDay": 25 }
          ]
       }
```       
   i.e. There are so many possible editing mistakes with json; just forget or add an extra quote, comma, colon, bracket, or brace, or you can misspell a property name, etc.
   But I understand why some would want to have everything in appsettings.json, so it's up to you.

4) Create an IFilteringKey implementation to pass to the RateLimits() constructor (or use of the two provided).
   These are super-simple and only involve a few lines of code to create your own.
   For Razor Pages, the PageHandlerExecutingContext is passed to your `IFilteringKey.BuildKey()` function, so you can grab almost everything from HttpContext, Razor Page Context, Connection, etc.
   There are two implementations included:
       a) an IPv4 address filtering builder (IPv4FilteringKey)
       b) an Identity User Name (IdentityUserFilteringKey)

Tech Notes:
1) The whitelisting is a simple exact string match based on your key returned from the `IFilteringKey.BuildKey()` function (trimmed and converted to lower case).
   Because it's converted to lower case, any IFilteringKey implementations should also return lower case keys.

2) In cache, the filter key name from `IFilteringKey.GetFilterKeyName()` is included to make them unique.
   This can be the case when you want to have multiple filters running simultaneously.

3) Action path/page is taken from `ActionDescriptor.ViewEnginePath`. I believe I could also use ActionDescriptor.DisplayName as get the same.
   This differs from ActionDescriptor.RelativePath, which is the full Razor file path with file extension.
   This differs from HttpContext.Request.Path, which is the path with optional Area name included.

4) A timer is used to periodically look for changes; there is no "live" config change trigger.
   This is mainly due to the quirky IxxxProvider.Watch() and IChangeToken, where a callback can be called many times in succession for a single change.
   There are other alternatives, but they are not used in this package.
   - `CSVConfigLoader()` uses the LastWriteDateTime of the file to detect changes
   - `AppConfigLoader()` uses a "Version" sub-section key (see above) as a file-change trigger, so when editing your live app config settings, always increment the "Version" key.

5) Here is some sample setup code for CSV and IConfiguration using an IPv4 address filter. Both use a 15 minute check for changes timer.
```
   IFilteringKey filterKey = new IPv4FilteringKey();
   #region Sample code for initializing Startup.cs using CSV file-based setup
      // Rate Limit file is stored in a folder off the Content Root at: /RateLimitRules/{envName}RateRules.txt
      var rateLimitFilePath = Path.Combine(env.ContentRootPath, "RateLimitRules", $"{env.EnvironmentName}RateRules.txt");
      // IPv4 Whitelist is stored in a folder off the Content Root at: /RateLimitRules/{envName}WhitelistIPv4.txt
      var whitelistFilePath = Path.Combine(env.ContentRootPath, "RateLimitRules", $"{env.EnvironmentName}WhitelistIPv4.txt");
      IRateLimitConfigLoader configLoader = new CSVConfigLoader(rateLimitFilePath, whitelistFilePath);
   #endregion
   #region Setup Rate-limiting using App settings
      // this assumes configuration is a private field containing IConfiguration from the Startup() constructor.
      IRateLimitConfigLoader configLoader = new AppConfigLoader(configuration);
   #endregion
   // In this sample, ConfigChanged and ConfigChecked are delegates called when config files are respectively changed and checked. 
   // ConfigChecked triggers on the timer interval mentioned above in #4.
   var limits = new RateLimits(configLoader, filterKey, RateLimits.FifteenMinutes, ConfigChanged, ConfigChecked);
   // then add rate-limiting rules to IoC container as a singleton
   services.AddSingleton<RateLimits>(limits);
```
6) The Razor Filter response with Http Status Code 429 when a limit is reached.
   In this case, a "Retry-After" response header is added with a value in seconds to wait.
   For example, if you exceed an hour limit in the first 5 minutes, you should see a value of 3300 or 55*60.
   This makes it feasible to react programmatically without any parsing of return values.

7) Currently, IPv4 addresses for whitelist are not validated. This could be a future enhancement if you are concerned about data-entry errors.
