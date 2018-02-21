using System;

namespace MonikAI
{
    public class UpdateConfig
    {
        public int ProgramVersion { get; set; }

        public int ResponsesVersion { get; set; }

        public string ProgramURL { get; set; }

        public string[] ResponseURLs { get; set; }

        public UpdateConfig(int programVersion, int responsesVersion, string programURL, string[] responseURLs)
        {
            this.ProgramVersion = programVersion;
            this.ResponsesVersion = responsesVersion;
            this.ProgramURL = programURL;
            this.ResponseURLs = responseURLs;
        }
    }
}