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
                }
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