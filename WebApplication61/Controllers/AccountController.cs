using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NETCore.Encrypt.Extensions;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;
using WebApplication61.Entities;
using WebApplication61.Helpers;
using WebApplication61.Models;

namespace WebApplication61.Controllers
{
    [Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
    public class AccountController : Controller
    {
        private readonly DatabaseContext _databaseContext;
        private readonly IConfiguration _configuration;
        private readonly IHasher _hasher;

        public AccountController(DatabaseContext databaseContext, IConfiguration configuration, IHasher hasher)
        {
            _databaseContext = databaseContext;
            _configuration = configuration;
            _hasher = hasher;
        }
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                //login işlemleri ilk önce kullanıcı adı ve şifre doğru mu 
                string hashedPasword = _hasher.DoMD5HashedString(model.Password);

                User user = _databaseContext.Users.SingleOrDefault(x => x.Username.ToLower() == model.Username.ToLower() && x.Password == hashedPasword); // kayıt varsa bir tane bize dönecek birden fazla varsa hata fırlatıcak.

                if (user != null)
                {
                    if (user.Locked)
                    {
                        ModelState.AddModelError(nameof(model.Username), "User is locked.");
                        return View(model);
                    }
                    //cockie nin içerisine koyacağımız yapıları oluşturalım.
                    List<Claim> claims = new List<Claim>(); // veriyi claim içerisinde saklıyoruz.kullanıcı roller herhangi bir tanım bir hak yetkilendirmede de kullanılır. 
                    claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()));
                    claims.Add(new Claim(ClaimTypes.Name, user.FullName ?? String.Empty));  //fullname null ise null tutamıyor claimler boş string yada herhangi bir şey yazın.
                    claims.Add(new Claim(ClaimTypes.Role, user.Role));
                    claims.Add(new Claim("Username", user.Username));
                    //kullanıcının adını fullname alanına koymak yerine hazır bazı claim tanımları var username name claime basabiliriz.aslında stringler üzerine tıkladığında nasıl string tuttuklarını görebiliriz.
                    ClaimsIdentity identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Home");


                }
                else
                {
                    ModelState.AddModelError("", "Username or password is incorrect.");
                }


            }
            return View(model); // aksi halde hata varsa view ile modeli geri gönderelim.
        }

       

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }
        [AllowAnonymous]
        [HttpPost]
        public IActionResult Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {


                //insert ederken registerda kullanıcı var mı kontrol edelim
                if (_databaseContext.Users.Any(x => x.Username.ToLower() == model.Username.ToLower()))
                {
                    //username alanı altında gösterelim.
                    ModelState.AddModelError(nameof(model.Username), "Username is already exist.");
                    return View(model);
                }
                //Register işlemleri 
                string hashedPasword = _hasher.DoMD5HashedString(model.Password);

                User user = new()
                {
                    Username = model.Username,
                    Password = hashedPasword
                };
                _databaseContext.Users.Add(user);
                int affectedRowCount = _databaseContext.SaveChanges();

                if (affectedRowCount == 0)
                {
                    ModelState.AddModelError("", "User can not be added.");
                }
                else
                {
                    return RedirectToAction(nameof(Login));
                }


            }
            return View();
        }
        public IActionResult Profile()
        {
            ProfileInfoloader();
            return View();
        }

        private void ProfileInfoloader()
        {
            Guid userid = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
            User user = _databaseContext.Users.SingleOrDefault(x => x.Id == userid);

            ViewData["FullName"] = user.FullName;
            ViewData["ProfileImage"] = user.ProfileImageFileName;
        }

        [HttpPost()]
        public IActionResult ProfileChangeFullName([Required][StringLength(50)] string? fullname)
        {
            if (ModelState.IsValid)
            {
                Guid userid = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
                User user = _databaseContext.Users.SingleOrDefault(x => x.Id == userid);

                user.FullName = fullname;
                _databaseContext.SaveChanges();

                return RedirectToAction(nameof(Profile));
            }

            ProfileInfoloader();
            return View("Profile");
        }

        [HttpPost()]
        public IActionResult ProfileChangePassword([Required][MinLength(6)][MaxLength(16)] string? password)
        {
            if (ModelState.IsValid)
            {
                Guid userid = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
                User user = _databaseContext.Users.SingleOrDefault(x => x.Id == userid);

                string hashedPasword = _hasher.DoMD5HashedString(password);

                user.Password = hashedPasword;
                _databaseContext.SaveChanges();

                ViewData["result"] = "PasswordChanged";
            }

            ProfileInfoloader();
            return View("Profile");
        }

        [HttpPost]
        public IActionResult ProfileChangeImage([Required] IFormFile file)
        {
            if (ModelState.IsValid)
            {
                Guid userid = new Guid(User.FindFirstValue(ClaimTypes.NameIdentifier));
                User user = _databaseContext.Users.SingleOrDefault(x => x.Id == userid);

                //p_guid.jpg
                string fileName = $"p_{userid}.jpg";
                Stream stream = new FileStream($"wwwroot/uploads/{fileName}",FileMode.OpenOrCreate);

                file.CopyTo(stream);
                stream.Close();
                stream.Dispose();

                user.ProfileImageFileName = fileName;
                _databaseContext.SaveChanges();

                return RedirectToAction(nameof(Profile));
            }

            ProfileInfoloader();
            return View("Profile");
        }
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction(nameof(Login));
        }



    }
}
