using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using FututreTechApp.Models;
using Microsoft.Azure.Cosmos;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Microsoft.Azure.Cosmos.Linq;
using System.Text.Json;
using Azure.Storage.Sas;
using Azure.Storage;


namespace FututreTechApp.Services
{
    public class StudentService
    {
        private readonly CosmosClient _cosmosClient;
        private readonly Container _container;
        private readonly IConfiguration _configuration;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _blobContainerName;
        private readonly string _accountName;
        private readonly string _accountKey;

        public StudentService(CosmosClient cosmosClient, BlobServiceClient blobServiceClient, IConfiguration configuration)
        {
            _cosmosClient = cosmosClient;
            var db = _cosmosClient.GetDatabase("StudentDB");
            _container = db.GetContainer("Students");
            _blobServiceClient = blobServiceClient;
            _blobContainerName = configuration["BlobStorage:ContainerName"];
            _accountName = configuration["BlobStorage:AccountName"];
            _accountKey = configuration["BlobStorage:AccountKey"];
        }

        public string GenerateSasUrlForStudentImage(string studentId)
        {
            var blobClient = _blobServiceClient
                .GetBlobContainerClient(_blobContainerName)
                .GetBlobClient($"{studentId}.jpg");

            if (!blobClient.Exists())
            {
                return null;
            }

            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _blobContainerName,
                BlobName = blobClient.Name,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasToken = sasBuilder.ToSasQueryParameters(new StorageSharedKeyCredential(_accountName, _accountKey)).ToString();

            return $"{blobClient.Uri}?{sasToken}";
        }

        public async Task<string> UploadPhotoAsync(IFormFile photo, string studentId)
        {
            if (photo == null || photo.Length == 0)
                throw new ArgumentException("Photo file is required.");

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
            var fileExtension = Path.GetExtension(photo.FileName).ToLowerInvariant();

            if (!allowedExtensions.Contains(fileExtension))
                throw new InvalidOperationException("Only JPEG and PNG images are allowed.");

            if (photo.Length > 5 * 1024 * 1024) // 5MB size limit
                throw new InvalidOperationException("File size must be less than 5MB.");

            var containerClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            // Save the file using the student ID as the name
            var fileName = $"{studentId}{fileExtension}"; 
            var blobClient = containerClient.GetBlobClient(fileName);

            try
            {
                using (var stream = photo.OpenReadStream())
                {
                    await blobClient.UploadAsync(stream, overwrite: true);
                }
                return blobClient.Uri.ToString(); 
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to upload photo. Please try again.", ex);
            }
        }

        private async Task<string> GenerateStudentIdAsync()
        {
            var query = _container.GetItemLinqQueryable<Student>(allowSynchronousQueryExecution: false)
                .OrderByDescending(s => s.id)
                .Take(1)
                .ToFeedIterator();

            var currentMaxId = "student-000"; // Default value if no students exist
            if (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                if (response.Count > 0)
                {
                    currentMaxId = response.First().id; // Get the current max ID
                }
            }

            // Extract the numeric part and increment it
            var currentNumber = int.Parse(currentMaxId.Split('-')[1]);
            var newNumber = currentNumber + 1;

            // Generate the new student ID
            return $"student-{newNumber:D3}"; // Format as student-001, student-002, etc.
        }

        public async Task CreateStudentAsync(StudentViewModel model)
        {
            try
            {
                var studentId = await GenerateStudentIdAsync();

                // Create the Student object
                var student = new Student
                {
                    id = studentId, // Use the generated ID
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    EnrolmentStatus = model.EnrolmentStatus,
                    ProfileImageUrl = await UploadPhotoAsync(model.Photo, studentId)
                };

                // Log the student object before saving
                Console.WriteLine($"Creating student: {JsonSerializer.Serialize(student)}");

                // Create the item in the Cosmos DB container
                await _container.CreateItemAsync(student, new PartitionKey(studentId));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating student: {ex.Message}");
                throw;
            }
        }

        public async Task<Student> GetStudentByIdAsync(string id)
        {
            try
            {
                var response = await _container.ReadItemAsync<Student>(id, new PartitionKey(id));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving student: {ex.Message}");
                throw;
            }
        }

        public async Task<Student> UpdateStudentAsync(Student student)
        {
            ItemResponse<Student> existingStudentResponse = await _container.ReadItemAsync<Student>(student.id, new PartitionKey(student.id));
            Student existingStudent = existingStudentResponse.Resource;

            existingStudent.FirstName = student.FirstName;
            existingStudent.LastName = student.LastName;
            existingStudent.Email = student.Email;
            existingStudent.MobileNumber = student.MobileNumber;
            existingStudent.EnrolmentStatus = student.EnrolmentStatus;

            if (!string.IsNullOrEmpty(student.ProfileImageUrl))
            {
                existingStudent.ProfileImageUrl = student.ProfileImageUrl;
            }
            
            await _container.ReplaceItemAsync(existingStudent, existingStudent.id, new PartitionKey(existingStudent.id));

            return existingStudent;
        }

        public async Task DeleteStudentAsync(string id)
        {
            try
            {
                var student = await _container.ReadItemAsync<Student>(id, new PartitionKey(id));

                await _container.DeleteItemAsync<Student>(id, new PartitionKey(id));

                var blobClient = _blobServiceClient.GetBlobContainerClient(_blobContainerName).GetBlobClient($"{id}.jpg");
                await blobClient.DeleteIfExistsAsync();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Handle the case where the student was not found
                Console.WriteLine($"Student with ID {id} not found.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting student: {ex.Message}");
                throw;
            }
        }
    }
}