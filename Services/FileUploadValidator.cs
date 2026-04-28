using Microsoft.AspNetCore.Http;

namespace Projectpath.Services
{
    public static class FileUploadValidator
    {
        public const long MaxFileSizeBytes = 10 * 1024 * 1024;

        private static readonly HashSet<string> BlockedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".exe", ".bat", ".cmd", ".com", ".scr", ".msi", ".ps1", ".vbs", ".js", ".jar", ".dll"
        };

        public static bool IsValid(IFormFile? file, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (file == null || file.Length == 0)
            {
                return true;
            }

            if (file.Length > MaxFileSizeBytes)
            {
                errorMessage = "File size must be 10MB or less.";
                return false;
            }

            var extension = Path.GetExtension(file.FileName);
            if (BlockedExtensions.Contains(extension))
            {
                errorMessage = "This file type is not allowed for security reasons.";
                return false;
            }

            return true;
        }

        public static async Task<(string FileName, string RelativePath)> SaveAsync(IFormFile file, IWebHostEnvironment environment, string folderName)
        {
            var uploadsFolder = Path.Combine(environment.WebRootPath, "uploads", folderName);
            Directory.CreateDirectory(uploadsFolder);

            var originalFileName = Path.GetFileName(file.FileName);
            var extension = Path.GetExtension(originalFileName);
            var safeFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(uploadsFolder, safeFileName);

            using var stream = new FileStream(fullPath, FileMode.Create);
            await file.CopyToAsync(stream);

            var relativePath = $"/uploads/{folderName}/{safeFileName}";
            return (originalFileName, relativePath);
        }
    }
}