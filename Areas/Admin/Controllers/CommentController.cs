
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyWebApp.Data;
using MyWebApp.Models;


namespace MyWebApp.Areas.Admin.Controllers;


public class CommentController : BaseController
{
    public CommentController(DataContext context) : base(context) { }

    public async Task<IActionResult> Index()
    {
        var comments = await _context.Comments
            .Include(c => c.User)
            .Include(c => c.Product)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return View(comments);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.Status = CommentStatus.Approved;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Hide(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            comment.Status = CommentStatus.Hidden;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
        return RedirectToAction("Index");
    }
}