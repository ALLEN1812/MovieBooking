using Microsoft.AspNetCore.Mvc;
using OnlineMovieBooking.Services;

namespace OnlineMovieBooking.Controllers;

public class HomeController : Controller
{
    private readonly IMovieService _movies;

    public HomeController(IMovieService movies) => _movies = movies;

    public async Task<IActionResult> Index()
    {
        var nowShowing = await _movies.GetAllMoviesAsync("now_showing");
        var comingSoon = await _movies.GetAllMoviesAsync("coming_soon");
        ViewBag.NowShowing = nowShowing;
        ViewBag.ComingSoon = comingSoon;
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}
