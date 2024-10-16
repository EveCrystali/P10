using System;
using System.Security.Claims;
using Frontend.Models;
using Newtonsoft.Json;

namespace Frontend.Controllers.Service;

public class PatientService
{
    private readonly HttpClient _httpClient;
    private readonly string _authServiceUrl;

    private readonly IHttpContextAccessor _httpContextAccessor;

    private readonly JwtValidationService _jwtValidationService;
    private readonly ILogger<PatientService> _logger;
    public PatientService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor, IConfiguration configuration, ILogger<PatientService> logger, JwtValidationService jwtValidationService)
    {
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _jwtValidationService = jwtValidationService;
        _authServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Auth");
    }

    public PatientNotesViewModel MapPatientNoteToPatientNotesViewModel(Patient patient, List<Note> notes)
    {
        PatientNotesViewModel patientNotesViewModel = new()
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
        return patientNotesViewModel;

    }

    // DONE: Implement a mapping method for PatientNotesViewModel to Patient
    public Patient MapPatientNotesViewModelToPatient(PatientNotesViewModel patientNotesViewModel)
    {
        Patient patient = new()
        {
            Id = patientNotesViewModel.PatientId,
            FirstName = patientNotesViewModel.FirstName,
            LastName = patientNotesViewModel.LastName,
            DateOfBirth = patientNotesViewModel.DateOfBirth,
            Gender = patientNotesViewModel.Gender,
            Address = patientNotesViewModel.Address,
            PhoneNumber = patientNotesViewModel.PhoneNumber
        };
        return patient;
    }

    // DONE: Implement a mapping method for PatientNotesViewModel to Note

    public List<Note> MapPatientNotesViewModelToNotes(PatientNotesViewModel patientNotesViewModel)
    {
        List<Note> notes = patientNotesViewModel.Notes
            .Select(note => new Note
            {
                Id = note.Id,
                PractionnerId = note.PractionnerId,
                PatientId = patientNotesViewModel.PatientId,
                CreatedDate = note.CreatedDate,
                LastUpdatedDate = note.LastUpdatedDate,
                Title = note.Title,
                Body = note.Body
            })
            .ToList();
        return notes;
    }


    // TODO: Include this method in Note Edition and Details views
    // Note:  in creation it is not needed as the user name will be the current user 
    public string? GetPractitionnerUserName()
    {
        // DONE: implement this
        string? tokenSerialized = _httpContextAccessor.HttpContext?.Request.Cookies["AuthTokens"];

        if (tokenSerialized != null)
        {
            AuthToken? authToken = JsonConvert.DeserializeObject<AuthToken>(tokenSerialized);
            if (authToken != null && !string.IsNullOrEmpty(authToken.Token))
            {
                ClaimsPrincipal? principal = _jwtValidationService.ValidateToken(authToken.Token);
                if (principal != null)
                {
                    string username = principal.Identity?.Name ?? principal.Claims.FirstOrDefault(c => c.Type == "unique_name")?.Value ?? "defaultNullUsername";
                    return username;
                }
            }
        }
        return "defaultNullUsername";
    }

    // DONE: Implement a new method returning the PractionnerId of the current user
    public async Task<string?> GetUserIdFromAuthToken()
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

        HttpRequestMessage request = new(HttpMethod.Get, $"https://localhost:7201/user/username/{username}");

        try
        {
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError($"Failed to retrieve user ID. Status code: {response.StatusCode}");
                return null;
            }

            string? userId = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogError("Received empty user ID.");
                return null;
            }

            return userId;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error occurred while retrieving user ID: {ex.Message}");
            return null;
        }
    }

}

