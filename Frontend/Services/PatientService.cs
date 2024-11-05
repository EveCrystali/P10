using System.Security.Claims;
using Frontend.Models;
using Newtonsoft.Json;
namespace Frontend.Services;

public class PatientService(IHttpContextAccessor httpContextAccessor, ILogger<PatientService> logger, JwtValidationService jwtValidationService)
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly JwtValidationService _jwtValidationService = jwtValidationService;
    private readonly ILogger<PatientService> _logger = logger;

    public static PatientViewModel MapPatientNoteToPatientNotesViewModel(Patient patient, List<Note> notes)
    {
        PatientViewModel patientViewModel = new()
        {
            PatientId = patient.Id,
            FirstName = patient.FirstName,
            LastName = patient.LastName,
            DateOfBirth = patient.DateOfBirth,
            Gender = patient.Gender,
            Address = patient.Address ?? "",
            PhoneNumber = patient.PhoneNumber ?? "",
            Notes = notes
        };
        return patientViewModel;
    }

    public static Patient MapPatientNotesViewModelToPatient(PatientViewModel patientViewModel)
    {
        Patient patient = new()
        {
            Id = patientViewModel.PatientId,
            FirstName = patientViewModel.FirstName,
            LastName = patientViewModel.LastName,
            DateOfBirth = patientViewModel.DateOfBirth,
            Gender = patientViewModel.Gender,
            Address = patientViewModel.Address,
            PhoneNumber = patientViewModel.PhoneNumber
        };
        return patient;
    }

    public List<Note> MapPatientNotesViewModelToNotes(PatientViewModel patientViewModel)
    {
        if (patientViewModel.Notes == null) return [];

        List<Note>? notes = patientViewModel?.Notes
                                            .Select(note => new Note
                                            {
                                                Id = note.Id,
                                                Creator = note.Creator,
                                                PatientId = patientViewModel.PatientId,
                                                CreatedDate = note.CreatedDate,
                                                LastUpdatedDate = note.LastUpdatedDate,
                                                Title = note.Title,
                                                Body = note.Body
                                            })
                                            .ToList();
        return notes;
    }

    public async Task<string?> GetUsernameFromAuthToken()
    {
        if (_httpContextAccessor.HttpContext == null)
        {
            _logger.LogError("HttpContext is null.");
            return null;
        }

        string? tokenSerialized = _httpContextAccessor.HttpContext.Request.Cookies["AuthTokens"];

        if (string.IsNullOrEmpty(tokenSerialized))
        {
            _logger.LogError("No AuthToken found in cookies.");
            return null;
        }

        AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized);
        if (authToken == null || string.IsNullOrEmpty(authToken.Token))
        {
            _logger.LogError("AuthToken is null or token is empty.");
            return null;
        }

        ClaimsPrincipal? principal = _jwtValidationService.ValidateToken(authToken.Token);
        if (principal == null)
        {
            _logger.LogError("Failed to validate JWT token. Principal is null.");
            return null;
        }

        string username = principal.Identity?.Name ?? principal.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value ?? "defaultNullUsername";

        if (string.IsNullOrEmpty(username) || username == "defaultNullUsername")
        {
            _logger.LogError("Username is null or defaultNullUsername.");
            return null;
        }

        return username;
    }
}