namespace backend.DTOs;

public class DeleteImpactResponseDto
{
    public int EntityId { get; set; }
    public int AttachmentCount { get; set; }
    public int TagCount { get; set; }
    public int CalendarAppointmentCount { get; set; }
}
