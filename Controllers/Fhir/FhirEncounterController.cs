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
/// FHIR R4 Encounter endpoints.
/// Route: /fhir/r4/Encounter
/// Gated by the FHIR feature flag; returns application/fhir+json.
/// </summary>
[ApiController]
[Route("fhir/r4/Encounter")]
[Authorize]
[FeatureGate(FeatureFlags.FHIR)]
public class FhirEncounterController : ControllerBase
{
    private static readonly FhirJsonSerializer Serializer = new(new SerializerSettings { Pretty = false });

    private readonly AppDbContext _db;
    private readonly FhirMappingService _mapper;

    public FhirEncounterController(AppDbContext db, FhirMappingService mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    /// <summary>GET /fhir/r4/Encounter/{id}</summary>
    [HttpGet("{id:int}")]
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    public async Task<ContentResult> GetById(int id)
    {
        var encounter = await _db.Encounters
            .Include(e => e.Patient)
            .Include(e => e.Doctor)
            .Include(e => e.Nurse)
            .Include(e => e.Diagnoses)
            .FirstOrDefaultAsync(e => e.Id == id);

        if (encounter is null)
            return FhirNotFound($"Encounter/{id}");

        return FhirJson(_mapper.ToFhirEncounter(encounter));
    }

    /// <summary>GET /fhir/r4/Encounter?subject=Patient/{patientId}&status=</summary>
    [HttpGet]
    [Authorize(Roles = "Doctor,Nurse,Admin")]
    public async Task<ContentResult> Search(
        [FromQuery] string? subject,
        [FromQuery] string? status)
    {
        var query = _db.Encounters
            .Include(e => e.Patient)
            .Include(e => e.Doctor)
            .Include(e => e.Diagnoses)
            .AsQueryable();

        // subject format: "Patient/42"
        if (!string.IsNullOrWhiteSpace(subject))
        {
            var parts = subject.Split('/');
            if (parts.Length == 2 && int.TryParse(parts[1], out int patientId))
                query = query.Where(e => e.PatientId == patientId);
        }

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(e => e.Status == status);

        var encounters = await query.Take(50).ToListAsync();

        var bundle = new Bundle
        {
            Type  = Bundle.BundleType.Searchset,
            Total = encounters.Count,
            Entry = encounters
                .Select(e => new Bundle.EntryComponent { Resource = _mapper.ToFhirEncounter(e) })
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
