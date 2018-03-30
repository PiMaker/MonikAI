using System.Collections.Generic;

namespace MonikAI.Parsers.Models
{
    /// <summary>
    /// DokiResponses are objects populated from a parser that are used to populate behavior response tables.
    /// </summary>
    public class DokiResponse
    {
        public List<string> ResponseTriggers { get; set; }
        public List<Expression> ResponseChain { get; set; }

        public DokiResponse()
        {
            ResponseTriggers = new List<string>();
            ResponseChain = new List<Expression>();
        }
    }
}