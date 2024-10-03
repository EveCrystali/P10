using Frontend.Controllers.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Frontend.Controllers
{
    [Route("patient")]
    public class PatientsController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<PatientsController> _logger;
        private readonly string _patientServiceUrl;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PatientsController(
            ILogger<PatientsController> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _patientServiceUrl = new ServiceUrl(configuration, _logger).GetServiceUrl("Patient");
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Helper method to create HttpRequestMessage with authentication cookie
        /// </summary>
        private HttpRequestMessage CreateRequest(HttpMethod method, string url, HttpContent? content = null)
        {
            var request = new HttpRequestMessage(method, url);
            if (content != null)
            {
                request.Content = content;
            }

            // Extraire le cookie d'authentification de la requête entrante
            var authCookie = _httpContextAccessor.HttpContext?.Request.Cookies["P10AuthCookie"];
            if (!string.IsNullOrEmpty(authCookie))
            {
                request.Headers.Add("Cookie", $"P10AuthCookie={authCookie}");
                _logger.LogInformation("Authentication cookie added to the request: {Cookie}", authCookie);
            }
            else
            {
                _logger.LogWarning("No authentication cookie found in the incoming request.");
            }

            return request;
        }

        public async Task<IActionResult> Index()
        {
            var client = _httpClientFactory.CreateClient();
            var request = CreateRequest(HttpMethod.Get, _patientServiceUrl);
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                List<Frontend.Models.Patient>? patients = await response.Content.ReadFromJsonAsync<List<Frontend.Models.Patient>>();
                if (patients != null)
                {
                    foreach (Frontend.Models.Patient patient in patients)
                    {
                        Console.WriteLine($"Patients: {patient.Id} {patient.FirstName} {patient.LastName}");
                    }
                }

                return View(patients);
            }

            ModelState.AddModelError(string.Empty, "Unable to load patients.");
            return View(new List<Frontend.Models.Patient>());
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient();
            var request = CreateRequest(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                return View(patient);
            }

            ModelState.AddModelError(string.Empty, "Patient not found.");
            return View();
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create(Frontend.Models.Patient patient)
        {
            if (ModelState.IsValid)
            {
                var client = _httpClientFactory.CreateClient();
                var content = JsonContent.Create(patient);
                var request = CreateRequest(HttpMethod.Post, _patientServiceUrl, content);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Frontend.Models.Patient? createdPatient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                    if (createdPatient != null)
                    {
                        return RedirectToAction(nameof(Details), new { id = createdPatient.Id });
                    }
                    else
                    {
                        return RedirectToAction(nameof(Index), "Home");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Error from the server");
                    _logger.LogError("Error from the server : {ReasonPhrase}", response.ReasonPhrase);
                    return RedirectToAction(nameof(Index), "Home");
                }
            }
            else
            {
                _logger.LogError("Model state is not valid.");
                ModelState.AddModelError(string.Empty, "Unable to create patient.");
                return View(patient);
            }
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            if (ModelState.IsValid)
            {
                var client = _httpClientFactory.CreateClient();
                var request = CreateRequest(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                    return View(patient);
                }
                else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    ModelState.AddModelError(string.Empty, "Patient not found.");
                    return View();
                }

                ModelState.AddModelError(string.Empty, "Unable to load patient for edit.");
                return View();
            }
            else
            {
                _logger.LogError("Model state is not valid.");
                ModelState.AddModelError(string.Empty, "Unable to load patient for edit.");
                return View();
            }
        }

        [HttpPost("edit/{id}")]
        public async Task<IActionResult> Edit(Frontend.Models.Patient patient)
        {
            if (ModelState.IsValid)
            {
                _logger.LogInformation("Updating patient with id {PatientId} to {Patient}", patient.Id, patient);
                var client = _httpClientFactory.CreateClient();
                var content = JsonContent.Create(patient);
                var request = CreateRequest(HttpMethod.Put, $"{_patientServiceUrl}/{patient.Id}", content);
                HttpResponseMessage response = await client.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Patient with id {PatientId} was successfully updated.", patient.Id);
                    return RedirectToAction(nameof(Details), new { id = patient.Id });
                }
                else
                {
                    _logger.LogError("Failed to update patient with id {PatientId}. Status code: {StatusCode}", patient.Id, response.StatusCode);
                }
            }
            else
            {
                _logger.LogError("Model state is not valid.");
            }

            ModelState.AddModelError(string.Empty, "Unable to update patient.");
            return View(patient);
        }

        [HttpGet("delete/{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient();
            var request = CreateRequest(HttpMethod.Get, $"{_patientServiceUrl}/{id}");
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                Frontend.Models.Patient? patient = await response.Content.ReadFromJsonAsync<Frontend.Models.Patient>();
                return View(patient);
            }

            ModelState.AddModelError(response.StatusCode.ToString(), "Unable to load patient for deletion.");
            return View();
        }

        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var client = _httpClientFactory.CreateClient();
            var request = CreateRequest(HttpMethod.Delete, $"{_patientServiceUrl}/{id}");
            HttpResponseMessage response = await client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete patient with id {PatientId}. Status code: {StatusCode}, Error: {Error}", id, response.StatusCode, errorContent);
                ModelState.AddModelError(string.Empty, "Unable to delete patient.");
                TempData["Error"] = ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage;
                return RedirectToAction("Index", "Home");
            }
        }
    }
}
