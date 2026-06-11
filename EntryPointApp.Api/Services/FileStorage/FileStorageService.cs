using EntryPointApp.Api.Models.Configuration;
using Microsoft.Extensions.Options;

namespace EntryPointApp.Api.Services.FileStorage
{
    public class FileStorageService : IFileStorageService
    {
        private readonly FileStorageSettings _settings;
        private readonly ILogger<FileStorageService> _logger;
        private readonly string _rootPath;

        public FileStorageService(
            IWebHostEnvironment env,
            IOptions<FileStorageSettings> settings,
            ILogger<FileStorageService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
            _rootPath = Path.Combine(env.ContentRootPath, _settings.BasePath);

            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
                _logger.LogInformation("Created receipt storage directory: {Path}", _rootPath);
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, int userId, int weeklyLogId, CancellationToken cancellationToken = default)
        {
            if (file.Length > _settings.MaxFileSizeBytes)
                throw new ArgumentException(
                    $"File size exceeds the maximum allowed size of {_settings.MaxFileSizeBytes / 1_048_576} MB.");

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_settings.AllowedExtensions.Contains(extension))
                throw new ArgumentException(
                    $"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _settings.AllowedExtensions)}");

            var subfolderPath = Path.Combine(_rootPath, userId.ToString(), weeklyLogId.ToString());
            Directory.CreateDirectory(subfolderPath);

            var guidFileName = $"{Guid.NewGuid()}{extension}";
            var fullPath = Path.Combine(subfolderPath, guidFileName);
            var relativePath = Path.Combine(userId.ToString(), weeklyLogId.ToString(), guidFileName);

            await using var stream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await file.CopyToAsync(stream, cancellationToken);

            _logger.LogInformation("Saved receipt file {RelativePath} ({Bytes} bytes)", relativePath, file.Length);

            return relativePath;
        }

        public string GetFilePath(string fileName) => Path.Combine(_rootPath, fileName);

        public bool FileExists(string fileName) => File.Exists(Path.Combine(_rootPath, fileName));

        public void DeleteFile(string fileName)
        {
            var fullPath = Path.Combine(_rootPath, fileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                _logger.LogInformation("Deleted receipt file {FileName}", fileName);
            }
            else
            {
                _logger.LogWarning("Attempted to delete non-existent receipt file {FileName}", fileName);
            }
        }

        public Stream OpenFileStream(string fileName)
        {
            var fullPath = Path.Combine(_rootPath, fileName);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("Receipt file not found on disk.", fileName);

            return new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        }
    }
}
