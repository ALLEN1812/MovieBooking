using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineMovieBooking.Models.ViewModels;
using OnlineMovieBooking.Services;

namespace OnlineMovieBooking.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _auth;

    public AccountController(IAuthService auth) => _auth = auth;

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _auth.LoginAsync(model.Email, model.Password);
        if (user == null)
        {
            ModelState.AddModelError("", "Email hoặc mật khẩu không chính xác");
            return View(model);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = model.RememberMe });

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return user.Role == "admin"
            ? RedirectToAction("Index", "Admin", new { area = "" })
            : RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var (success, message) = await _auth.RegisterAsync(model.Name, model.Email, model.Phone, model.Password);
        if (!success)
        {
            ModelState.AddModelError("", message);
            return View(model);
        }

        TempData["Success"] = "Đăng ký thành công! Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View();

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var token = await _auth.GenerateResetTokenAsync(model.Email);
        // In production, send email with reset link
        // For demo: show token in TempData
        if (token != null)
            TempData["ResetLink"] = Url.Action("ResetPassword", "Account", new { token }, Request.Scheme);

        TempData["Info"] = "Nếu email tồn tại, liên kết đặt lại mật khẩu đã được gửi.";
        return RedirectToAction("ForgotPassword");
    }

    [HttpGet]
    public IActionResult ResetPassword(string token) => View(new ResetPasswordViewModel { Token = token });

    [HttpPost]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var success = await _auth.ResetPasswordAsync(model.Token, model.NewPassword);
        if (!success)
        {
            ModelState.AddModelError("", "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn");
            return View(model);
        }
        TempData["Success"] = "Mật khẩu đã được đặt lại thành công!";
        return RedirectToAction("Login");
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();
}
