﻿using Document_Management.Data;
using Document_Management.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace Document_Management.Controllers
{
    public class AccountController : Controller
    {
        //Database Context
        private readonly ApplicationDbContext _dbcontext;

        private readonly string? userrole;
        private readonly string? username;

        //Passing the dbcontext in to another variable
        public AccountController(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _dbcontext = context;

            // Ensure that HttpContext and the session value are not null
            if (httpContextAccessor.HttpContext != null)
            {
                userrole = httpContextAccessor.HttpContext.Session.GetString("userrole")?.ToLower();
                username = httpContextAccessor.HttpContext.Session.GetString("username");
            }
            else
            {
                userrole = null; // or set a default value as needed
            }
        }

        //Action for Account/Index
        public async Task<IActionResult> Index()
        {
            if (!string.IsNullOrEmpty(username))
            {
                if (userrole != "admin")
                {
                    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
                }

                var users = await _dbcontext.Account
                    .OrderBy(u => u.EmployeeNumber)
                    .ToListAsync();

                return View(users);
            }
            else
            {
                return RedirectToAction("Login", "Account");
            }
        }

        //Get for the Action Account/Create
        [HttpGet]
        public IActionResult Create()
        {
            if (string.IsNullOrEmpty(username))
            {
                if (userrole != "admin")
                {
                    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
                }
                return RedirectToAction("Login", "Account");
            }

            return View(new Register());
        }

        //Post for the Action Account/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Register user, string[] AccessFolders, string[] ModuleAccess)
        {
            if (ModelState.IsValid)
            {
                var usernameExists = _dbcontext.Account
                    .Any(u => u.Username == user.Username);

                var employeeNumberExists = _dbcontext.Account
                    .Any(u => u.EmployeeNumber == user.EmployeeNumber);

                if (usernameExists && employeeNumberExists)
                {
                    ModelState.AddModelError("", "Both Username and Employee Number are already in use by other users.");
                }
                else if (usernameExists)
                {
                    ModelState.AddModelError("", "Username is already in use by another user.");
                }
                else if (employeeNumberExists)
                {
                    ModelState.AddModelError("", "Employee Number is already in use by another user.");
                }

                if (usernameExists || employeeNumberExists)
                {
                    return View(user); // Return the user object that failed validation
                }

                if (!string.IsNullOrEmpty(username))
                {
                    // Join selected departments into a comma-separated string
                    user.AccessFolders = string.Join(",", AccessFolders);
                    user.ModuleAccess = string.Join(",", ModuleAccess);

                    user.Password = HashPassword(user.Password);
                    _dbcontext.Account.Add(user);

                    //Implementing the logs
                    LogsModel logs = new(username, $"Add new user: {user.Username}");
                    _dbcontext.Logs.Add(logs);

                    _dbcontext.SaveChanges();
                    TempData["success"] = "User created successfully";
                    return RedirectToAction("Index", "Account");
                }
                else
                {
                    return RedirectToAction("Login", "Account");
                }
            }

            return View(user);
        }

        //Get for Action Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (string.IsNullOrEmpty(username))
            {
                return View();
            }
            return RedirectToAction("Index", "Home");
        }

        //Post for Action Account/Login
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (ModelState.IsValid)
            {
                var user = _dbcontext.Account
                    .FirstOrDefault(u => u.Username == username);

                if (user != null && user.Password == HashPassword(password))
                {
                    HttpContext.Session.SetString("username", user.Username); // Store username in session
                    HttpContext.Session.SetString("userrole", user.Role); // Store user role in session
                    HttpContext.Session.SetString("useraccessfolders", user.AccessFolders); // Store user folder access in session
                    HttpContext.Session.SetString("usermoduleaccess", user.ModuleAccess); // Store user module access in session
                    HttpContext.Session.SetString("userfirstname", user.FirstName); // Store user module access in session

                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Invalid username or password");
                }
            }

            return View();
        }

        //Get for the Action Account/Edit
        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (string.IsNullOrEmpty(username))
            {
                if (userrole != "admin")
                {
                    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
                }
                return RedirectToAction("Login", "Account");
            }

            // Retrieve the user from the database
            var user = _dbcontext.Account
                .FirstOrDefault(x => x.Id == id);

            if (user == null)
            {
                return NotFound();
            }

            // Split the comma-separated AccessFolders into a list of selected departments
            if (!string.IsNullOrEmpty(user.AccessFolders))
            {
                var selectedDepartments = user.AccessFolders.Split(',').ToList();

                // Join the selected departments back into a comma-separated string
                user.AccessFolders = string.Join(",", selectedDepartments);
            }

            return View(user);
        }

        //Post for the Action Account/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Register model, string[] AccessFolders, string[] ModuleAccess, string newPassword, string newConfirmPassword)
        {
            var user = await _dbcontext.Account
                .FindAsync(model.Id);

            if (string.IsNullOrEmpty(username))
            {
                if (userrole != "admin")
                {
                    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
                }
                return RedirectToAction("Login", "Account");
            }

            if (user != null)
            {
                // Check if there are any changes in user data
                bool dataChanged = user.EmployeeNumber != model.EmployeeNumber ||
                                   user.FirstName != model.FirstName ||
                                   user.LastName != model.LastName ||
                                   user.Department != model.Department ||
                                   user.Username != model.Username ||
                                   user.Role != model.Role ||
                                   (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(newConfirmPassword)) ||
                                   !Enumerable.SequenceEqual(user.AccessFolders?.Split(','), AccessFolders ?? new string[0]) ||
                                   !Enumerable.SequenceEqual(user.ModuleAccess?.Split(','), ModuleAccess ?? new string[0]);

                if (dataChanged)
                {
                    // Update the user properties
                    user.EmployeeNumber = model.EmployeeNumber;
                    user.FirstName = model.FirstName;
                    user.LastName = model.LastName;
                    user.Department = model.Department;
                    user.Username = model.Username;
                    user.Role = model.Role;

                    // Check if a new password is provided
                    if (!string.IsNullOrEmpty(newPassword) && !string.IsNullOrEmpty(newConfirmPassword))
                    {
                        // Hash and update the new password
                        user.Password = HashPassword(newPassword);
                    }

                    // Join the selected departments into a comma-separated string
                    if (AccessFolders != null && AccessFolders.Length > 0)
                    {
                        user.AccessFolders = string.Join(",", AccessFolders);
                    }
                    else
                    {
                        // Handle the case where no departments are selected
                        user.AccessFolders = string.Empty;
                    }

                    if (ModuleAccess != null && ModuleAccess.Length > 0)
                    {
                        user.ModuleAccess = string.Join(",", ModuleAccess);
                    }
                    else
                    {
                        // Handle the case where no departments are selected
                        user.ModuleAccess = string.Empty;
                    }

                    // Implementing the logs
                    LogsModel logs = new(username, $"Update user: {user.Username}");
                    _dbcontext.Logs.Add(logs);

                    await _dbcontext.SaveChangesAsync();
                    TempData["success"] = "User updated successfully";
                    return RedirectToAction("Index");
                }
                return RedirectToAction("Edit");
            }

            return RedirectToAction("Index");
        }

        // GET: Account/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (string.IsNullOrEmpty(username))
            {
                if (userrole != "admin")
                {
                    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
                }
                return RedirectToAction("Login", "Account");
            }

            if (id == null || _dbcontext.Account == null)
            {
                return NotFound();
            }

            var employee = await _dbcontext.Account.FirstOrDefaultAsync(m => m.Id == id);
            if (employee == null)
            {
                return NotFound();
            }

            return View(employee);
        }

        // POST: Account/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_dbcontext.Account == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Account'  is null.");
            }

            if (string.IsNullOrEmpty(username))
            {
                if (userrole != "admin")
                {
                    TempData["ErrorMessage"] = "You have no access to this action. Please contact the MIS Department if you think this is a mistake.";
                    return RedirectToAction("Privacy", "Home"); // Redirect to the login page or another appropriate action
                }

                return RedirectToAction("Login", "Account");
            }

            var employee = await _dbcontext.Account
                .FindAsync(id);

            if (employee != null)
            {
                _dbcontext.Account.Remove(employee);

                //Implementing the logs
                LogsModel logs = new(username, $"Delete user: {employee.Username}");
                _dbcontext.Logs.Add(logs);

                await _dbcontext.SaveChangesAsync();

                TempData["success"] = "User deleted successfully";
            }

            await _dbcontext.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(Register model)
        {
            var user = await _dbcontext.Account
                .FirstOrDefaultAsync(x => x.Username == username);

            if (user != null)
            {
                if (user.Password == HashPassword(model.Password))
                {
                    TempData["error"] = "New password must not the same with the previous.";
                }

                user.Password = HashPassword(model.Password);
                await _dbcontext.SaveChangesAsync();
            }

            TempData["success"] = "Change password successfully";
            return View();
        }

        //Action for Account/Logout and remove the session
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();

            return RedirectToAction("Index", "Home");
        }

        // Hash the password using a salt
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }
}