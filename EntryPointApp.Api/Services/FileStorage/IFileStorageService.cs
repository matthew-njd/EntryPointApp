namespace EntryPointApp.Api.Services.FileStorage
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, int userId, int weeklyLogId, CancellationToken cancellationToken = default);
        string GetFilePath(string fileName);
        bool FileExists(string fileName);
        void DeleteFile(string fileName);
        Stream OpenFileStream(string fileName);
    }
}
