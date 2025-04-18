using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using ZhaoYi_Test2.Models;
using ZhaoYi_Test2.ViewModels;
using Microsoft.Extensions.Logging;

namespace ZhaoYi_Test2.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;

        private readonly ILogger<AccountController> _logger;


        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)

        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View("LoginMobile");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    var user = await _userManager.FindByEmailAsync(model.Email);
                    if (user.Role == 0)
                    {
                        return RedirectToAction("ChooseRole", "Role");
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError(string.Empty, "Đăng nhập không thành công.");
                return View("LoginMobile",model);
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View("RegisterMobile");
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email, Role = 0 };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("ChooseRole", "Role");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }
            return View("RegisterMobile", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // Action được gọi khi nhấn nút "Đăng nhập với Google"
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Yêu cầu chuyển hướng đến nhà cung cấp bên ngoài
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider); // Trả về ChallengeResult để chuyển hướng đến Google
        }

        // Action xử lý callback từ Google sau khi xác thực thành công
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            returnUrl = returnUrl ?? Url.Content("~/"); // Trang mặc định nếu returnUrl là null
            if (remoteError != null)
            {
                // Xử lý lỗi từ Google trả về
                ModelState.AddModelError(string.Empty, $"Lỗi từ nhà cung cấp bên ngoài: {remoteError}");
                return RedirectToAction(nameof(Login), new { returnUrl }); // Quay lại trang Login với lỗi
            }

            // Lấy thông tin đăng nhập từ Google
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                ModelState.AddModelError(string.Empty, "Lỗi lấy thông tin đăng nhập từ nhà cung cấp bên ngoài.");
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            // Thử đăng nhập người dùng bằng thông tin từ Google (nếu họ đã liên kết tài khoản trước đó)
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);

            if (result.Succeeded)
            {
                // Đăng nhập thành công (tài khoản đã tồn tại và đã liên kết)
                _logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal.Identity.Name, info.LoginProvider); // Thêm logging nếu có _logger

                // Kiểm tra Role sau khi đăng nhập thành công
                var user = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
                if (user != null && user.Role == 0)
                {
                    return RedirectToAction("ChooseRole", "Role"); // Chuyển đến chọn role nếu chưa có
                }

                // Chuyển hướng về returnUrl hoặc trang chủ
                return LocalRedirect(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout"); // Chuyển đến trang thông báo khóa tài khoản
            }
            else
            {
                // Nếu người dùng chưa có tài khoản hoặc chưa liên kết tài khoản Google này
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["ProviderDisplayName"] = info.ProviderDisplayName; // Lấy tên nhà cung cấp (vd: Google)

                // Lấy email từ thông tin Google trả về
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                if (email == null)
                {
                    // Google không trả về email - Xử lý lỗi
                    ModelState.AddModelError(string.Empty, "Không thể lấy thông tin email từ Google. Vui lòng thử lại hoặc đăng ký bằng email.");
                    // Có thể cần tạo một trang riêng để xử lý lỗi này hoặc quay lại Login
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }

                // Kiểm tra xem có tài khoản nào đã dùng email này chưa
                var existingUser = await _userManager.FindByEmailAsync(email);
                if (existingUser != null)
                {
                    // Email đã tồn tại, nhưng chưa liên kết với Google login này
                    // Tiến hành liên kết tài khoản Google này với tài khoản hiện có
                    var addLoginResult = await _userManager.AddLoginAsync(existingUser, info);
                    if (addLoginResult.Succeeded)
                    {
                        // Liên kết thành công, đăng nhập người dùng
                        await _signInManager.SignInAsync(existingUser, isPersistent: false);
                        _logger.LogInformation("User account linked to {Name} provider.", info.LoginProvider);

                        // Kiểm tra Role sau khi liên kết và đăng nhập
                        if (existingUser.Role == 0)
                        {
                            return RedirectToAction("ChooseRole", "Role");
                        }
                        return LocalRedirect(returnUrl);
                    }
                    else
                    {
                        // Lỗi khi liên kết tài khoản
                        ModelState.AddModelError(string.Empty, $"Không thể liên kết tài khoản Google. Lỗi: {string.Join(", ", addLoginResult.Errors.Select(e => e.Description))}");
                        return RedirectToAction(nameof(Login), new { returnUrl });
                    }
                }
                else
                {
                    // Nếu không có tài khoản với email này -> Tạo tài khoản mới
                    var user = new ApplicationUser
                    {
                        UserName = email, // Dùng email làm UserName
                        Email = email,
                        EmailConfirmed = true, // Google đã xác thực email
                        Role = 0 // Bắt đầu với Role = 0, sau đó chuyển đến ChooseRole
                    };

                    var createUserResult = await _userManager.CreateAsync(user);
                    if (createUserResult.Succeeded)
                    {
                        // Tạo user thành công, thêm liên kết Google login
                        createUserResult = await _userManager.AddLoginAsync(user, info);
                        if (createUserResult.Succeeded)
                        {
                            _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                            await _signInManager.SignInAsync(user, isPersistent: false);
                            // Chuyển hướng đến trang chọn vai trò
                            return RedirectToAction("ChooseRole", "Role");
                        }
                    }
                    // Nếu tạo user hoặc thêm login thất bại
                    foreach (var error in createUserResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    // Quay lại trang Login với các lỗi
                    TempData["LoginErrors"] = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return RedirectToAction(nameof(Login), new { returnUrl });
                }
            }
        }
    }
}
