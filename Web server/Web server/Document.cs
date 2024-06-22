namespace GithubIssues
{
    public class Document
    {
        public string Text { get; set; }
    }

    public class TransformedDocument : Document
    {
        public float[] Topics { get; set; }
    }
}
