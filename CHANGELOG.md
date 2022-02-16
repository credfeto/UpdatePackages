# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
- FF-1429 - Updated FunFair.CodeAnalysis to 5.8.1.1203
- FF-1429 - Updated Meziantou.Analyzer to 1.0.695
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [1.8.2] - 2022-02-09
### Fixed
- New code analysis issues
### Changed
- FF-1429 - Updated Microsoft.Extensions to 6.0.0
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 17.0.64
- FF-1429 - Updated FunFair.CodeAnalysis to 5.7.3.1052
- FF-1429 - Updated SmartAnalyzers.CSharpExtensions.Annotations to 4.2.1
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.33.0.40503
- FF-1429 - Updated Meziantou.Analyzer to 1.0.680
- FF-1429 - Updated SecurityCodeScan.VS2019 to 5.6.0
- FF-1429 - Updated Meziantou.Analyzer to 1.0.681
- FF-3881 - Updated DotNet SDK to 6.0.101
- FF-1429 - Updated Meziantou.Analyzer to 1.0.683
- FF-1429 - Updated Meziantou.Analyzer to 1.0.684
- FF-1429 - Updated Meziantou.Analyzer to 1.0.685
- FF-1429 - Updated Meziantou.Analyzer to 1.0.686
- FF-1429 - Updated Meziantou.Analyzer to 1.0.687
- FF-1429 - Updated Philips.CodeAnalysis.MaintainabilityAnalyzers to 1.2.27
- FF-1429 - Updated Meziantou.Analyzer to 1.0.688
- FF-1429 - Updated Philips.CodeAnalysis.DuplicateCodeAnalyzer to 1.1.5
- FF-1429 - Updated Philips.CodeAnalysis.DuplicateCodeAnalyzer to 1.1.6
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.34.0.42011
- FF-1429 - Updated Roslynator.Analyzers to 4.0.0
- FF-1429 - Updated Roslynator.Analyzers to 4.0.2
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.35.0.42613
- FF-1429 - Updated Meziantou.Analyzer to 1.0.689
- FF-1429 - Updated Meziantou.Analyzer to 1.0.690
- FF-1429 - Updated Meziantou.Analyzer to 1.0.692
- FF-1429 - Updated Meziantou.Analyzer to 1.0.693
- FF-1429 - Updated Meziantou.Analyzer to 1.0.694
- FF-1429 - Updated FunFair.CodeAnalysis to 5.8.0.1196
- FF-3881 - Updated DotNet SDK to 6.0.102

## [1.8.0] - 2021-10-26
### Added
- Support for packages in Product SDK
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.30.0.37606

## [1.7.1] - 2021-10-15
### Fixed
- Re-added the missing output metadata used by scripts

## [1.7.0] - 2021-09-29
### Changed
- FF-1429 - Updated Roslynator.Analyzers to 3.2.0
- FF-1429 - Updated NuGet to 5.11.0
- FF-1429 - Updated Roslynator.Analyzers to 3.2.2
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.29.0.36737
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 17.0.63
- Optimised for when there are no matching packages in the solution
- Switched to NuGet.Protocol from old NuGet.Protocol.Core.V3 lib

## [1.6.0] - 2021-06-04
### Fixed
- Prefix match now matches an exact package too
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.23.0.32424
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.10.56

## [1.5.0] - 2021-04-19
### Changed
- FF-1429 - Updated AsyncFixer to 1.5.1
- FF-1429 - Updated Roslynator.Analyzers to 3.1.0
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.9.54
- FF-1429 - Updated NuGet to 5.9.1
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.21.0.30542
- Tab size to 4 chars

## [1.4.0] 2020-11-21
### Changed
- FF-1429 - Updated Microsoft.Extensions to 5.0.0
- FF-1429 - Updated Microsoft.CodeAnalysis.FxCopAnalyzers to 3.3.1
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.8.51
- Updated to .NET 5.0

## [1.3.0] 2020-10-26
### Added
- Added support for updating one package or a group of packages with the same prefix e.g. Microsoft.Extensions

## [1.2.0] 2020-10-26
### Changed
- FF-1429 - Updated Roslynator.Analyzers to 3.0.0
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.11.0.20529
- FF-1429 - Updated Microsoft.CodeAnalysis.FxCopAnalyzers to 3.3.0
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.7.54
- Updated build scripts

## [1.1.0] 2020-04-26
### Changed
- Updated to .net core 3.1.302
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.10.0.19839
- FF-1429 - Updated AsyncFixer to 1.3.0

## [1.0.0] 2020-04-26
### Changed
- Converted to to be a dotnet tool

## [0.0.0] - Project created