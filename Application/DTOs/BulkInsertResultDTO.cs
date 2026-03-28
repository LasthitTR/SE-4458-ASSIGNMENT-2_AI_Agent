namespace AirbnbClone.Api.Application.DTOs;

public class BulkInsertResultDTO
{
    public int TotalRows { get; set; }
    public int InsertedRows { get; set; }
    public int FailedRows { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}
