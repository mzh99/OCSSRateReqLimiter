using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OCSS.StringUtil;

namespace OCSS.Web.RateReqLimiter {

   /// <summary>CSV formatted config file loader</summary>
   /// <remarks>See BuildRuleFromConfigEntry() for file layout of entries.</remarks>
   public class CSVConfigLoader: IRateLimitConfigLoader {

      private static readonly int FieldMethod = 0;
      private static readonly int FieldPage = 1;
      private static readonly int FieldReqPerMin = 2;
      // private static readonly int FieldReqPerHour = 3;
      private static readonly int FieldReqPerDay = 4;
      private static readonly int MinNumFields = 5;
      private static readonly string[] ValidMethods = Enum.GetNames(typeof(HttpVerb)).Select(s => s.ToLower()).ToArray();

      private DateTime lastWriteDateRateLimitRules;
      private DateTime lastWriteDateWhitelistIPv4;

      public string RateLimitRuleFilename { get; private set; }
      public string WhitelistIPv4FileName { get; private set; }

      public CSVConfigLoader(string rateLimitFilename, string whitelistIPv4Filename) {
         this.RateLimitRuleFilename = rateLimitFilename;
         this.WhitelistIPv4FileName = whitelistIPv4Filename;
      }

      public IEnumerable<RateLimitRule> LoadRateLimitRules() {
         if (File.Exists(RateLimitRuleFilename) == false)
            throw new FileNotFoundException("Rate-limit rule file not found: " + RateLimitRuleFilename);
         // save last write time as it's the trigger for ConfigHasChanged()
         lastWriteDateRateLimitRules = File.GetLastWriteTimeUtc(RateLimitRuleFilename);
         return File.ReadAllLines(RateLimitRuleFilename).Where(s => string.IsNullOrWhiteSpace(s) == false).Select(rate => BuildRuleFromConfigEntry(rate));
      }

      public IEnumerable<string> LoadWhitelist() {
         if (File.Exists(WhitelistIPv4FileName) == false)
            throw new FileNotFoundException("IPv4 whitelist file not found: " + WhitelistIPv4FileName);
         // save last write time as it's the trigger for ConfigHasChanged()
         lastWriteDateWhitelistIPv4 = File.GetLastWriteTimeUtc(WhitelistIPv4FileName);
         return File.ReadAllLines(WhitelistIPv4FileName).Where(s => string.IsNullOrWhiteSpace(s) == false).Select(s => s.ParseIPv4());

      }

      public bool RateLimitConfigHasChanged() {
         return DateTime.Compare(lastWriteDateRateLimitRules, File.GetLastWriteTimeUtc(RateLimitRuleFilename)) != 0;
      }

      public bool WhitelistHasChanged() {
         return DateTime.Compare(lastWriteDateWhitelistIPv4, File.GetLastWriteTimeUtc(WhitelistIPv4FileName)) != 0;
      }

      /// <summary>Build a rate-limit rule from a comma-delimited string </summary>
      /// <param name="entry">string entry</param>
      /// <returns>A rate-limit rule (RateLimitRule)</returns>
      /// <remarks>
      /// Entry is a simple, flat comma-delimited string consisting of:
      ///   Method,Page,ReqsPerMin,ReqsPerHour,ReqsPerDay
      ///   ex) Post,/Account/Login,6,30,100
      ///
      ///   * Blank lines are okay and will be ignored
      ///
      ///   * Whitespace will be trimmed around each entry, so use it freely
      ///   ex) Post,    /Account/Login,         6,  30, 100
      ///
      ///   * Case is unimportant in method and page
      ///   ex) get,/account/login,6,30,100
      ///
      ///   * Empty Request Rates will be treated as zero meaning no tracking for that time-unit
      ///   ex) get,/account/login,,30,100   (no per-minute tracking)
      ///   ex) get,/account/login,,,100   (no per-minute and per-hour tracking)
      ///
      ///   * Anything after the fifth field in the entry will be ignored, so use as comments if you want
      ///   ex) get,/account/login,5,30,100, !! Never remove this entry
      ///
      /// </remarks>
      private RateLimitRule BuildRuleFromConfigEntry(string entry) {
         var fields = entry.Split(new char[] { ',' }, StringSplitOptions.None).Select(s => s.Trim().ToLower()).ToArray();
         if (fields.Length < MinNumFields)
            throw new ArgumentException($"Entry must have at least {MinNumFields} comma-delimited fields");
         int methodNdx = Array.IndexOf(ValidMethods, fields[FieldMethod]);
         if (methodNdx < 0)
            throw new ArgumentException($"Method {fields[FieldMethod]} is not a valid choice for field 1");
         HttpVerb verb = (HttpVerb) methodNdx;
         if (string.IsNullOrWhiteSpace(fields[FieldPage]))
            throw new ArgumentException("Page/path (field 2) is empty.");
         // setup array for request counts
         int[] reqCnts = new int[3];
         for (int z = FieldReqPerMin; z <= FieldReqPerDay; z++) {
            int result = 0;   // default for empty time-unit
            if (fields[z] != string.Empty) {    // only raise exception for non-empty field
               if (int.TryParse(fields[z], out result) == false) {
                  throw new ArgumentException($"Number of requests per time-unit in field {z + 1} is not a valid integer");
               }
            }
            reqCnts[z - FieldReqPerMin] = result;
         }

         return new RateLimitRule(verb, fields[1], reqCnts[0], reqCnts[1], reqCnts[2]);
      }

   }

}
