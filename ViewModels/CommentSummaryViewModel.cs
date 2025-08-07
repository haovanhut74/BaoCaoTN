namespace MyWebApp.ViewModels;

public class CommentSummaryViewModel
{
    public string UserId { get; set; }
    public string? UserName { get; set; }
    public string LatestComment { get; set; }
    public string ProductName { get; set; }
    public DateTime LatestTime { get; set; }
    public int TotalComments { get; set; }
    public Guid LatestCommentId { get; set; }
}