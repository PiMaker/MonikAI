using System;

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
    }
}