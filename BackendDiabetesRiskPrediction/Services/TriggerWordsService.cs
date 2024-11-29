using System.Text.Json;
using System.IO;

namespace BackendDiabetesRiskPrediction.Services;

public interface ITriggerWordsService
{
    HashSet<string> GetTriggerWords();
    void SaveTriggerWords(HashSet<string> triggerWords);
    HashSet<string> ResetToDefault();
}

public class TriggerWordsService : ITriggerWordsService
{
    private const string TriggerWordsFileName = "triggerwords.json";
    private readonly string _triggerWordsFilePath;
    private readonly ILogger<TriggerWordsService> _logger;
    private static readonly SemaphoreSlim _semaphore = new(1, 1);

    public TriggerWordsService(ILogger<TriggerWordsService> logger)
    {
        _logger = logger;
        _triggerWordsFilePath = Path.Combine("/app/data", TriggerWordsFileName);
        
        // Créer le répertoire s'il n'existe pas
        var directory = Path.GetDirectoryName(_triggerWordsFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    public HashSet<string> GetTriggerWords()
    {
        try
        {
            _semaphore.Wait();
            if (!File.Exists(_triggerWordsFilePath))
            {
                return GetDefaultTriggerWords();
            }

            var json = File.ReadAllText(_triggerWordsFilePath);
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
        
        if (triggerWords.Count == 0)
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
            File.WriteAllText(_triggerWordsFilePath, json);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public HashSet<string> ResetToDefault()
    {
        _logger.LogDebug("ResetToDefault from TriggerWordsService called");
        HashSet<string> defaultWords = GetDefaultTriggerWords();
        SaveTriggerWords(defaultWords);
        return defaultWords;
    }

    public static HashSet<string> GetDefaultTriggerWords() =>
    [
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
    ];
}
