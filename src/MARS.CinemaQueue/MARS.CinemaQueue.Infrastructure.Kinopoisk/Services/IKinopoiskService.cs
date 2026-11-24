using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MARS.CinemaQueue.Infrastructure.Kinopoisk.Configuration;
using MARS.CinemaQueue.Infrastructure.Kinopoisk.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace MARS.CinemaQueue.Infrastructure.Kinopoisk.Services;

public interface IKinopoiskService
{
    Task<KinopoiskMovieDto?> GetMovieByIdAsync(
        int movieId,
        CancellationToken cancellationToken = default
    );
    Task<KinopoiskMovieDto?> GetMovieByUrlAsync(
        string url,
        CancellationToken cancellationToken = default
    );
}
