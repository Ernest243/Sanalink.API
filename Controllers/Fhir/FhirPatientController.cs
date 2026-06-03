using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.FeatureManagement.Mvc;
using Sanalink.API.Data;
using Sanalink.API.Infrastructure;
using Sanalink.API.Services.Fhir;

namespace Sanalink.API.Controllers.Fhir;

/// <summary>
/// FHIR R4 Patient endpoints.
/// Route: /fhir/r4/Patient
/// Gated by the FHIR feature flag; returns application/fhir+json.
/// </summary>
[ApiController]
[Route("fhir/r4/Patient")]
[Authorize]
[FeatureGate(FeatureFlags.FHIR)]
public class FhirPatientController : ControllerBase
{
    private static readonly FhirJsonSerializer Serializer = new(new SerializerSettings { Pretty = false });

    private readonly AppDbContext _db;
    private readonly FhirMappingService _mapper;

    public FhirPatientController(AppDbContext db, FhirMappingService mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    /// <summary>GET /fhir/r4/Patient/{id}</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<ContentResult> GetById(int id)
    {
        var patient = await _db.Patients.FindAsync(id);
        if (patient is null)
            return FhirNotFound($"Patient/{id}");

        return FhirJson(_mapper.ToFhirPatient(patient));
    }

    /// <summary>GET /fhir/r4/Patient?family=&given=</summary>
    [HttpGet]
    [Authorize(Roles = "Doctor,Nurse,Admin,Accueil")]
    public async Task<ContentResult> Search([FromQuery] string? family, [FromQuery] string? given)
    {
        var query = _db.Patients.AsQueryable();

        if (!string.IsNullOrWhiteSpace(family))
            query = query.Where(p => p.LastName.Contains(family));

        if (!string.IsNullOrWhiteSpace(given))
            query = query.Where(p => p.FirstName.Contains(given));

        var patients = await query.Take(50).ToListAsync();

        var bundle = new Bundle
        {
            Type  = Bundle.BundleType.Searchset,
            Total = patients.Count,
            Entry = patients
                .Select(p => new Bundle.EntryComponent { Resource = _mapper.ToFhirPatient(p) })
                .ToList()
        };

        return FhirJson(bundle);
    }

    private ContentResult FhirJson(Resource resource)
        => Content(Serializer.SerializeToString(resource), "application/fhir+json");

    private ContentResult FhirNotFound(string reference)
    {
        Response.StatusCode = StatusCodes.Status404NotFound;
        var outcome = new OperationOutcome
        {
            Issue = new List<OperationOutcome.IssueComponent>
            {
                new OperationOutcome.IssueComponent
                {
                    Severity    = OperationOutcome.IssueSeverity.Error,
                    Code        = OperationOutcome.IssueType.NotFound,
                    Diagnostics = $"{reference} not found"
                }
            }
        };
        return Content(Serializer.SerializeToString(outcome), "application/fhir+json");
    }
}
