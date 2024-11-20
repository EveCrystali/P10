using System.Security.Claims;
using Frontend.Models;
using Newtonsoft.Json;
namespace Frontend.Services;

public class PatientService(IHttpContextAccessor httpContextAccessor, ILogger<PatientService> logger, JwtValidationService jwtValidationService)
{

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

    public async Task<string?> GetUsernameFromAuthToken()
    {
        if (httpContextAccessor.HttpContext == null)
        {
            logger.LogError("HttpContext is null.");
            return null;
        }

        string? tokenSerialized = httpContextAccessor.HttpContext.Request.Cookies["AuthTokens"];

        if (string.IsNullOrEmpty(tokenSerialized))
        {
            logger.LogError("No AuthToken found in cookies.");
            return null;
        }

        AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized);
        if (authToken == null || string.IsNullOrEmpty(authToken.Token))
        {
            logger.LogError("AuthToken is null or token is empty.");
            return null;
        }

        ClaimsPrincipal? principal = jwtValidationService.ValidateToken(authToken.Token);
        if (principal == null)
        {
            logger.LogError("Failed to validate JWT token. Principal is null.");
            return null;
        }

        string username = principal.Identity?.Name ?? principal.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value ?? "defaultNullUsername";

        if (string.IsNullOrEmpty(username) || username == "defaultNullUsername")
        {
            logger.LogError("Username is null or defaultNullUsername.");
            return null;
        }

        return username;
    }
}