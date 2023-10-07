using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

class Program
{
    private static readonly HttpClient httpClient = new HttpClient();

    // LibreTranslate configuration
    private static readonly string libreTranslateUrl = "https://libretranslate.com/translate";
    private static readonly string libreTranslateApiKey = "YOUR_LIBRETRANSLATE_API_KEY";

    // Wordnik configuration
    private static readonly string wordnikApiKey = "YOUR_WORDNIK_API_KEY";
    private static readonly string wordnikDefinitionUrl = "https://api.wordnik.com/v4/word.json/{0}/definitions?api_key=" + wordnikApiKey;
    private static readonly string wordnikFrequencyUrl = "https://api.wordnik.com/v4/word.json/{0}/frequency?api_key=" + wordnikApiKey;

    public static async Task Main(string[] args)
    {
        Console.WriteLine("Input path to your book:");
        string path = Console.ReadLine();
        string text = File.ReadAllText(path);

        char[] delimiters = GetNonAlphaChars(text);
        foreach (char c in delimiters)
        {
            text = text.Replace(c, ' ');
        }

        text = text.ToLower();
        string[] words = text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        List<string> uniqueWords = new List<string>();
        foreach (string word in words)
        {
            if (CountWordOccurrence(word, words) == 1)
            {
                uniqueWords.Add(word);
            }
        }

        Console.WriteLine(uniqueWords.Count);
        uniqueWords.Sort();

        await TranslateAndPrint(uniqueWords);
    }

    private static char[] GetNonAlphaChars(string text)
    {
        HashSet<char> set = new HashSet<char>();
        foreach (char c in text)
        {
            if (!char.IsLetter(c) && c != '\'')
            {
                set.Add(c);
            }
        }
        return set.ToArray();
    }

    private static int CountWordOccurrence(string word, string[] words)
    {
        int count = 0;
        foreach (string w in words)
        {
            if (w == word)
            {
                count++;
            }
        }
        return count;
    }

    private static async Task TranslateAndPrint(List<string> words)
    {
        foreach (string word in words)
        {
            string translatedWord = await Translate(word);
            string definition = await GetWordDefinition(word);
            string difficulty = await GetWordDifficulty(word);

            Console.WriteLine($"{word} -> {translatedWord} | Definition: {definition} | Difficulty: {difficulty}");
        }
    }

    private static async Task<string> Translate(string word)
    {
        var values = new Dictionary<string, string>
        {
            { "q", word },
            { "source", "en" },
            { "target", "zh" }
        };

        var content = new FormUrlEncodedContent(values);

        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {libreTranslateApiKey}");
        var response = await httpClient.PostAsync(libreTranslateUrl, content);
        var responseJson = await response.Content.ReadAsStringAsync();

        JObject jsonResponse = JObject.Parse(responseJson);
        return jsonResponse["translatedText"].ToString();
    }

    private static async Task<string> GetWordDefinition(string word)
    {
        var response = await httpClient.GetStringAsync(string.Format(wordnikDefinitionUrl, word));
        JArray jsonArray = JArray.Parse(response);
        if (jsonArray.Count > 0)
        {
            return jsonArray[0]["text"].ToString();
        }
        return "Not Found";
    }

    private static async Task<string> GetWordDifficulty(string word)
    {
        var response = await httpClient.GetStringAsync(string.Format(wordnikFrequencyUrl, word));
        JObject jsonResponse = JObject.Parse(response);

        if (jsonResponse.ContainsKey("totalCount") && Convert.ToInt32(jsonResponse["totalCount"]) > 0)
        {
            int frequency = Convert.ToInt32(jsonResponse["totalCount"]);
            if (frequency > 1000) return "Easy";
            else if (frequency > 100) return "Medium";
            else return "Hard";
        }

        return "Unknown";
    }
}
