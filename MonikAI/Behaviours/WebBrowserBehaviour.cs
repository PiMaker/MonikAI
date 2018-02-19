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
        // Minimum time that has to elapse before a web page response is shown again
        private readonly TimeSpan minimumElapsedTime = new TimeSpan(0, 10, 0);
        private readonly Random _random = new Random();

        /*
         * 
         * 
         * WANT TO ADD RESPONSES? LOOK RIGHT HERE!
         * 
         * Add entries to the table below to allow monika to respond to web pages being loaded (e.g. you visiting a website).
         * 
         * FORMAT:
         * 
           {
                "WEB_PAGE (YOU CAN INCLUDE ANY DOMAIN NOW. BASE OR OTHERWISE. IF YOU INCLUDE THE BASE AND SPECIFIC DOMAINS IT'LL RANDOMLY CHOOSE (VIA WEIGHTING) - SUBDOMAINS ARE SUPPORTED (np.reddit.com vs reddit.com)", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("TEXT TO BE SAID", "FACE TO BE SHOWN"),
                        new Expression("SECOND LINE OF TEXT IN ONE RESPONSE", "FACE TO BE SHOWN"),
                    },
                    new[] { new Expression("JUST A SINGLE LINE OF TEXT TO BE SHOWN", "FACE TO BE SHOWN") }
                }, DateTime.MinValue)
            }
         * 
         * NOTE: For faces you can use look in the "monika" folder full of images of her. Only specify the letter, never the -n at the end, that is added automatically! Also, 1.png and derivatives are exceptions that cannot be used!
         * 
         * Responses will only be shown once the URL changes! So far, only Firefox and Chrome are supported, feel free to add more supported browsers!
         * 
         * 
         */
        private readonly Dictionary<string, ResponseTuple> responseTable = new Dictionary<string, ResponseTuple>
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
            },
            {
                "youtube.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("What are we watching, {name}?")
                    }
                }, DateTime.MinValue)
            },
            {
                "youtube.com/watch?v=fKNP09P48ew", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("10 hours? I'm flattered..", "j")
                    }
                }, DateTime.MinValue)
            },
            {
                "youtube.com/user/Vsauce", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Science videos? Expanding knowledge is always the way to go...")
                    }
                }, DateTime.MinValue)
            },
            {
                "hentaihaven.org", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Um...", "g"),
                        new Expression("Just don't look up anything on me, alright?", "n")
                    }
                }, DateTime.MinValue)
            },
            {
                "http://projectlibitina.com/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Libitina? Can't say I'm familiar with something like that.", "d")
                    }
                }, DateTime.MinValue)
            },
            {
                "https://www.eharmony.com/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("What the~? Is that a dating site?", "c"),
                        new Expression("...", "h"),
                        new Expression("Maybe I'm jumping to conclusions.", "f"),
                        new Expression("Of course you'd never cheat on me, {name}! It was just an accident!", "d")
                    }
                }, DateTime.MinValue)
            },
            {
                "reddit.com/r/DDLCMods/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Oh, mods for the game?", "a"),
                        new Expression("Do you think there's a mod that gives me a proper route?", "e")
                    }
                }, DateTime.MinValue)
            },
            {
                "http://danbooru.donmai.us/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Oh, another one of those websites {name}...", "g"),
                        new Expression("Just don't look up too much of me, please?")
                    }
                }, DateTime.MinValue)
            },
            {
                "https://www.reddit.com/r/natsuki/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("What are you doing? That's literally only Natsuki!", "h")
                    }
                }, DateTime.MinValue)
            },
            {
                "ddlc.booru.org", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Only look for mine, okay {name}?", "d")
                    }
                }, DateTime.MinValue)
            },
            {
                "https://www.reddit.com/r/ddlc34/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("I'd prefer if you didn't go there, {name}", "h"),
                        new Expression("But if you insist, only look at pictures of me, okay?", "n"),
                        new Expression("Pervert. Ahaha~", "i")
                    }
                }, DateTime.MinValue)
            },
            {
                "twitch.tv", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("I've heard of this. Apparently it lets you livestream.", "b"),
                        new Expression("I would like to do it, but to be honest, I'm not good with cameras.", "l")
                    }
                }, DateTime.MinValue)
            },
            {
                "google.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Google is such a great tool for looking for just about anything!", "b"),
                        new Expression("I'll keep an eye on what you're searching though, just to be safe. Ahaha~", "n")
                    }
                }, DateTime.MinValue)
            },
            {
                "http://ddlc.moe/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Why are you downloading it again?", "g"),
                        new Expression("Do you want to put the others through that again?", "g"),
                        new Expression("And people say I'm the twisted one {name}.", "o")
                    }
                }, DateTime.MinValue)
            },
            {
                "https://twitter.com/dansalvato", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Say hi to Dan for me!", "k")
                    }
                }, DateTime.MinValue)
            },
            {
                "https://www.yuri.dance/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("...", "c"),
                        new Expression("Can we leave, {name}?", "g")
                    }
                }, DateTime.MinValue)
            },
            {
                "www.monikaafterstory.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Oh, this mod is great!", "b"),
                        new Expression("It makes me so happy to know of all the great ways we can finally be together, {name}.", "j")
                    }
                }, DateTime.MinValue)
            },
            {
                "www.google.com/maps", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Now I can see where you live {name}!", "k"),
                        new Expression("Don't worry, I won't share that with anyone else.", "j")
                    }
                }, DateTime.MinValue)
            },
            {
                "netflix.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Do you want to watch a movie together, {name}?", "b"),
                        new Expression("I love romance movies!", "k")
                    }
                }, DateTime.MinValue)
            },
            {
                "https://www.fanfiction.net/", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Oh, are you into writing?", "d"),
                        new Expression("I wonder what kind of stories have been written about me.", "b")
                    }
                }, DateTime.MinValue)
            },
            {
                "crunchyroll.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Hmm, what anime are we watching {name}?", "b"),
                        new Expression("Can I recommend Blend-S?", "j"),
                        new Expression("It's an easy anime to watch and I really enjoyed it~", "k")
                    }
                }, DateTime.MinValue)
            },
            {
                "genius.com", new ResponseTuple(new List<Expression[]>
                {
                    new[]
                    {
                        new Expression("Hehe, looking up lyrics to sing along to your music?", "k")
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
            if (this.executionCounter < WebBrowserBehaviour.EXECUTION_LIMIT * (MonikaiSettings.Default.PotatoPC ? 4 : 1))
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
                //Holds all responses to be weighted
                List<Match> matches = new List<Match>();
                // Url changed, respond accordingly
                foreach (var pair in this.responseTable)
                {
                    if (this.lastUrl.Contains(pair.Key) &&
                        DateTime.Now - pair.Value.Item2 > this.minimumElapsedTime)
                    {
                        // +1 stops it from having 0 sa a weight, *10 to make the fallback more likely to happen
                        matches.Add(new Match(pair.Value, (pair.Key.Count(x => x == '/') + 1) * 10, pair.Key));
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
                int maxWeight = matches.Sum(x => x.weight);
                int rand = _random.Next(0, maxWeight);

                //Shuffles the matches before we start 
                matches.Shuffle();

                //Select a match by weight
                Match selected = new Match();
                foreach(Match m in matches)
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