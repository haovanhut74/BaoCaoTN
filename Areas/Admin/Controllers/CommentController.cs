using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Areas.Permission;
using MyWebApp.Data;
using MyWebApp.Models;
using MyWebApp.ViewModels;


namespace MyWebApp.Areas.Admin.Controllers;

[Area("Admin")]
[HasPermission("ManageComment")]
public class CommentController : BaseController
{
    public CommentController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var groupedComments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Product)
            .GroupBy(c => c.UserId)
            .Select(g => new CommentSummaryViewModel
            {
                UserId = g.Key,
                UserName = g.First().User.UserName,
                LatestComment = g.OrderByDescending(c => c.CreatedAt).First().Content,
                ProductName = g.OrderByDescending(c => c.CreatedAt).First().Product.Name,
                LatestTime = g.Max(c => c.CreatedAt),
                TotalComments = g.Count(),
                LatestCommentId = g.OrderByDescending(c => c.CreatedAt).First().Id
            })
            .OrderByDescending(x => x.LatestTime)
            .ToListAsync();

        return View(groupedComments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("ApproveComment")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.Status = CommentStatus.Approved;
            await _context.SaveChangesAsync();
            return RedirectToAction("Detail", new { userId = comment.UserId });
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("HidenComment")]
    public async Task<IActionResult> Hide(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.Status = CommentStatus.Hidden;
            await _context.SaveChangesAsync();
            return RedirectToAction("Detail", new { userId = comment.UserId });
        }

        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [HasPermission("DeleteComment")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var comment = await _context.Comments
            .Include(c => c.Replies) // giả sử bạn có navigation property Replies
            .FirstOrDefaultAsync(c => c.Id == id);

        if (comment == null) return RedirectToAction("Index");

        DeleteCommentWithReplies(comment);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    private void DeleteCommentWithReplies(Comment comment)
    {
        // Đệ quy xóa tất cả replies
        foreach (var reply in _context.Comments.Where(c => c.ParentCommentId == comment.Id).ToList())
        {
            DeleteCommentWithReplies(reply);
        }

        _context.Comments.Remove(comment);
    }


    public async Task<IActionResult> Detail(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return NotFound();

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return NotFound();

        var comments = await _context.Comments
            .Where(c => c.UserId == userId)
            .Include(c => c.Product)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        ViewBag.User = user;
        return View(comments);
    }
}