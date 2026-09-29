using Microsoft.AspNetCore.Mvc;

namespace Awai.Controllers
{
    public class BaseController : Controller
    {
        private readonly IWebHostEnvironment _host;

        public BaseController(IWebHostEnvironment host)
        {
            _host = host;
        }

        public string? UploadFile(string folder, IFormFile? file, string? fileUrl, string? isThereFile)
        {
            if (isThereFile == null)
            {
                DeleteOldFile(fileUrl);
                return null;
            }

            if (file != null)
            {
                DeleteOldFile(fileUrl);

                string folderPath = Path.Combine(_host.WebRootPath, "upload", folder);
                if (!Directory.Exists(folderPath))
                    Directory.CreateDirectory(folderPath);

                string fileName = Guid.NewGuid() + "_" + Path.GetFileName(file.FileName);
                string newImageUrl = Path.Combine(folderPath, fileName);

                using (var stream = new FileStream(newImageUrl, FileMode.Create))
                    file.CopyTo(stream);

                return Path.Combine(folder, fileName).Replace("\\", "/");
            }

            return fileUrl;
        }

        public string? SaveApplicationFile(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            string[] allowed = [".jpeg", ".jpg", ".png", ".gif", ".webp", ".bmp", ".pdf"];
            if (!allowed.Contains(extension))
                extension = ".bin";

            var folderPath = Path.Combine(_host.ContentRootPath, "App_Data", "applications");
            Directory.CreateDirectory(folderPath);
            var fileName = Guid.NewGuid().ToString("N") + extension;
            using (var stream = new FileStream(Path.Combine(folderPath, fileName), FileMode.Create))
                file.CopyTo(stream);
            return fileName;
        }

        public static string? ResolveApplicationFile(IWebHostEnvironment host, string? stored)
        {
            if (string.IsNullOrWhiteSpace(stored) || stored.Contains("..") || Path.IsPathRooted(stored))
                return null;

            var fileName = Path.GetFileName(stored.Replace('\\', '/'));
            if (string.IsNullOrWhiteSpace(fileName))
                return null;

            string[] roots =
            [
                Path.Combine(host.ContentRootPath, "App_Data", "applications"),
                Path.Combine(host.WebRootPath, "upload", "applications")
            ];

            foreach (var root in roots)
            {
                var rootFull = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
                var full = Path.GetFullPath(Path.Combine(root, fileName));
                if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (System.IO.File.Exists(full))
                    return full;
            }

            return null;
        }

        public void DeleteOldFile(string? fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            string relativePath = fileUrl.Replace("/", Path.DirectorySeparatorChar.ToString());
            string fullPath = Path.Combine(_host.WebRootPath, "upload", relativePath);

            if (System.IO.File.Exists(fullPath))
            {
                try
                {
                    System.IO.File.Delete(fullPath);
                }
                catch
                {
                }
            }
        }

        public bool CheckImgExtension(IFormFile? img)
        {
            if (img == null)
                return true;

            string fileExtension = Path.GetExtension(img.FileName.ToLower());
            string[] validExtensions = [".jpeg", ".jpg", ".bmp", ".gif", ".png", ".tiff", ".ico", ".webp", ".svg"];
            return validExtensions.Contains(fileExtension);
        }

        public bool CheckDocExtension(IFormFile file)
        {
            string fileExtension = Path.GetExtension(file.FileName.ToLower());
            string[] valid =
            [
                ".jpeg", ".jpg", ".png", ".pdf", ".webp"
            ];
            return valid.Contains(fileExtension);
        }
    }
}
