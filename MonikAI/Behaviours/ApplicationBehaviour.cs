// File: ApplicationBehaviour.cs
// Created: 20.02.2018
// 
// See <summary> tags for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using MonikAI.Parsers;
using MonikAI.Parsers.Models;
using ResponseTuple =
    System.Tuple<System.Collections.Generic.List<MonikAI.Expression[]>, System.Func<bool>, System.TimeSpan,
        System.DateTime>;

namespace MonikAI.Behaviours
{
    public class ApplicationBehaviour : IBehaviour
    {
        private readonly CSVParser parser = new CSVParser();

        private readonly Dictionary<string[], ResponseTuple> responseTable =
            new Dictionary<string[], ResponseTuple>(new TriggerComparer());

        private readonly object toSayLock = new object();

        private Expression[] toSay;
        private ManagementEventWatcher w;

        public void Init(MainWindow window)
        {
            // Process start
            WqlEventQuery q;
            try
            {
                // Parse the CSV file
                var csvFile = this.parser.GetData("Applications");
                this.PopulateResponseTable(this.parser.ParseData(csvFile));

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
                var pairsToSample = new List<KeyValuePair<string[],ResponseTuple>>();
                foreach (var pair in this.responseTable)
                {
                    if (pair.Key.Contains(processName.ToLower().Trim()))
                    {
                        if (DateTime.Now - pair.Value.Item4 > pair.Value.Item3 && pair.Value.Item2())
                        {
                            pairsToSample.Add(pair);
                        }
                    }
                }

                if (!pairsToSample.Any())
                {
                    return;
                }

                // Allow multiple multi-app responses to still apply to one single launched app
                var val = pairsToSample.Sample();
                lock (this.toSayLock)
                {
                    this.toSay = val.Value.Item1.Sample();
                }

                // Update last executed time
                this.responseTable[val.Key] = new ResponseTuple(val.Value.Item1, val.Value.Item2,
                    val.Value.Item3, DateTime.Now);
            }
        }

        /// <summary>
        ///     Fills the response table with the currently selected character's triggers and responses from the csv.
        /// </summary>
        /// <param name="characterResponses">A list containing all of the triggers and responses of the current character.</param>
        private void PopulateResponseTable(List<DokiResponse> characterResponses)
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

                Func<bool> triggerFunc = () => true;

                // Determine if the trigger is a browser
                // Opera isn't really supported, but alright
                for (var i = 0; i < triggers.Length; i++)
                {
                    // If trigger is a browser, only respond if the user recently launched the browser
                    if (triggers[i].Contains("firefox") || triggers[i].Contains("chrome") ||
                        triggers[i].Contains("opera"))
                    {
                        triggerFunc = () =>
                        {
                            return Process.GetProcesses()
                                          .Where(p => p.ProcessName.ToLower().Contains("firefox") ||
                                                      p.ProcessName.ToLower().Contains("chrome") ||
                                                      p.ProcessName.ToLower().Contains("opera"))
                                          .ToList()
                                          .All(p => (DateTime.Now - p.StartTime).TotalSeconds < 4);
                        };
                    }
                }

                /* NOTE:
                * This should probably be re-designed.
                * The quick solution to checking if an entry exists would be to just iterate through every pair and then check every trigger in each key
                * which is being done in the eventarrived method.
                * Alternatively, implementing a custom comparer can be done which is what I've done here (see TriggerComparer.cs)
                * I think it would be better to just have an individual process name as a key because duplicating values is more performant than duplicating keys
                * O(1) lookup time is one of the main strengths of using a dictionary in the first place but that is lost when storing an array as a key.
                * Also unrelated, but it might be better to just create a public dictionary that gets populated from the parser class so that this method can be moved to keep this class cleaner.
                */

                // If key already exists in the table, append the new response chain
                List<Expression[]> triggerResponses;
                if (this.responseTable.ContainsKey(triggers))
                {
                    triggerResponses = this.responseTable[triggers].Item1;
                    triggerResponses.Add(responseChain);
                }
                else
                {
                    triggerResponses = new List<Expression[]> {responseChain};
                }

                // If trigger is a browser, only respond if the user recently launched the browser
                this.responseTable[triggers] = new ResponseTuple(triggerResponses, triggerFunc,
                    TimeSpan.FromMinutes(5), DateTime.MinValue);
            }
        }
    }
}
