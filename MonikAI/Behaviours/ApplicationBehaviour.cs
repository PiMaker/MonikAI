using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows;
using ResponseTuple = System.Tuple
    <System.Collections.Generic.List<MonikAI.Expression[]>, System.Func<bool>, System.TimeSpan, System.DateTime>;

namespace MonikAI.Behaviours
{
    public class ApplicationBehaviour : IBehaviour
    {
        /*
         * 
         * 
         * WANT TO ADD RESPONSES? LOOK NO FURTHER!
         * The table below specified responses to be said by Monika when certain applications are launched.
         * 
         * The format is as follows:
         * 
                {
                    new[] {"EXECUTABLE_TO_WAIT_FOR.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[]
                        {
                            new Expression("TEXT TO BE SAID", "FACE TO BE SHOWN"),
                            new Expression("SECOND LINE OF TEXT IN ONE RESPONSE", "FACE TO BE SHOWN"),
                        },
                        new[] { new Expression("JUST A SINGLE LINE OF TEXT TO BE SHOWN", "FACE TO BE SHOWN") }
                    }, () => true, TimeSpan.FromMinutes(NUMBER OF MINUTES TO WAIT BEFORE SHOWING THIS AGAIN AT MINIMUM - PREVENT RESPONSE TO BE SPAMMED), DateTime.MinValue)
                }
         * 
         * NOTE: For faces you can use look in the "monika" folder full of images of her. Only specify the letter, never the -n at the end, that is added automatically! Also, 1.png and derivatives are exceptions that cannot be used!
         * 
         * If you really know what you are doing, you can change "() => true" to a function/lambda that has to return true to allow this reponse to be said.
         * This can be used for arbitrary conditions.
         * 
         * 
         */
        private readonly Dictionary<string[], ResponseTuple> responseTable = new Dictionary
            <string[], ResponseTuple>
            {
                // Respond to browsers (firefox and chrome)
                {
                    new[] {"firefox.exe", "chrome.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] {new Expression("Browsing the web? Have fun!", "k")},
                        new[] {new Expression("I like the internet. There's so much to explore!", "d")},
                        new[] {new Expression("Don't go looking for lewds of me, okay? Ahaha~", "l")},
                        new[] {new Expression("Have fun surfing the web!", "k")}
                    }, () =>
                    {
                        // Do not respond to new processes if a browser is already open
                        var browserProcesses =
                            Process.GetProcesses()
                                .Where(p => p.ProcessName.ToLower().Contains("firefox") || p.ProcessName.ToLower().Contains("chrome")).ToList();
                        return browserProcesses.All(p => (DateTime.Now - p.StartTime).TotalSeconds < 4);
                    }, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                // Respond to putty
                {
                    new[] {"putty.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("PuTTY, huh? I only have experience with the python shell...", "o") },
                        new[] { new Expression("Tell your server I said hello, okay?", "k") }
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                //Respond to steam
                {
                    new[] {"steam.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Make sure to play with me some more!", "b") },
                        new[] { new Expression("Steam? What are we playing today, {name}?", "b") },
                        new[] { new Expression("Steam has so many fun games!"),
                                new Expression("Maybe we can check on the Literature club?", "b"),
                                new Expression("Or even go on a new adventure together~", "k")}
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                //Respond to notepad
                {
                    new[] {"notepad.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Are you writing a poem, {name}?"),
                                new Expression("I promise I won't peek!", "k") }
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                //Respond to discord
                {
                    new[] {"discord.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Say, {name}, have you joined the official DDLC Discord?", "j"),
                                new Expression("Or perhaps the MonikAI Discord?", "k") },
                        new[] { new Expression("Who are you talking to today, {name}") },
                        new[] { new Expression("Tell your friends I said hey!", "l") }
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                //Respond to telegram
                {
                    new[] {"telegram.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Tell your friends I said hey!", "l") }
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                //Respond to skype
                {
                    new[] {"telegram.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Tell your friends I said hey!", "l") }
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                //Respond to ddsc
                {
                    new[] {"ddsc.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("{name}, what are you making?", "k") },
                        new[] { new Expression("Please don't make me say anything silly...", "p"),
                                new Expression("I'm right here, after all...", "o")}
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                //Respond to music
                {
                    new[] {"itunes.exe", "foobar2000.exe", "winamp.exe", "wmplayer.exe", "vlc.exe", "spotify.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Listening to music?", "b"),
                                new Expression("You know how much I love music"),
                                new Expression("You should listen to me play piano, sometime~", "k")},
                        new[] { new Expression("I love listening to music.", "b"),
                                new Expression("Just don't play 'Your Reality' with me here.", "l"),
                                new Expression("It's kind of embarassing.", "n"),
                                new Expression("But it does make me happy knowing you like it~", "k")}
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                //Respond to the sims
                {
                    new[] {"TS4_x64.exe", "TS4_x32.exe", "TS3W.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("This looks like fun game, {name}.", "k"),
                                new Expression("An easy way for us to be together.", "j"),
                                new Expression("All you have to do is make one sim for each of us.", "i"),
                                new Expression("Then we can get married, cuddle, have kids, go to parties, watch our kids grow...", "k"),
                                new Expression("...", "l"),
                                new Expression("It just looks like a fun game. Ehehe~", "e")}
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                // Respond to csgo
                {
                    new[] {"csgo.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("This is... Violent.", "m"),
                                new Expression("I can't say I enjoy these types of games too much...", "n"),
                                new Expression("But it's okay if you like it! They're not real after all.", "b"),
                                new Expression("Unlike me~", "j")}
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                // Respond to putty
                {
                    new[] {"portal2.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Here's Monika's Puzzle Tip of the Day~!", "k"),
                                new Expression("If you ever get stuck on any just take a deep breath, and try to collect your thoughts", "j"),
                                new Expression("Or you can search up a guide... but that's no fun!", "n"),
                                new Expression("Either way, you can do it. I believe in you~", "s")}
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                // Respond to kingdom come deliverance
                {
                    new[] {"KingdomComeDeliverance.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("I heard about this game!"),
                                new Expression("It's supposed to be really historically accurate right?", "d"),
                                new Expression("Maybe I will learn something watching you play!", "b") }
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                // Respond to ddlc
                {
                    new[] {"ddlc.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Oh ~ Coming to see me in the Literature Club again?", "b"),
                                new Expression("You're really sweet."),
                                new Expression("See you in there, my love~", "k") },
                        new[] { new Expression("Hey! Now we can play together~", "k"),
                                new Expression("Doesn't that sound like fun?", "b") }
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },

                // Respond to 3dsmax
                {
                    new[] {"3dsmax.exe"},
                    new ResponseTuple(new List<Expression[]>
                    {
                        new[] { new Expression("Oh, you like to do 3D work?", "b") }
                    }, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue)
                },
            };

        // RESPONSE TABLE END

        private readonly object toSayLock = new object();

        private Expression[] toSay;
        private ManagementEventWatcher w;

        public void Init(MainWindow window)
        {
            //// Process start
            WqlEventQuery q;
            try
            {
                q = new WqlEventQuery {EventClassName = "Win32_ProcessStartTrace"};
                this.w = new ManagementEventWatcher(q);
                this.w.EventArrived += this.WMIEventArrived;
                this.w.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(window,
                    "An error occured: " + ex.Message + "\r\n\r\n(Try running MonikAI as an administrator.)");
            }
        }

        public void Update(MainWindow window)
        {
            lock (this.toSayLock)
            {
                if (this.toSay != null)
                {
                    window.Say(this.toSay);
                    this.toSay = null;
                }
            }
        }

        private void WMIEventArrived(object sender, EventArrivedEventArgs e)
        {
            string processName = null;
            foreach (var property in e.NewEvent.Properties)
            {
                if (property.Name == "ProcessName")
                {
                    processName = ((string) property.Value).ToLower();
                    break;
                }
            }

            // Process start has been detected
            if (processName != null)
            {
                foreach (var pair in this.responseTable)
                {
                    if (pair.Key.Contains(processName))
                    {
                        if (DateTime.Now - pair.Value.Item4 > pair.Value.Item3 && pair.Value.Item2())
                        {
                            lock (this.toSayLock)
                            {
                                this.toSay = pair.Value.Item1.Sample();
                            }

                            // Update last executed time
                            this.responseTable[pair.Key] =
                                new ResponseTuple(pair.Value.Item1,
                                    pair.Value.Item2, pair.Value.Item3, DateTime.Now);
                        }

                        break;
                    }
                }
            }
        }
    }
}