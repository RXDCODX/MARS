using System;

namespace MARS.Commands.Services.Entitys;

[Flags]
public enum CommandVisibility
{
    None = 0,
    FullList = 1,
    ShortList = 2,
    Inline = 4,
    All = FullList | ShortList | Inline,
}
