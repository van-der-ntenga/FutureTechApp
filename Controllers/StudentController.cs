using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using Azure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FututreTechApp.Models;
using FututreTechApp.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using X.PagedList;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using X.PagedList.Extensions;

namespace FututreTechApp.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly CosmosClient _cosmosClient;
        private readonly string _databaseId = "StudentDB";
        private readonly string _containerId = "Students";
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _blobContainerName = "studentimages";
        private readonly ILogger<StudentController> _logger;
        private readonly IConfiguration _configuration;
        private readonly StudentService _studentService;
        private readonly string _accountName;
        private readonly string _accountKey;

        public StudentController(StudentService studentService, CosmosClient cosmosClient, IConfiguration configuration, ILogger<StudentController> logger, BlobServiceClient blobServiceClient)
        {
            _studentService = studentService;
            _cosmosClient = cosmosClient;
            _blobServiceClient = blobServiceClient;
            _configuration = configuration;
            _logger = logger;
            _blobContainerName = configuration["BlobStorage:ContainerName"];
            _accountName = configuration["BlobStorage:AccountName"];
            _accountKey = configuration["BlobStorage:AccountKey"];
        }

        public async Task<IActionResult> Index(int? page, string sortOrder)
        {
            var container = _cosmosClient.GetContainer(_databaseId, _containerId);
            var query = container.GetItemQueryIterator<Student>();
            List<Student> students = new List<Student>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                students.AddRange(response);
            }

            // Sorting logic
            switch (sortOrder)
            {
                case "id":
                    students = students.OrderBy(s => s.id).ToList();
                    break;
                case "id_desc":
                    students = students.OrderByDescending(s => s.id).ToList();
                    break;
                case "firstName":
                    students = students.OrderBy(s => s.FirstName).ToList();
                    break;
                case "firstName_desc":
                    students = students.OrderByDescending(s => s.FirstName).ToList();
                    break;
                case "lastName":
                    students = students.OrderBy(s => s.LastName).ToList();
                    break;
                case "lastName_desc":
                    students = students.OrderByDescending(s => s.LastName).ToList();
                    break;
                case "email":
                    students = students.OrderBy(s => s.Email).ToList();
                    break;
                case "email_desc":
                    students = students.OrderByDescending(s => s.Email).ToList();
                    break;
                case "enrolmentStatus":
                    students = students.OrderBy(s => s.EnrolmentStatus).ToList();
                    break;
                case "enrolmentStatus_desc":
                    students = students.OrderByDescending(s => s.EnrolmentStatus).ToList();
                    break;
                default:
                    students = students.OrderBy(s => s.id).ToList();
                    break;
            }

            foreach (var student in students)
            {
                student.ProfileImageUrl = _studentService.GenerateSasUrlForStudentImage(student.id);
            }

            int pageSize = 10;
            int pageNumber = page ?? 1;
            return View(students.ToPagedList(pageNumber, pageSize));
        }

        [HttpGet]
        public IActionResult Create()
        {
            ViewBag.StatusList = new SelectList(new[] { "Active", "Inactive" });
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(StudentViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Log the model state errors
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    Console.WriteLine(error.ErrorMessage);
                }
                ViewBag.StatusList = new SelectList(new[] { "Active", "Inactive" });
                return View(model);
            }

            // Validate the uploaded photo
            if (model.Photo != null)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                var fileExtension = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("Photo", "Only JPEG and PNG images are allowed.");
                    ViewBag.StatusList = new SelectList(new[] { "Active", "Inactive" });
                    return View(model);
                }

                if (model.Photo.Length > 5 * 1024 * 1024) // 5MB size limit
                {
                    ModelState.AddModelError("Photo", "File size must be less than 5MB.");
                    ViewBag.StatusList = new SelectList(new[] { "Active", "Inactive" });
                    return View(model);
                }
            }

            // Proceed with creating the student
            await _studentService.CreateStudentAsync(model);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            var currentPhotoUrl = _studentService.GenerateSasUrlForStudentImage(student.id);
            ViewBag.CurrentPhotoUrl = currentPhotoUrl;
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var currentPhotoUrl = _studentService.GenerateSasUrlForStudentImage(student.id);
            ViewBag.CurrentPhotoUrl = currentPhotoUrl;

            var model = new StudentViewModel
            {
                id = student.id,
                FirstName = student.FirstName,
                LastName = student.LastName,
                Email = student.Email,
                MobileNumber = student.MobileNumber,
                EnrolmentStatus = student.EnrolmentStatus,
                Photo = null
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(StudentViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Retrieve the existing student
                var existingStudent = await _studentService.GetStudentByIdAsync(model.id);
                if (existingStudent == null)
                {
                    return NotFound();
                }

                // Create a new Student object from the ViewModel
                var studentToUpdate = new Student
                {
                    id = existingStudent.id,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    MobileNumber = model.MobileNumber,
                    EnrolmentStatus = model.EnrolmentStatus,
                    ProfileImageUrl = existingStudent.ProfileImageUrl // Keep the existing photo URL initially
                };

                // Check if a new photo is uploaded
                if (model.Photo != null)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(model.Photo.FileName).ToLowerInvariant();

                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        ModelState.AddModelError("Photo", "Only JPEG and PNG images are allowed.");
                        ViewBag.CurrentPhotoUrl = _studentService.GenerateSasUrlForStudentImage(model.id);
                        return View(model);
                    }

                    if (model.Photo.Length > 5 * 1024 * 1024) // 5MB size limit
                    {
                        ModelState.AddModelError("Photo", "File size must be less than 5MB.");
                        ViewBag.CurrentPhotoUrl = _studentService.GenerateSasUrlForStudentImage(model.id);
                        return View(model);
                    }

                    // Upload the new photo and update the profile image URL
                    studentToUpdate.ProfileImageUrl = await _studentService.UploadPhotoAsync(model.Photo, existingStudent.id);
                }

                // Save the updated student back to the database
                await _studentService.UpdateStudentAsync(studentToUpdate);
                return RedirectToAction("Index");
            }

            ViewBag.CurrentPhotoUrl = _studentService.GenerateSasUrlForStudentImage(model.id);
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var student = await _studentService.GetStudentByIdAsync(id);
            

            if (student == null)
            {
                return NotFound();
            }

            var currentPhotoUrl = _studentService.GenerateSasUrlForStudentImage(id);
            ViewBag.CurrentPhotoUrl = currentPhotoUrl;

            return View(student);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            await _studentService.DeleteStudentAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Search(string searchBy, string searchTerm, int? page)
        {
            var container = _cosmosClient.GetContainer(_databaseId, _containerId);
            var query = container.GetItemQueryIterator<Student>();
            List<Student> students = new List<Student>();

            while (query.HasMoreResults)
            {
                var response = await query.ReadNextAsync();
                students.AddRange(response);
            }

            IEnumerable<Student> filteredStudents = students;

            if (!string.IsNullOrEmpty(searchTerm))
            {
                switch (searchBy?.ToLower())
                {
                    case "firstname":
                        filteredStudents = students.Where(s => s.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "lastname":
                        filteredStudents = students.Where(s=>s.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "id":
                        filteredStudents = students.Where(s => s.id.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                        break;
                    case "email":
                        filteredStudents = students.Where(s => s.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                        break;
                    
                }
            }

            foreach (var student in filteredStudents)
            {
                student.ProfileImageUrl = _studentService.GenerateSasUrlForStudentImage(student.id);
            }

            int pageSize = 10;
            int pageNumber = page ?? 1;

            // Check if there are no results and set a message
            if (!filteredStudents.Any())
            {
                ViewBag.NoResultsMessage = "Student not found.";
            }

            return View(filteredStudents.ToPagedList(pageNumber, pageSize));
        }

        
    }
}
