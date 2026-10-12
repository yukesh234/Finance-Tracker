using System.Net;
using System.Net.Mail;
using Finance_Tracker.Models;
using Finance_Tracker.ViewModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Finance_Tracker.Controllers
{
    public class UserController : Controller
    {
        private readonly SignInManager<Applicationuser> signInManager;
        private readonly UserManager<Applicationuser> userManager;

        public UserController(SignInManager<Applicationuser> signInManager, UserManager<Applicationuser> userManager)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
        }

        public IActionResult Login() => View();

        public IActionResult Register() => View();

        [HttpPost]
        public IActionResult Register(RegisterViewModel Reg)
        {
            if (ModelState.IsValid)
            {
                //// Store user data in session
                //HttpContext.Session.SetString("Reg_Name", Reg.Name);
                //HttpContext.Session.SetString("Reg_Email", Reg.Email);
                //HttpContext.Session.SetString("Reg_Password", Reg.Password);

                return RedirectToAction("VerifyCode", new { email = Reg.Email, Reg.Name, Reg.Password });
            }

            return View(Reg);
        }

        public IActionResult VerifyCode(string email, string Name, string Password)
        {
            // Store user data in session
            HttpContext.Session.SetString("Reg_Name", Name);
            HttpContext.Session.SetString("Reg_Email", email);
            HttpContext.Session.SetString("Reg_Password", Password);


            if (string.IsNullOrEmpty(email))
                return RedirectToAction("Register");

            // Generate and store 4-digit code in session
            int code = new Random().Next(1000, 9999);
            HttpContext.Session.SetString("VerificationCode", code.ToString());
            string senderEmail = builder.Configuration["EmailSettings:Email"];
            string password = builder.Configuration["EmailSettings:Password"];

            // Send email
            MailMessage mail = new()
            {
                From = new MailAddress(senderEmail, "Finance Tracker"),
                Subject = $"Hello {email}, Your Verification Code",
                Body = $"Your verification code is: {code}",
                IsBodyHtml = false
            };
            mail.To.Add(email);

            SmtpClient smtp = new("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true
            };
            smtp.Send(mail);

            ViewBag.Email = email;
            return View();
        }

        [HttpPost]
        [HttpPost]
        public async Task<IActionResult> VerifyCodeSubmit(VerifyCodeViewModel model)
        {
            string enteredCode = $"{model.Digit1}{model.Digit2}{model.Digit3}{model.Digit4}";
            string? expectedCode = HttpContext.Session.GetString("VerificationCode");

            if (enteredCode == expectedCode)
            {
                string? name = HttpContext.Session.GetString("Reg_Name");
                string? email = HttpContext.Session.GetString("Reg_Email");
                string? password = HttpContext.Session.GetString("Reg_Password");

                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    ModelState.AddModelError("", "Session expired. Please register again.");
                    return RedirectToAction("Register");
                }

                // Check if the email is already in use
                var existingUser = await userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    ModelState.AddModelError("", "An account with this email already exists.");
                    return View("VerifyCode", model);
                }

                var user = new Applicationuser
                {
                    UserName = name,
                    Email = email
                };

                var result = await userManager.CreateAsync(user, password);
                if (result.Succeeded)
                {
                    // Clear the session
                    HttpContext.Session.Remove("Reg_Name");
                    HttpContext.Session.Remove("Reg_Email");
                    HttpContext.Session.Remove("Reg_Password");
                    HttpContext.Session.Remove("VerificationCode");

                    return RedirectToAction("Login");
                }

                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error.Description);
            }
            else
            {
                ModelState.AddModelError("", "Incorrect verification code.");
            }

            ViewBag.Email = HttpContext.Session.GetString("Reg_Email");
            return View("VerifyCode", model);
        }


        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.Users
              .AsNoTracking()
              .FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user != null)
                {
                    var result = await signInManager.PasswordSignInAsync(user.UserName, model.Password, model.Rememberme, false);
                    if (result.Succeeded)
                    {
                        return RedirectToAction("Dashboard", "Transaction");
                    }
                }

                ModelState.AddModelError("", "Email or password is incorrect.");
            }

            return View(model);
        }
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public IActionResult ForgetPassword()
        {
            return View();
        }

        // To check if the email exists 
        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetpasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByEmailAsync(model.Email);

                if (user == null)
                {
                    ModelState.AddModelError("", "Email not found.");
                    return View(model);
                }

                return RedirectToAction("SendCode", new { email = user.Email });
            }

            return View(model);
        }

        // To send verification code 
        public IActionResult SendCode(string email)
        {
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("ForgetPassword");

            int code = new Random().Next(1000, 9999);
            HttpContext.Session.SetString("VerificationCode", code.ToString());
            HttpContext.Session.SetString("Email", email);

            // Send email
            MailMessage mail = new()
            {
                From = new MailAddress("fiancetracker@gmail.com", "Finance Tracker"),
                Subject = "Your Verification Code",
                Body = $"Your verification code is: {code}",
                IsBodyHtml = false
            };
            mail.To.Add(email);

            SmtpClient smtp = new("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential("fiancetracker@gmail.com", "tlyecsjrslqabjri"),
                EnableSsl = true
            };

            try
            {
                smtp.Send(mail);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Failed to send email. Please try again.");
                return RedirectToAction("ForgetPassword");
            }

            ViewBag.Email = email;
            return View();
        }

        // To verify the code 
        [HttpPost]
        public IActionResult ChangePassword(SendCodeViewModel model)
        {
            string enteredCode = $"{model.Digit1}{model.Digit2}{model.Digit3}{model.Digit4}";
            string? expectedCode = HttpContext.Session.GetString("VerificationCode");
            string? email = HttpContext.Session.GetString("Email");

            if (enteredCode == expectedCode)
            {
                return RedirectToAction("UpdatePassword", new { email });
            }

            ModelState.AddModelError("", "Incorrect verification code.");
            ViewBag.Email = email;
            return View(model);
        }

        // To update the password
        // GET: Display the UpdatePassword view
        public IActionResult UpdatePassword(string email)
        {
            if (string.IsNullOrEmpty(email))
                return RedirectToAction("ForgetPassword");

            var model = new UpdatePasswordViewModel
            {
                Email = email
            };

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdatePassword(UpdatePasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email not found.");
                return View(model);
            }

            // Check if the user has a password
            if (!await userManager.HasPasswordAsync(user))
            {
                ModelState.AddModelError("", "This user does not have a password set.");
                return View(model);
            }

            // Backup original password hash
            string? currentPasswordHash = user.PasswordHash;

            try
            {
                // Remove the current password
                var removeResult = await userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    foreach (var error in removeResult.Errors)
                        ModelState.AddModelError("", error.Description);
                    return View(model);
                }

                // Add the new password
                var addPassResult = await userManager.AddPasswordAsync(user, model.NewPassword);
                if (!addPassResult.Succeeded)
                {
                    // Restore the original password
                    user.PasswordHash = currentPasswordHash;
                    user.ConcurrencyStamp = Guid.NewGuid().ToString();
                    await userManager.UpdateAsync(user); // Attempt restoration

                    foreach (var error in addPassResult.Errors)
                        ModelState.AddModelError("", error.Description);

                    return View(model);
                }

                // Clear any session used in password recovery
                HttpContext.Session.Remove("VerificationCode");
                HttpContext.Session.Remove("Email");

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");

                try
                {
                    user.PasswordHash = currentPasswordHash;
                    user.ConcurrencyStamp = Guid.NewGuid().ToString();
                    await userManager.UpdateAsync(user);
                }
                catch
                {
                    ModelState.AddModelError("", "Critical error: failed to restore password. Please contact support.");
                }

                ModelState.AddModelError("", "An unexpected error occurred while updating your password.");
                return View(model);
            }
        }


    }
}
