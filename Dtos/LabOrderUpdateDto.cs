namespace Sanalink.API.DTOs
{
    public class LabOrderUpdateDto
    {
        public string? TestName { get; set; }
        public string? TestCategory { get; set; }
        public string? Priority { get; set; }
        public string? ClinicalNotes { get; set; }
        public string? Result { get; set; }
        public string? ResultNotes { get; set; }
    }
}
