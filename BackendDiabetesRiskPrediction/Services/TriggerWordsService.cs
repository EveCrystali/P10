using System.Text.Json;

namespace BackendDiabetesRiskPrediction.Services;

public interface ITriggerWordsService
{
    HashSet<string> GetTriggerWords();
    void SaveTriggerWords(HashSet<string> triggerWords);
    HashSet<string> ResetToDefault();
}

public class TriggerWordsService : ITriggerWordsService
{
    private const string TriggerWordsFilePath = "triggerwords.json";
    private readonly ILogger<TriggerWordsService> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public TriggerWordsService(ILogger<TriggerWordsService> logger)
    {
        _logger = logger;
    }

    public HashSet<string> GetTriggerWords()
    {
        try
        {
            _semaphore.Wait();
            if (!File.Exists(TriggerWordsFilePath))
            {
                return GetDefaultTriggerWords();
            }

            var json = File.ReadAllText(TriggerWordsFilePath);
            return JsonSerializer.Deserialize<HashSet<string>>(json) ?? GetDefaultTriggerWords();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de la lecture des mots déclencheurs, utilisation des mots par défaut");
            return GetDefaultTriggerWords();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public void SaveTriggerWords(HashSet<string> triggerWords)
    {
        ArgumentNullException.ThrowIfNull(triggerWords);
        
        if (!triggerWords.Any())
        {
            throw new ArgumentException("La liste des mots déclencheurs ne peut pas être vide", nameof(triggerWords));
        }

        if (triggerWords.Any(word => string.IsNullOrWhiteSpace(word)))
        {
            throw new ArgumentException("Les mots déclencheurs ne peuvent pas être vides", nameof(triggerWords));
        }

        try
        {
            _semaphore.Wait();
            var json = JsonSerializer.Serialize(triggerWords);
            File.WriteAllText(TriggerWordsFilePath, json);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public HashSet<string> ResetToDefault()
    {
        var defaultWords = GetDefaultTriggerWords();
        SaveTriggerWords(defaultWords);
        return defaultWords;
    }

    public static HashSet<string> GetDefaultTriggerWords() => new()
    {
        "Hémoglobine A1C",
        "Microalbumine",
        "Taille",
        "Poids",
        "Fumeur",
        "Anormal",
        "Cholestérol",
        "Vertiges",
        "Rechute",
        "Réaction",
        "Anticorps"
    };
}
