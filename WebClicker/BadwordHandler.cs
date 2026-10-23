namespace WebClicker;

public class BadwordHandler
{
    public static BadwordHandler Singleton;

    private HashSet<string> wordlist = new();

    public BadwordHandler(string listUrl = "https://www.cs.cmu.edu/~biglou/resources/bad-words.txt")
    {
        Init(listUrl);
        BadwordHandler.Singleton = this;
    }

    private async Task Init(string listUrl)
    {
        using HttpClient client = new HttpClient();
        try
        {
            using Stream stream = await client.GetStreamAsync(listUrl);
            using StreamReader reader = new StreamReader(stream);
            wordlist.Clear();
            List<string> list = new();
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
                if (!string.IsNullOrWhiteSpace(line)) list.Add(line.Trim().ToLower());
            if (list.Count > 0) wordlist = list.ToHashSet();

            Console.WriteLine($"Successfully loaded {wordlist.Count} words.");
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"HttpRequestError: {ex.Message}");
        }
    }

    public bool ContainsBadwords(string input)
    {
        char[] delims = [' ', '.', ',', '!', '?', ';'];
        string[] words = input.Split(delims, StringSplitOptions.RemoveEmptyEntries);
        return words.Any(x => wordlist.Contains(x.ToLower()));
    }
}
