namespace MonikAI
{
    public class Expression
    {
        public string Text { get; set; }
        public string Face { get; set; }

        public Expression(string text, string face)
        {
            this.Text = text;
            this.Face = face;
        }

        public Expression(string text)
        {
            this.Text = text;
            this.Face = "a";
        }
    }
}