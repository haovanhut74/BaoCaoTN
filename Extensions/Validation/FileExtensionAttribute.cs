using System.ComponentModel.DataAnnotations;

namespace MyWebApp.Extensions.Validation;

public class FileExtensionAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object value, ValidationContext validationContext)
    {
        if (value is not IFormFile file)
        {
            return new ValidationResult("Invalid file.");
        }

        if (file.Length == 0)
        {
            return new ValidationResult("File cannot be empty.");
        }

        if (file.Length > 5 * 1024 * 1024) // 5 MB limit
        {
            return new ValidationResult("File size must not exceed 5 MB.");
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(fileExtension))
        {
            return new ValidationResult($"File type '{fileExtension}' is not allowed. Allowed types are: {string.Join(", ", allowedExtensions)}.");
        }

        return ValidationResult.Success;
    }
}
