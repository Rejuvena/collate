﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CliFx.Attributes;
using CliFx.Infrastructure;
using Rejuvena.Collate.Packing;
using Rejuvena.Collate.Packing.Properties;
using Rejuvena.Collate.Packing.References;
using Rejuvena.Collate.Util;

namespace Rejuvena.Collate.CLI.Commands;

[Command(COMMAND_NAME, Description = "Packages a mod into a .tmod file.")]
public sealed class PackageModCommand : VersionSensitiveCommand, IPropertiesProvider, IReferencesProvider
{
    public const string COMMAND_NAME = "package";

    protected override string CommandName => COMMAND_NAME;

#region Build properties
    [CommandOption("display-name", Description = "The display name of the mod.")]
    public string? DisplayName { get; set; } = null;

    [CommandOption("author", Description = "The author(s) of the mod.")]
    public string? Author { get; set; } = null;

    [CommandOption("mod-version", Description = "The version of the mod.")]
    public string? ModVersion { get; set; } = null;

    [CommandOption("homepage", Description = "The homepage of the mod.")]
    public string? Homepage { get; set; } = null;

    [CommandOption("mod-side", Description = "The client-server side the mod should be loaded on (Both, Client, Server, NoSync.")]
    public string? ModSide { get; set; } = null;

    [CommandOption("sort-before", Description = "What mods should be loaded before this mod.")]
    public string? SortBefore { get; set; } = null;

    [CommandOption("sort-after", Description = "What mods should be loaded after this mod.")]
    public string? SortAfter { get; set; } = null;

    [CommandOption("hide-code", Description = "Whether the DLL of this mod should be hidden.")]
    public bool? HideCode { get; set; } = null;

    [CommandOption("hide-resources", Description = "Whether the resources of this mod should be hidden.")]
    public bool? HideResources { get; set; } = null;

    [CommandOption("include-source", Description = "Whether additional source files of this mod should be hidden.")]
    public bool? IncludeSource { get; set; } = null;

    [CommandOption("build-ignore", Description = "Additional, finer control over what files should be ignored.")]
    public string? BuildIgnore { get; set; } = null;
#endregion

    [CommandOption("asmrefs-path")]
    public string AsmRefsPath { get; set; }

    [CommandOption("nugetrefs-path")]
    public string NuGetRefsPath { get; set; }

    [CommandOption("modrefs-path")]
    public string ModRefsPath { get; set; }

    /// <summary>
    ///     The project directory.
    /// </summary>
    [CommandOption("proj-dir", 'p', IsRequired = true, Description = "The project directory.")]
    public string ProjectDirectory { get; set; } = string.Empty;

    [CommandOption("proj-out-dir")]
    public string ProjectOutputDirectory { get; set; }

    [CommandOption("asm-name")]
    public string AssemblyName { get; set; }

    [CommandOption("tml-ver")]
    public string TmlVersion { get; set; }

    [CommandOption("tml-path")]
    public string TmlPath { get; set; }

    [CommandOption("out-dir")]
    public string OutputTmodPath { get; set; }

    protected override async ValueTask ExecuteAsync(IConsole console, Version version) {
        if (Debug) {
            await console.Output.WriteLineAsync("Options:");
            await console.Output.WriteLineAsync($"  {nameof(AsmRefsPath)}: {AsmRefsPath}");
            await console.Output.WriteLineAsync($"  {nameof(NuGetRefsPath)}: {NuGetRefsPath}");
            await console.Output.WriteLineAsync($"  {nameof(ModRefsPath)}: {ModRefsPath}");
            await console.Output.WriteLineAsync($"  {nameof(ProjectDirectory)}: {ProjectDirectory}");
            await console.Output.WriteLineAsync($"  {nameof(ProjectOutputDirectory)}: {ProjectOutputDirectory}");
            await console.Output.WriteLineAsync($"  {nameof(AssemblyName)}: {AssemblyName}");
            await console.Output.WriteLineAsync($"  {nameof(TmlVersion)}: {TmlVersion}");
            await console.Output.WriteLineAsync($"  {nameof(TmlPath)}: {TmlPath}");
            await console.Output.WriteLineAsync($"  {nameof(OutputTmodPath)}: {OutputTmodPath}");

            await console.Output.WriteLineAsync("Properties:");
            foreach ((string key, string value) in GetProperties()) await console.Output.WriteLineAsync($"  {key}: {value}");
        }

        if (string.IsNullOrEmpty(OutputTmodPath)) OutputTmodPath = PathLocator.FindSavePath(TmlPath, AssemblyName);

        TModPacker.PackMod(
            new PackingOptions
                {
                    ProjectDirectory      = ProjectDirectory,
                    ProjectBuildDirectory = ProjectOutputDirectory,
                    AssemblyName          = AssemblyName,
                    TmlVersion            = TmlVersion,
                    OutputTmodPath        = OutputTmodPath,
                }
                .WithReferencesProvider(this)
                .WithPropertiesProvider(this)
        );
    }

    public Dictionary<string, string> GetProperties() {
        var properties = new Dictionary<string, string>();

        void includeIfNotNull(string key, string? value) {
            if (value is not null) properties.Add(key, value);
        }

        includeIfNotNull("displayName",   DisplayName);
        includeIfNotNull("author",        Author);
        includeIfNotNull("modVersion",    ModVersion);
        includeIfNotNull("homepage",      Homepage);
        includeIfNotNull("side",          ModSide);
        includeIfNotNull("sortBefore",    SortBefore);
        includeIfNotNull("sortAfter",     SortAfter);
        includeIfNotNull("hideCode",      HideCode.ToString());
        includeIfNotNull("hideResources", HideResources.ToString());
        includeIfNotNull("includeSource", IncludeSource.ToString());
        includeIfNotNull("buildIgnore",   BuildIgnore);

        return properties;
    }

    public IEnumerable<ModReference> GetModReferences() {
        string   text  = File.ReadAllText(ModRefsPath);
        string[] lines = text.Split('\n');

        foreach (string line in lines) {
            string[] parts = line.Split(';');
            yield return new ModReference(parts[0], parts[1], !string.IsNullOrEmpty(parts[2]) && bool.Parse(parts[2]));
        }
    }

    public IEnumerable<AssemblyReference> GetAssemblyReferences() {
        string   text  = File.ReadAllText(AsmRefsPath);
        string[] lines = text.Split('\n');

        foreach (string line in lines) {
            string[] parts = line.Split(';');
            yield return new AssemblyReference(Path.GetFileNameWithoutExtension(parts[0]), parts[0], !string.IsNullOrEmpty(parts[1]) && bool.Parse(parts[1]));
        }
    }

    public IEnumerable<NuGetReference> GetPackageReferences() {
        string   text  = File.ReadAllText(NuGetRefsPath);
        string[] lines = text.Split('\n');

        foreach (string line in lines) {
            string[] parts = line.Split(';');
            yield return new NuGetReference(parts[0], parts[1], !string.IsNullOrEmpty(parts[2]) && bool.Parse(parts[2]));
        }
    }
}
