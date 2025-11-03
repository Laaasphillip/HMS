using HMS.Models;

public class Leave
{
    public int Id { get; set; }
    public int staffId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string LeaveType { get; set; }
    public string? Description { get; set; }

    public virtual Staff Staff { get; set; }

}