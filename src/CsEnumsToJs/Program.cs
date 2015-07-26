﻿using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CsEnumsToJs
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            Cmd.WriteLine("C# Enum to JS converter");

            if (args.Length != 4)
            {
                Cmd.WriteErrorLine("Incorrect number of arguments");
                Cmd.WriteLine("Usage: csenumstojs <path to .dll> <output filename> <namespace> <include attribute>");
                Cmd.WriteLine("Enums in the DLL will be scanned and written to the output file under the provided namespace.");
                Cmd.WriteLine("Eg. csenumstojs foo\\bin\\debug\\foo.dll ui\\enums.js foo JsEnum");
                return;
            }

            var source = args[0];
            var jsNamespace = args[1];
            var outputFilename = args[2];
            var absoluteSourcePath = Path.GetFullPath(source);
            var includeAttribute = args[3];

            Cmd.WriteLine($"Source: {absoluteSourcePath}");
            Cmd.WriteLine($"Namespace: {jsNamespace}");
            Cmd.WriteLine($"Output: {outputFilename}");
            Cmd.WriteLine($"Include attribute: {includeAttribute}");

            Cmd.WriteLine("Reading assembly...");

            var sourceAssembly = Assembly.LoadFile(absoluteSourcePath);

            var enumTypes = (
                from type in sourceAssembly.GetTypes()
                where type.IsEnum
                where type.CustomAttributes.Any(x => x.AttributeType.Name.StartsWith(includeAttribute))
                select type
                ).ToArray();

            Cmd.WriteInfoLine($"Found {enumTypes.Count()} enums available for export");
            Cmd.WriteLine("Generating JS...");

            var builder = new StringBuilder();
            builder.AppendLine($"/* Auto-generated. Do not edit this file. */");
            builder.AppendLine($"window.{jsNamespace} = window.{jsNamespace} || {{}};");
            builder.AppendLine($"window.{jsNamespace}.enums = window.{jsNamespace}.enums || [];");
            builder.AppendLine($"window.{jsNamespace}.Enum = function() {{");
            builder.AppendLine($"	this.__descriptions = [];");
            builder.AppendLine($"	this.__ids = [];");
            builder.AppendLine($"	this.__last_value = 0;");
            builder.AppendLine($"}}");
            builder.AppendLine($"window.{jsNamespace}.Enum.prototype.add = function(name, val) {{");
            builder.AppendLine($"	if(val == undefined) val = ++this.__last_value;");
            builder.AppendLine($"	this[name] = val;");
            builder.AppendLine($"	this[val] = name;");
            builder.AppendLine($"	this.__ids[val] = name;");
            builder.AppendLine($"	this.__descriptions[val] = name.replace(/ShowWithEllipses$/,'...').replace(/([a-z])([A-Z])/g, '$1 $2').replace(/^\\s+/,'');");
            builder.AppendLine($"    return this;");
            builder.AppendLine($"}}");

            foreach (var type in enumTypes)
            {
                Cmd.WriteLine($"Exporting {type.Name}...");
                builder.AppendLine($"window.{jsNamespace}.enums.{type.Name} = (new Enum())");
                var underlyingType = Enum.GetUnderlyingType(type);
                foreach (var value in Enum.GetValues(type).Cast<object>())
                {
                    var underlyingValue = Convert.ChangeType(value, underlyingType);
                    builder.AppendLine($"\t.add(\"{value.ToString()}\", {underlyingValue})");
                }
                builder.AppendLine("\t;");
            }

            Cmd.WriteLine("Writing...");
            File.WriteAllText(outputFilename, builder.ToString());

            Cmd.WriteSuccessLine("Done");
        }
    }
}