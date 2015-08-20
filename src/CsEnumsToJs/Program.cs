﻿using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;

namespace CsEnumsToJs
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Cmd.WriteLine("C# Enum to JS converter");

            try
            {
                if (args.Length != 4)
                {
                    Cmd.WriteErrorLine("Incorrect number of arguments");
                    Cmd.WriteLine("Usage: csenumstojs <path to .dll> <output filename> <namespace> <include attribute>");
                    Cmd.WriteLine("Enums in the DLL will be scanned and written to the output file under the provided namespace.");
                    Cmd.WriteLine("Eg. csenumstojs foo\\bin\\debug\\foo.dll ui\\enums.js foo JsEnum");
                    return 1;
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
                builder.AppendLine($"window.{jsNamespace}.enums = window.{jsNamespace}.enums || {{}};");
                builder.AppendLine($"window.{jsNamespace}.Enum = function() {{");
                builder.AppendLine($"	var self = this;");
                builder.AppendLine($"	self.__descriptions = [];");
                builder.AppendLine($"   self.getDescription = function(val){{ return self.__descriptions[val]; }};");
                builder.AppendLine($"   self.__map = [];");
                builder.AppendLine($"   self.getAll = function() {{ return self.__map; }};");
                builder.AppendLine($"}}");
                builder.AppendLine($"window.{jsNamespace}.Enum.prototype.add = function(name, val, description) {{");
                builder.AppendLine($"	var self = this;");
                builder.AppendLine($"	self[name] = val;");
                builder.AppendLine($"	self[val] = name;");
                builder.AppendLine($"	self.__ids[val] = name;");
                builder.AppendLine($"	self.__descriptions[val] = description;");
                builder.AppendLine($"   self.__map.push({{ id: val, name: name, description: description }});");
                builder.AppendLine($"   return this;");
                builder.AppendLine($"}}");

                foreach (var type in enumTypes)
                {
                    Cmd.WriteLine($"Exporting {type.Name}...");
                    builder.AppendLine($"window.{jsNamespace}.enums.{type.Name.ToCamelCase()} = (new window.{jsNamespace}.Enum())");
                    var underlyingType = Enum.GetUnderlyingType(type);
                    foreach (var value in Enum.GetValues(type).Cast<object>())
                    {
                        var description = GetDescription(type, value);
                        var underlyingValue = Convert.ChangeType(value, underlyingType);
                        builder.AppendLine($"\t.add(\"{value.ToString().ToCamelCase()}\", {underlyingValue}, \"{description}\")");
                    }
                    builder.AppendLine("\t;");
                }

                Cmd.WriteLine("Writing...");
                File.WriteAllText(outputFilename, builder.ToString());
            }
            catch (ReflectionTypeLoadException e)
            {
                return 1;
            }

            Cmd.WriteSuccessLine("Done");

            return 0;
        }

        private static string GetDescription(Type enumType, object value)
        {
            var memberInfo = enumType.GetMember(value.ToString())[0];
            var description = memberInfo.GetCustomAttributes<DescriptionAttribute>().FirstOrDefault();
            return description?.Description ?? Wordify(value.ToString());
        }

        static string Wordify(string value)
        {
            var words =
                from word in Regex.Split(value, "([A-Z]+[a-z]+)")
                where !string.IsNullOrEmpty(word)
                select word;
            return string.Join(" ", words.ToArray());
        }
    }
}