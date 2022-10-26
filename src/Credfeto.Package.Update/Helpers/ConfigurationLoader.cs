using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;

namespace Credfeto.Package.Update.Helpers;

internal static class ConfigurationLoader
{
    public static IConfigurationRoot LoadConfiguration(string[] args)
    {
        Dictionary<string, string> mappings = new(StringComparer.Ordinal)
                                              {
                                                  [@"-packageId"] = @"packageid",
                                                  ["-packageprefix"] = "packageprefix",
                                                  [@"-folder"] = @"folder",
                                                  [@"-source"] = @"source",
                                                  [@"-exclude"] = @"exclude"
                                              };

        return new ConfigurationBuilder().AddCommandLine(args: args, switchMappings: mappings)
                                         .Build();
    }
}