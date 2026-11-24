using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MARS.MediaStorage.Domain.Entities;

namespace MARS.MediaStorage.Application.Storage;

public readonly record struct IndexResult(int Added, int Updated);
