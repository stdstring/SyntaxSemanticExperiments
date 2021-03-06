﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using SourceCheckUtil.Analyzers;
using SourceCheckUtil.Config;
using SourceCheckUtil.Output;

namespace SourceCheckUtil.Processors
{
    internal class SolutionProcessor : ISourceProcessor
    {
        public SolutionProcessor(String solutionFilename, IConfig externalConfig, OutputImpl output)
        {
            if (String.IsNullOrEmpty(solutionFilename))
                throw new ArgumentNullException(nameof(solutionFilename));
            if (externalConfig == null)
                throw new ArgumentNullException(nameof(externalConfig));
            if (output == null)
                throw new ArgumentNullException(nameof(output));
            _solutionFilename = solutionFilename;
            _output = output;
            _processorHelper = new ProjectProcessorHelper(externalConfig, output);
        }

        public Boolean Process(IList<IFileAnalyzer> analyzers)
        {
            _output.WriteInfoLine($"Processing of the solution {_solutionFilename} is started");
            MSBuildWorkspace workspace = MSBuildWorkspace.Create();
            if (!File.Exists(_solutionFilename))
            {
                _output.WriteFailLine($"Bad (unknown) target {_solutionFilename}");
                return false;
            }
            Solution solution = workspace.OpenSolutionAsync(_solutionFilename).Result;
            Boolean result = true;
            foreach (ProjectId projectId in solution.GetProjectDependencyGraph().GetTopologicallySortedProjects())
            {
                Project project = solution.GetProject(projectId);
                result &= Process(project, analyzers);
            }
            _output.WriteInfoLine($"Processing of the solution {_solutionFilename} is finished");
            return result;
        }

        private Boolean Process(Project project, IList<IFileAnalyzer> analyzers)
        {
            _output.WriteInfoLine($"Processing of the project {project.FilePath} is started");
            Boolean result = _processorHelper.ProcessProject(project, analyzers, Process);
            _output.WriteInfoLine($"Processing of the project {project.FilePath} is finished");
            return result;
        }

        private Boolean Process(Document file, Compilation compilation, ConfigData externalData, IList<IFileAnalyzer> analyzers)
        {
            _output.WriteInfoLine($"Processing of the file {file.FilePath} is started");
            SyntaxTree tree = file.GetSyntaxTreeAsync().Result;
            SemanticModel model = compilation.GetSemanticModel(tree);
            Boolean result = _processorHelper.ProcessFile(file.FilePath, tree, model, externalData, analyzers);
            _output.WriteInfoLine($"Processing of the file {file.FilePath} is finished");
            return result;
        }

        private readonly String _solutionFilename;
        private readonly OutputImpl _output;
        private readonly ProjectProcessorHelper _processorHelper;
    }
}