namespace AirbnbClone.Api.Application.DTOs;

public class BookStayDTO
{
    public Guid ListingId { get; set; }
    public DateOnly FromDate { get; set; }
    public DateOnly ToDate { get; set; }
    public int NoOfPeople { get; set; }
}
