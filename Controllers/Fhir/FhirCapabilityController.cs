using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Sanalink.API.Infrastructure;

namespace Sanalink.API.Controllers.Fhir;

/// <summary>
/// GET /fhir/r4/metadata — FHIR CapabilityStatement describing this server's conformance.
/// Gated by the FHIR feature flag.
/// </summary>
[ApiController]
[Route("fhir/r4")]
[FeatureGate(FeatureFlags.FHIR)]
public class FhirCapabilityController : ControllerBase
{
    private static readonly FhirJsonSerializer Serializer = new(new SerializerSettings { Pretty = false });

    [HttpGet("metadata")]
    public ContentResult GetCapabilityStatement()
    {
        var cs = new CapabilityStatement
        {
            Status      = PublicationStatus.Active,
            Date        = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Kind        = CapabilityStatementKind.Instance,
            FhirVersion = FHIRVersion.N4_0_1,
            Format      = new[] { "application/fhir+json" },
            Software    = new CapabilityStatement.SoftwareComponent { Name = "Sanalink API" },
            Rest        = new List<CapabilityStatement.RestComponent>
            {
                new CapabilityStatement.RestComponent
                {
                    Mode = CapabilityStatement.RestfulCapabilityMode.Server,
                    Resource = new List<CapabilityStatement.ResourceComponent>
                    {
                        FhirResource("Patient",
                            CapabilityStatement.TypeRestfulInteraction.Read,
                            CapabilityStatement.TypeRestfulInteraction.SearchType),
                        FhirResource("Encounter",
                            CapabilityStatement.TypeRestfulInteraction.Read,
                            CapabilityStatement.TypeRestfulInteraction.SearchType),
                    }
                }
            }
        };

        return Content(Serializer.SerializeToString(cs), "application/fhir+json");
    }

    private static CapabilityStatement.ResourceComponent FhirResource(
        string typeName, params CapabilityStatement.TypeRestfulInteraction[] interactions) =>
        new CapabilityStatement.ResourceComponent
        {
            TypeElement = new Code(typeName),
            Interaction = interactions
                .Select(i => new CapabilityStatement.ResourceInteractionComponent { Code = i })
                .ToList()
        };
}
