using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PGKing.UI.Services
{
    public class StorageService : IStorageService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;
        private readonly bool _useS3;
        private readonly string? _s3ServiceUrl;
        private readonly string? _s3Region;
        private readonly string? _s3AccessKeyId;
        private readonly string? _s3SecretAccessKey;
        private readonly string? _s3BucketName;
        private readonly string? _s3PublicUrlBase;

        public StorageService(IWebHostEnvironment environment, IConfiguration configuration)
        {
            _environment = environment;
            _configuration = configuration;

            _useS3 = _configuration.GetValue<bool>("StorageSettings:UseS3");
            
            if (_useS3)
            {
                _s3ServiceUrl = _configuration["StorageSettings:S3:ServiceUrl"] ?? throw new ArgumentNullException("StorageSettings:S3:ServiceUrl is not configured");
                _s3Region = _configuration["StorageSettings:S3:Region"] ?? "us-east-1";
                _s3AccessKeyId = _configuration["StorageSettings:S3:AccessKeyId"] ?? throw new ArgumentNullException("StorageSettings:S3:AccessKeyId is not configured");
                _s3SecretAccessKey = _configuration["StorageSettings:S3:SecretAccessKey"] ?? throw new ArgumentNullException("StorageSettings:S3:SecretAccessKey is not configured");
                _s3BucketName = _configuration["StorageSettings:S3:BucketName"] ?? throw new ArgumentNullException("StorageSettings:S3:BucketName is not configured");
                _s3PublicUrlBase = _configuration["StorageSettings:S3:PublicUrlBase"] ?? throw new ArgumentNullException("StorageSettings:S3:PublicUrlBase is not configured");
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subFolder)
        {
            if (file == null || file.Length == 0)
            {
                return string.Empty;
            }

            string fileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);

            if (_useS3)
            {
                // Upload to Supabase Storage S3
                var config = new AmazonS3Config
                {
                    ServiceURL = _s3ServiceUrl,
                    ForcePathStyle = true // Supabase S3 requires ForcePathStyle
                };

                using (var client = new AmazonS3Client(_s3AccessKeyId, _s3SecretAccessKey, config))
                {
                    using (var stream = file.OpenReadStream())
                    {
                        var key = $"{subFolder}/{fileName}".Replace("\\", "/");
                        var request = new PutObjectRequest
                        {
                            BucketName = _s3BucketName,
                            Key = key,
                            InputStream = stream,
                            ContentType = file.ContentType
                        };

                        await client.PutObjectAsync(request);
                        
                        // Construct the public URL for Supabase Storage
                        // Format: {PublicUrlBase}/{BucketName}/{Key}
                        return $"{_s3PublicUrlBase!.TrimEnd('/')}/{_s3BucketName}/{key}";
                    }
                }
            }
            else
            {
                // Save locally
                string uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", subFolder);
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string filePath = Path.Combine(uploadFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                return "/uploads/" + subFolder + "/" + fileName;
            }
        }

        public async Task DeleteFileAsync(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl)) return;

            // Check if it's an S3 URL or a local URL
            if (fileUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || fileUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                if (!_useS3) return; // Cannot delete S3 files if S3 credentials/S3 storage is disabled
                
                try
                {
                    var uri = new Uri(fileUrl);
                    var segments = uri.Segments.Select(s => s.Trim('/')).Where(s => !string.IsNullOrEmpty(s)).ToList();
                    
                    // S3 structure: /storage/v1/object/public/[bucket]/[key]
                    int bucketIndex = segments.IndexOf(_s3BucketName!);
                    if (bucketIndex != -1 && bucketIndex < segments.Count - 1)
                    {
                        var keySegments = segments.Skip(bucketIndex + 1);
                        var key = string.Join("/", keySegments);

                        var config = new AmazonS3Config
                        {
                            ServiceURL = _s3ServiceUrl,
                            ForcePathStyle = true
                        };

                        using (var client = new AmazonS3Client(_s3AccessKeyId, _s3SecretAccessKey, config))
                        {
                            var request = new DeleteObjectRequest
                            {
                                BucketName = _s3BucketName,
                                Key = key
                            };
                            await client.DeleteObjectAsync(request);
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Fail silently but we can write to debug/console
                    Console.WriteLine($"Error deleting file from S3: {ex.Message}");
                }
            }
            else
            {
                // Deleting locally
                string fullPath = Path.Combine(_environment.WebRootPath, fileUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath))
                {
                    System.IO.File.Delete(fullPath);
                }
            }
        }
    }
}
