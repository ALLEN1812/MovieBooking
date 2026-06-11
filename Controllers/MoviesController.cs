using Microsoft.AspNetCore.Mvc;
using OnlineMovieBooking.Services;

namespace OnlineMovieBooking.Controllers;

public class MoviesController : Controller
{
    private readonly IMovieService _movies;

    public MoviesController(IMovieService movies) => _movies = movies;

    public async Task<IActionResult> Index(string? status, string? search)
    {
        ViewBag.Status = status;
        ViewBag.Search = search;
        var movies = await _movies.GetAllMoviesAsync(status, search);
        return View(movies);
    }

    public async Task<IActionResult> Details(int id)
    {
        var movie = await _movies.GetMovieByIdAsync(id);
        if (movie == null) return NotFound();
        var showtimes = await _movies.GetShowtimesByMovieAsync(id);
        ViewBag.Showtimes = showtimes;
        return View(movie);
    }

    public async Task<IActionResult> Schedule(int? cinemaId, string? date)
    {
        var cinemas = await _movies.GetAllCinemasAsync();
        ViewBag.Cinemas = cinemas;

        if (cinemaId.HasValue)
        {
            var cinema = await _movies.GetCinemaByIdAsync(cinemaId.Value);
            if (cinema == null) return NotFound();
            ViewBag.Cinema = cinema;

            var dates = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(i)).ToList();
            ViewBag.Dates = dates;

            var selectedDate = DateTime.TryParse(date, out var d) ? d : DateTime.Today;
            ViewBag.SelectedDate = selectedDate;

            var showtimes = await _movies.GetCinemaScheduleAsync(cinemaId.Value, selectedDate);
            ViewBag.GroupedShowtimes = showtimes.GroupBy(s => s.MovieId).ToList();
        }

        return View();
    }
}
