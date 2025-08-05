namespace MyWebApp.Models;

public class Comment
{
    public Guid Id { get; set; }

    public string Content { get; set; }

    public int Rating { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string UserId { get; set; }

    public ApplicationUser User { get; set; }

    public Guid ProductId { get; set; }

    public Product Product { get; set; }
    
    public Guid? ParentCommentId { get; set; }
    public Comment ParentComment { get; set; }
    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}