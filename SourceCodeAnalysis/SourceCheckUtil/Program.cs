﻿using System;
using System.Collections.Generic;
using System.Text;
using SourceCheckUtil.Analyzers;
using SourceCheckUtil.Args;
using SourceCheckUtil.Config;
using SourceCheckUtil.Output;
using SourceCheckUtil.Processors;

namespace SourceCheckUtil
{
    public class Program
    {
        public static Int32 Main(String[] args)
        {
            try
            {
                Boolean result = MainImpl(args);
                return result ? 0 : -1;
            }
            catch (PorterConfigException e)
            {
                Console.Error.WriteLine(e.Message);
                return -1;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
                return -1;
            }
        }

        private static Boolean MainImpl(String[] args)
        {
            AppArgs appArgs = AppArgsParser.Parse(args);
            switch (appArgs.Mode)
            {
                case AppUsageMode.Help:
                    Console.WriteLine(AppDescription);
                    return true;
                case AppUsageMode.Version:
                    Console.WriteLine(VersionNumber);
                    return true;
                case AppUsageMode.Analysis:
                    Console.OutputEncoding = Encoding.UTF8;
                    IConfig externalConfig = ConfigFactory.Create(appArgs);
                    if (externalConfig == null)
                    {
                        Console.Error.WriteLine(BadConfigMessage);
                        return false;
                    }
                    OutputImpl output = new OutputImpl(Console.Out, Console.Error, appArgs.OutputLevel);
                    ISourceProcessor processor = SourceProcessorFactory.Create(appArgs.Source, externalConfig, output);
                    IList<IFileAnalyzer> analyzers = AnalyzersFactory.Create(output);
                    Boolean processResult = processor.Process(analyzers);
                    output.WriteInfoLine($"Result of analysis: analysis is {(processResult ? "succeeded" : "failed")}");
                    return processResult;
                case AppUsageMode.BadSource:
                    Console.Error.WriteLine(BadSourceMessage);
                    return false;
                case AppUsageMode.BadConfig:
                    Console.Error.WriteLine(BadConfigMessage);
                    return false;
                case AppUsageMode.BadAppUsage:
                case AppUsageMode.Unknown:
                    Console.Error.WriteLine(BadUsageMessage);
                    Console.WriteLine(AppDescription);
                    return false;
                default:
                    throw new InvalidOperationException();
            }
        }

        private const String AppDescription = "Application usage:\r\n" +
                                              "1. {APP} --source={solution-filename.sln|project-filename.csproj|cs-filename.cs} [--config={config-file|config-dir}] [--output-level={Error|Warning|Info}]\r\n" +
                                              "2. {APP} --help\r\n" +
                                              "3. {APP} --version\r\n" +
                                              "Default values:\r\n" +
                                              "1. output-level=Error";
        private const String BadUsageMessage = "[ERROR]: Bad usage of the application.";
        private const String BadSourceMessage = "[ERROR]: Bad/empty/unknown source path.";
        private const String BadConfigMessage = "[ERROR]: Bad/empty/unknown config path.";
        private const String VersionNumber = "0.9";
    }
}
