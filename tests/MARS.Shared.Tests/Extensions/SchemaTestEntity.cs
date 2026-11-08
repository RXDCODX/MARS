using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Clients.Http.Extensions;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using MARS.Platform.Infrastructure.Web.AspNetCore.Configuration;
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
