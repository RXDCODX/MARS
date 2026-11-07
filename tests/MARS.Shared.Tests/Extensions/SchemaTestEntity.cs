using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MARS.Platform.Clients.Http.Infrastructure;
using MARS.Platform.Clients.Http.Infrastructure.Extensions;
using MARS.Platform.Persistence.Postgres.Infrastructure.Extensions;
using MARS.Platform.Web.AspNetCore.Infrastructure.Configuration;
using MARS.TestKit.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MARS.Shared.Tests.Extensions;

internal sealed class SchemaTestEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
