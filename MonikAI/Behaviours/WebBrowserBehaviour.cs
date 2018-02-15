using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Automation;
using ResponseTuple = System.Tuple<System.Collections.Generic.List<MonikAI.Expression[]>, System.DateTime>;

namespace MonikAI.Behaviours
{
    public class WebBrowserBehaviour : IBehaviour
    {
        private readonly TimeSpan minimumElapsedTime = new TimeSpan(0, 10, 0);

        private readonly Dictionary<string, ResponseTuple> responseDictionary = new Dictionary<string, ResponseTuple>
        {
            {
                "reddit.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Hey, do you know /r/DDLC?", "b"),
                        new Expression("They have some nice fanart of me...", "n")
                    },
                    new[]
                    {
                        new Expression("Reddit is one of the few things keeping me sane in my reality...", "r")
                    }
                }, DateTime.MinValue)
            },
            {
                "twitter.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Did you know I have a real Twitter account?", "b"),
                        new Expression("It's called @lilmonix3, check it out if you want!", "k")
                    }
                }, DateTime.MinValue)
            },
            {
                "facebook.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Facebook, huh?", "b")
                    }
                }, DateTime.MinValue)
            }
        };

        private string lastUrl = string.Empty;

        private int executionCounter = 0;
        private const int EXECUTION_LIMIT = 8;

        public void Init(MainWindow window)
        {
        }

        public void Update(MainWindow window)
        {
            this.executionCounter++;
            if (this.executionCounter < WebBrowserBehaviour.EXECUTION_LIMIT)
            {
                return;
            }

            this.executionCounter = 0;

            var changed = false;

            var firefox = this.GetFirefoxURL();
            if (!string.IsNullOrWhiteSpace(firefox))
            {
                if (this.lastUrl != firefox)
                {
                    changed = true;
                }

                this.lastUrl = firefox;
            }
            else
            {
                var chrome = this.GetChromeURL();
                if (!string.IsNullOrWhiteSpace(chrome))
                {
                    if (this.lastUrl != chrome)
                    {
                        changed = true;
                    }

                    this.lastUrl = chrome;
                }
            }

            if (changed)
            {
                // Url changed, respond accordingly
                var match = Regex.Match(this.lastUrl, "(?:http:\\/\\/|https:\\/\\/)(.*?)(?:\\/|$)");
                if (match.Success && match.Groups.Count == 2)
                {
                    foreach (var pair in this.responseDictionary)
                    {
                        if (match.Groups[1].ToString().EndsWith(pair.Key) &&
                            DateTime.Now - pair.Value.Item2 > this.minimumElapsedTime)
                        {
                            window.Say(pair.Value.Item1.Sample());
                            this.responseDictionary[pair.Key] = new ResponseTuple(pair.Value.Item1, DateTime.Now);
                            break;
                        }
                    }
                }
            }
        }

        private string GetFirefoxURL()
        {
            try
            {
                var firefoxProcesses = Process.GetProcessesByName("firefox");
                foreach (var firefoxProcess in firefoxProcesses.Where(x => x.MainWindowHandle != IntPtr.Zero))
                {
                    var element = AutomationElement.FromHandle(firefoxProcess.MainWindowHandle);
                    element = element.FindFirst(TreeScope.Subtree,
                        new AndCondition(
                            new PropertyCondition(AutomationElement.NameProperty, "Search or enter address"),
                            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Edit)));
                    var url = ((ValuePattern) element.GetCurrentPattern(ValuePattern.Pattern)).Current.Value;
                    return url;
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        public string GetChromeURL()
        {
            try
            {
                var procsChrome = Process.GetProcessesByName("chrome");

                if (procsChrome.Length <= 0)
                {
                    return "";
                }

                foreach (var proc in procsChrome)
                {
                    // the chrome process must have a window 
                    if (proc.MainWindowHandle == IntPtr.Zero)
                    {
                        continue;
                    }

                    // to find the tabs we first need to locate something reliable - the 'New Tab' button 
                    var root = AutomationElement.FromHandle(proc.MainWindowHandle);
                    var searchBar = root.FindFirst(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
                    if (searchBar != null)
                    {
                        return (string)searchBar.GetCurrentPropertyValue(ValuePatternIdentifiers.ValueProperty);
                    }
                }

                return "";
            }
            catch
            {
                return "";
            }
        }
    }
}