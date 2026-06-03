using Hl7.Fhir.Model;
using Sanalink.API.Models;
using FhirPatient   = Hl7.Fhir.Model.Patient;
using FhirEncounter = Hl7.Fhir.Model.Encounter;
using SanalinkPatient   = Sanalink.API.Models.Patient;
using SanalinkEncounter = Sanalink.API.Models.Encounter;

namespace Sanalink.API.Services.Fhir;

/// <summary>Converts internal domain models to FHIR R4 resources.</summary>
public class FhirMappingService
{
    private const string PatientSystem   = "http://sanalink.io/patient-id";
    private const string EncounterSystem = "http://sanalink.io/encounter-number";

    public FhirPatient ToFhirPatient(SanalinkPatient p)
    {
        var resource = new FhirPatient
        {
            Id = p.Id.ToString(),
            Identifier = new List<Identifier>
            {
                new Identifier { System = PatientSystem, Value = p.Id.ToString() }
            },
            Name = new List<HumanName>
            {
                new HumanName
                {
                    Use    = HumanName.NameUse.Official,
                    Family = p.LastName,
                    Given  = new[] { p.FirstName, p.MiddleName }
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .Select(n => n!)
                }
            },
            Gender = MapGender(p.Gender),
            BirthDate = p.DateOfBirth.ToString("yyyy-MM-dd")
        };

        if (!string.IsNullOrWhiteSpace(p.Phone))
            resource.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Phone,
                Value  = p.Phone,
                Use    = ContactPoint.ContactPointUse.Mobile
            });

        if (!string.IsNullOrWhiteSpace(p.Email))
            resource.Telecom.Add(new ContactPoint
            {
                System = ContactPoint.ContactPointSystem.Email,
                Value  = p.Email
            });

        return resource;
    }

    public FhirEncounter ToFhirEncounter(SanalinkEncounter e)
    {
        var resource = new FhirEncounter
        {
            Id = e.Id.ToString(),
            Identifier = new List<Identifier>
            {
                new Identifier { System = EncounterSystem, Value = e.EncounterNumber }
            },
            Status = MapEncounterStatus(e.Status),
            Class  = new Coding("http://terminology.hl7.org/CodeSystem/v3-ActCode", "AMB", "ambulatory"),
            Subject = new ResourceReference($"Patient/{e.PatientId}"),
            Period  = new Period
            {
                StartElement = new FhirDateTime(e.CreatedAt),
                EndElement   = e.ClosedAt.HasValue ? new FhirDateTime(e.ClosedAt.Value) : null
            }
        };

        // Doctor participant
        if (e.Doctor != null)
            resource.Participant.Add(new FhirEncounter.ParticipantComponent
            {
                Individual = new ResourceReference($"Practitioner/{e.DoctorId}")
            });

        // ICD-10 diagnoses (populated by EncounterService when ICD10 flag is on)
        if (e.Diagnoses != null)
        {
            foreach (var d in e.Diagnoses.OrderBy(x => x.Rank))
            {
                resource.Diagnosis.Add(new FhirEncounter.DiagnosisComponent
                {
                    Condition = new ResourceReference(),
                    Use = new CodeableConcept(
                        "http://terminology.hl7.org/CodeSystem/diagnosis-role",
                        d.DiagnosisType == "Primary" ? "AD" : "CM",
                        d.DiagnosisType),
                    Rank = d.Rank
                });
                resource.ReasonCode.Add(new CodeableConcept(
                    "http://hl7.org/fhir/sid/icd-10",
                    d.ICD10Code,
                    d.ICD10Description));
            }
        }
        else if (!string.IsNullOrWhiteSpace(e.Diagnosis))
        {
            // Fallback: free-text diagnosis when ICD10 flag is off
            resource.ReasonCode.Add(new CodeableConcept { Text = e.Diagnosis });
        }

        return resource;
    }

    private static AdministrativeGender MapGender(string gender) =>
        gender.ToLower() switch
        {
            "male"   or "homme" or "m" => AdministrativeGender.Male,
            "female" or "femme"  or "f" => AdministrativeGender.Female,
            _ => AdministrativeGender.Unknown
        };

    private static FhirEncounter.EncounterStatus MapEncounterStatus(string status) =>
        status switch
        {
            "Open"       => FhirEncounter.EncounterStatus.Planned,
            "InProgress" => FhirEncounter.EncounterStatus.InProgress,
            "Closed"     => FhirEncounter.EncounterStatus.Finished,
            _            => FhirEncounter.EncounterStatus.Unknown
        };
}
