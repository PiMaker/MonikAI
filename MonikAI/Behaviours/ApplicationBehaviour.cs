using MonikAI.Parsers;
using MonikAI.Parsers.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Windows;
using ResponseTuple = System.Tuple<System.Collections.Generic.List<MonikAI.Expression[]>, System.Func<bool>, System.TimeSpan, System.DateTime>;

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
        private readonly Dictionary<string[], ResponseTuple> responseTable = new Dictionary<string[], ResponseTuple>(new TriggerComparer());

        private readonly object toSayLock = new object();

        private Expression[] toSay;
        private ManagementEventWatcher w;

        private CSVParser parser = new CSVParser();

        public void Init(MainWindow window)
        {
            //// Process start
            WqlEventQuery q;
            try
            {
                // Parse the CSV file
                string csvFile = parser.GetData("dialogue");
                PopulateResponseTable(parser.ParseData(csvFile));

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
                            this.responseTable[pair.Key] = new ResponseTuple(pair.Value.Item1, pair.Value.Item2, pair.Value.Item3, DateTime.Now);
                        }

                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Fills the response table with the currently selected character's triggers and responses from the csv.
        /// </summary>
        /// <param name="characterResponses">A list containing all of the triggers and responses of the current character.</param>
        private void PopulateResponseTable(List<DokiResponse> characterResponses)
        {
            for (int res = 0; res < characterResponses.Count; res++)
            {
                // Convert triggers to array to use as a key for the dictionary
                string[] triggers = characterResponses[res].ResponseTriggers.ToArray();

                // Add every response to the current trigger into a new array to use as a value in the dictionary
                Expression[] responseChain = new Expression[characterResponses[res].ResponseChain.Count];
                for (int chain = 0; chain < characterResponses[res].ResponseChain.Count; chain++)
                {
                    responseChain[chain] = characterResponses[res].ResponseChain[chain];
                }

                // Determine if the trigger is a browser
                bool isBrowserProcess = false;
                for (int i = 0; i < triggers.Length; i++)
                {
                    // If trigger is a browser, only respond if the user recently launched the browser
                    if (triggers[i].Contains("firefox") || triggers[i].Contains("chrome") || triggers[i].Contains("opera"))
                    {
                        isBrowserProcess = true;
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
                if (responseTable.ContainsKey(triggers))
                {
                    triggerResponses = responseTable[triggers].Item1;
                    triggerResponses.Add(responseChain);
                }
                else
                {
                    triggerResponses = new List<Expression[]> { responseChain };
                }

                // If trigger is a browser, only respond if the user recently launched the browser
                if (isBrowserProcess)
                {
                    responseTable[triggers] = new ResponseTuple(triggerResponses, () =>
                    {
                        return Process.GetProcesses()
                        .Where(p => p.ProcessName.ToLower().Contains("firefox") || p.ProcessName.ToLower().Contains("chrome") || p.ProcessName.ToLower().Contains("opera"))
                        .ToList()
                        .All(p => (DateTime.Now - p.StartTime).TotalSeconds < 4);
                    }, TimeSpan.FromMinutes(5), DateTime.MinValue);
                }
                else
                {
                    responseTable[triggers] = new ResponseTuple(triggerResponses, () => true, TimeSpan.FromMinutes(5), DateTime.MinValue);
                }
            }
        }
    }
}