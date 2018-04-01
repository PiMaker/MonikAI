using System;
using System.Text.RegularExpressions;

namespace MonikAI
{
    public class Expression
    {
        public string Text { get; set; }
        public string Face { get; set; }

        public event EventHandler Executed;

        public Expression(string text, string face)
        {
            if (string.IsNullOrWhiteSpace(face))
            {
                face = "a";
            }
            else
            {
                // Numbers are pretty common because people can't read instructions,
                // let's remove them to at least use the face part of their expressions
                face = Regex.Replace(face, @"[0-9]", "");
            }

            if (face.Length > 1 || !Regex.IsMatch(face, @"[a-s]"))
            {
                face = "a";
            }

            this.Text = text;
            this.Face = face;
        }

        public Expression(string text)
        {
            this.Text = text;
            this.Face = "a";
        }

        public void OnExecuted()
        {
            this.Executed?.Invoke(this, EventArgs.Empty);
        }

        public Expression AttachEvent(EventHandler eh)
        {
            this.Executed += eh;
            return this;
        }
    }
}