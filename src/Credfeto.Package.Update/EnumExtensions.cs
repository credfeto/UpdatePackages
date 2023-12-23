using System.Diagnostics.CodeAnalysis;
using CommandLine;
using Credfeto.Enumeration.Source.Generation.Attributes;
using Microsoft.Extensions.Logging;

namespace Credfeto.Package.Update;

[EnumText(typeof(ErrorType))]
[EnumText(typeof(LogLevel))]
[SuppressMessage(category: "ReSharper", checkId: "PartialTypeWithSinglePart", Justification = "Needed for generated code")]
internal static partial class EnumExtensions
{
    // Code generated
}