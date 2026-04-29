namespace EntryPointApp.Api.Models.Configuration
{
    public class FileStorageSettings
    {
        public string BasePath { get; set; } = "Files/Receipts";
        public long MaxFileSizeBytes { get; set; } = 10_485_760;
        public List<string> AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".pdf"];
    }
}
