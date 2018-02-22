using MonikAI.Parsers.Models;
using System.Collections.Generic;

namespace MonikAI.Parsers
{
    /// <summary>
    /// Implement this interface to make it easier to switch between different types of response parsers.
    /// </summary>
    interface IParser
    {
        string GetData(string csvFileName);
        List<DokiResponse> ParseData(string csv);
    }
}