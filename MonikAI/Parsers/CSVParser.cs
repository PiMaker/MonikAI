using MonikAI.Parsers.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace MonikAI.Parsers
{
    class CSVParser : IParser
    {
        /// <summary>
        /// Retrieves the csv data containing Monika's responses and triggers.
        /// </summary>
        /// <param name="csvFileName">Name of the csv file to be parsed.</param>
        /// <returns>A string containing the path to the csv data or null if there is no csv file to load.</returns>
        public string GetData(string csvFileName)
        {
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MonikAI\\" + csvFileName + ".csv");

            if (!File.Exists(path))
            {
                return null;
            }

            return path;
        }

        /// <summary>
        /// Traverses a csv file delimited by semi-colons and populates a list of responses and their triggers.
        /// </summary>
        /// <param name="csvPath">The full path to the csv file.</param>
        /// <returns>A list of DokiResponses which contain a character's expressions and the triggers to those expressions</returns>
        public List<DokiResponse> ParseData(string csvPath)
        {
            if (string.IsNullOrWhiteSpace(csvPath))
            {
                return null;
            }

            using (var reader = new StreamReader(csvPath))
            {
                reader.ReadLine(); // Skip first line with instructions

                List<DokiResponse> characterResponses = new List<DokiResponse>();

                List<string> numColumns = new List<string>(); // A list containing all of the columns in the process sheet

                // Parse all csv column headings
                var headings = reader.ReadLine().Split(';');

                foreach (string heading in headings)
                {
                    if (!string.IsNullOrWhiteSpace(heading))
                    {
                        numColumns.Add(heading);
                    }
                }

                List<string> processResponses = new List<string>();

                // Parse process responses
                while (!reader.EndOfStream)
                {
                    DokiResponse res = new DokiResponse();

                    var row = reader.ReadLine();
                    var columns = row.Split(';');

                    // Skip columns[0] because it only contains the editing notes

                    // Separate response triggers by comma in case there are multiple triggers to the current response
                    var responseTriggers = columns[1].Split(',');
                    foreach (string trigger in responseTriggers)
                    {
                        if (!string.IsNullOrWhiteSpace(trigger))
                        {
                            res.ResponseTriggers.Add(trigger.Trim());
                        }
                    }

                    // Get text/face pairs
                    for (int textCell = 2; textCell < columns.Length - 1; textCell += 2)
                    {
                        if (!string.IsNullOrWhiteSpace(columns[textCell]))
                        {
                            res.ResponseChain.Add(new Expression(columns[textCell], columns[textCell + 1]));
                        }
                    }

                    // Add response to list
                    characterResponses.Add(res);
                }

                return characterResponses;
            }
        }
    }
}