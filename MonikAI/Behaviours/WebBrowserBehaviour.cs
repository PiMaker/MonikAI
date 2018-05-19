using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Windows.Automation;
using MonikAI.Behaviours.HttpRestServer;
using MonikAI.Parsers;
using MonikAI.Parsers.Models;
using ResponseTuple = System.Tuple<System.Collections.Generic.List<MonikAI.Expression[]>, System.DateTime>;

namespace MonikAI.Behaviours
{
    /// <summary>
    /// Manages responses to web sites being opened.
    /// Includes Google Search Behaviour!
    /// </summary>
    public class WebBrowserBehaviour : IBehaviour
    {
        // Translation table for shortened URLs
        private readonly Dictionary<string, string> unshorteningDictionary = new Dictionary<string, string>
        {
            {"youtu.be", "youtube.com"}
        };

        // Minimum time that has to elapse before a web page response is shown again
        private readonly TimeSpan minimumElapsedTime = new TimeSpan(0, 12, 0);
        private readonly Random random = new Random();

        // Sorry, couldn't resist. Regex is just too good.
        private const string GOOGLE_REGEX = ".*\\.?google\\..{2,3}.*q\\=(.*?)($|&)";

        private readonly CSVParser parser = new CSVParser();

        private readonly Dictionary<string, ResponseTuple> responseTable = new Dictionary<string, ResponseTuple>();
        private readonly Dictionary<string[], ResponseTuple> responseTableGoogle = new Dictionary<string[], ResponseTuple>(new TriggerComparer());

        private string lastUrl = string.Empty;

        private int executionCounter = 0;
        private const int EXECUTION_LIMIT = 8;

        public void Init(MainWindow window)
        {
            // Parse the CSV file
            var csvFile = this.parser.GetData("website");
            this.PopulateResponseTable(this.parser.ParseData(csvFile));

            // Add google search CSV
            var googleCsvFile = this.parser.GetData("google");
            this.PopulateGoogleResponseTable(this.parser.ParseData(googleCsvFile));
        }

        private void PopulateResponseTable(IEnumerable<DokiResponse> characterResponses)
        {
            foreach (var response in characterResponses)
            {
                var trigger = response.ResponseTriggers.First();
                trigger = trigger.ToLower().Trim().TrimEnd('/');

                if (trigger.StartsWith("http://"))
                {
                    trigger = trigger.Substring(7);
                }

                if (trigger.StartsWith("https://"))
                {
                    trigger = trigger.Substring(8);
                }

                if (trigger.StartsWith("www."))
                {
                    trigger = trigger.Substring(4);
                }

                if (this.responseTable.ContainsKey(trigger))
                {
                    var entry = this.responseTable[trigger];
                    this.responseTable[trigger] = new ResponseTuple(entry.Item1.Concat(new[] { response.ResponseChain.ToArray() }).ToList(), entry.Item2);
                }
                else
                {
                    this.responseTable.Add(trigger, new ResponseTuple(new List<Expression[]> { response.ResponseChain.ToArray() }, DateTime.MinValue));
                }
            }
        }

        private void PopulateGoogleResponseTable(IEnumerable<DokiResponse> characterResponses)
        {
            foreach (var response in characterResponses)
            {
                // Convert triggers to array to use as a key for the dictionary
                var triggers = response.ResponseTriggers.Select(x => x.ToLower().Trim()).ToArray();

                // Add every response to the current trigger into a new array to use as a value in the dictionary
                var responseChain = new Expression[response.ResponseChain.Count];
                for (var chain = 0; chain < response.ResponseChain.Count; chain++)
                {
                    responseChain[chain] = response.ResponseChain[chain];
                }

                List<Expression[]> triggerResponses;
                if (this.responseTableGoogle.ContainsKey(triggers))
                {
                    triggerResponses = this.responseTableGoogle[triggers].Item1;
                    triggerResponses.Add(responseChain);
                }
                else
                {
                    triggerResponses = new List<Expression[]> { responseChain };
                }

                // If trigger is a browser, only respond if the user recently launched the browser
                this.responseTableGoogle[triggers] = new ResponseTuple(triggerResponses, DateTime.MinValue);
            }
        }

        private DateTime lastUrlChangeTime = DateTime.MinValue;
        private bool lastUrlResponded = false;

        public void Update(MainWindow window)
        {
            this.executionCounter++;
            if (this.executionCounter < WebBrowserBehaviour.EXECUTION_LIMIT * (MonikaiSettings.Default.PotatoPC ? 4 : 1))
            {
                return;
            }

            this.executionCounter = 0;

            var changed = false;

            var url = this.GetURL();
            if (!string.IsNullOrWhiteSpace(url))
            {
                url = url.ToLower().Trim();

                if (this.lastUrl != url)
                {
                    changed = true;
                }

                this.lastUrl = url;
            }

            if (changed)
            {
                this.lastUrlChangeTime = DateTime.Now;
                this.lastUrlResponded = false;

                // Post process lastUrl with expansion table
                foreach (var pair in this.unshorteningDictionary)
                {
                    var found = this.lastUrl.IndexOf(pair.Value);
                    // Magic number 12, change it and I will find you irl
                    if (found >= 0 && found < 12)
                    {
                        // This actually replaces ALL occurances of the URL in question, but it *should be fine*
                        this.lastUrl = this.lastUrl.Replace(pair.Key, pair.Value);
                    }
                }
            }

            if (!this.lastUrlResponded && (DateTime.Now - this.lastUrlChangeTime).TotalSeconds > .75)
            {
                this.lastUrlResponded = true;

                // Ignore entries while typing, kind of anyway
                if (this.lastUrl.Length < 6)
                {
                    return;
                }

                var googleMatch = Regex.Match(this.lastUrl, WebBrowserBehaviour.GOOGLE_REGEX, RegexOptions.Compiled);
                if (googleMatch.Success)
                {
                    var search = HttpUtility.UrlDecode(googleMatch.Groups[1].ToString()).Trim();
                    foreach (var resp in this.responseTableGoogle)
                    {
                        if (resp.Key.Contains(search.ToLower().Trim()))
                        {
                            if ((DateTime.Now - resp.Value.Item2) > this.minimumElapsedTime)
                            {
                                window.Say(resp.Value.Item1.Sample());
                                this.responseTableGoogle[resp.Key] = new ResponseTuple(resp.Value.Item1, DateTime.Now);
                            }

                            break;
                        }
                    }
                    return;
                }

                //Holds all responses to be weighted
                var matches = new List<Match>();
                // Url changed, respond accordingly
                foreach (var pair in this.responseTable)
                {
                    if (this.lastUrl.Contains(pair.Key) &&
                        DateTime.Now - pair.Value.Item2 > this.minimumElapsedTime)
                    {
                        // +1 stops it from having 0 sa a weight, *6 to make the specific more likely to happen
                        matches.Add(new Match(pair.Value, (pair.Key.Count(x => x == '/') + 1) * 6, pair.Key));
                    }
                }

                //Don't need to do anything if there aren't any matches
                if (matches.Count == 0) return;

                //If there's only one match, use that
                if (matches.Count == 1)
                {
                    window.Say(matches[0].match.Item1.Sample());
                    this.responseTable[matches[0].key] = new ResponseTuple(matches[0].match.Item1, DateTime.Now);
                    return;
                }

                //Choose, by weight, for more than 1 match
                var maxWeight = matches.Sum(x => x.weight);
                var rand = this.random.Next(0, maxWeight);

                //Shuffles the matches before we start 
                matches.Shuffle();

                //Select a match by weight
                Match selected = new Match();
                foreach (Match m in matches)
                {
                    if (rand < m.weight)
                    {
                        selected = m;
                        break;
                    }

                    rand = rand - m.weight;
                }

                //This should never occur but I'm being extra safe
                if (new Match().Equals(selected)) return;

                window.Say(selected.match.Item1.Sample());
                this.responseTable[selected.key] = new ResponseTuple(selected.match.Item1, DateTime.Now);
            }
        }

        private string GetURL()
        {
            //get the latest url
            return UrlRestServer.URL;
        }

        //Struct for the weighted matches
        private struct Match
        {
            public ResponseTuple match;
            public int weight;
            public string key;

            public Match(ResponseTuple M, int W, string K)
            {
                match = M;
                weight = W;
                key = K;
            }
        }

        //private string GetFirefoxURL()
        //{
        //    try
        //    {
        //        var firefoxProcesses = Process.GetProcessesByName("firefox");
        //        foreach (var firefoxProcess in firefoxProcesses.Where(x => x.MainWindowHandle != IntPtr.Zero))
        //        {
        //            var element = AutomationElement.FromHandle(firefoxProcess.MainWindowHandle);
        //            element = element.FindFirst(TreeScope.Subtree,
        //                new AndCondition(
        //                    new PropertyCondition(AutomationElement.NameProperty, "Search or enter address"),
        //                    new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)));
        //            var url = ((ValuePattern) element.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
        //            return url;
        //        }
        //        return "";
        //    }
        //    catch
        //    {
        //        return "";
        //    }
        //}

        //public string GetChromeURL()
        //{
        //    try
        //    {
        //        var procsChrome = Process.GetProcessesByName("chrome");

        //        if (procsChrome.Length <= 0)
        //        {
        //            return "";
        //        }

        //        foreach (var proc in procsChrome)
        //        {
        //            // the chrome process must have a window 
        //            if (proc.MainWindowHandle == IntPtr.Zero)
        //            {
        //                continue;
        //            }

        //            // to find the tabs we first need to locate something reliable - the 'New Tab' button 
        //            var root = AutomationElement.FromHandle(proc.MainWindowHandle);
        //            var searchBar = root.FindFirst(TreeScope.Descendants,
        //                new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
        //            if (searchBar != null)
        //            {
        //                return (string)searchBar.GetCurrentPropertyValue(ValuePatternIdentifiers.ValueProperty);
        //            }
        //        }

        //        return "";
        //    }
        //    catch
        //    {
        //        return "";
        //    }
        //}
    }
}
