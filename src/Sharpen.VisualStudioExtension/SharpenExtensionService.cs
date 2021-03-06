﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.VisualStudio.LanguageServices;
using Sharpen.Engine;
using Sharpen.VisualStudioExtension.ToolWindows.AnalysisResultTreeViewBuilders;
using Sharpen.VisualStudioExtension.ToolWindows.AnalysisResultTreeViewItems;
using Task = System.Threading.Tasks.Task;

namespace Sharpen.VisualStudioExtension
{
    // TODO-IG: Find the best patterns for dependency injection and mediation in VS Extensions. Until then, this is going to be our Singleton God Object. Scarry.
    internal sealed class SharpenExtensionService : INotifyPropertyChanged
    {
        private readonly SharpenEngine sharpenEngine;

        private SharpenExtensionService(SharpenEngine sharpenEngine)
        {
            this.sharpenEngine = sharpenEngine;
            AnalysisResults = Enumerable.Empty<BaseTreeViewItem>();
        }

        // TODO-IG: Terrible and truly ugly :-( Mixing view aspects, logic, everything. But let's get the first analysis running and let's put the tool into practice and let's get some real-life results :-)
        private IEnumerable<BaseTreeViewItem> analysisResults;
        public IEnumerable<BaseTreeViewItem> AnalysisResults
        {
            get => analysisResults;
            private set
            {
                analysisResults = value;
                OnPropertyChanged();
            }
        }

        private bool analysisIsRunning;
        public bool AnalysisIsRunning
        {
            get => analysisIsRunning;
            private set
            {
                analysisIsRunning = value;
                OnPropertyChanged();
            }
        }

        private int analysisMaximumProgress;
        public int AnalysisMaximumProgress
        {
            get => analysisMaximumProgress;
            set
            {
                analysisMaximumProgress = value;
                OnPropertyChanged();
            }
        }

        private int analysisCurrentProgress;
        public int AnalysisCurrentProgress
        {
            get => analysisCurrentProgress;
            set
            {
                analysisCurrentProgress = Math.Min(value, AnalysisMaximumProgress);
                OnPropertyChanged();
            }
        }

        private static IEnumerable<BaseTreeViewItem> CreateAnalysisResultsTreeViewItemsFrom(IEnumerable<AnalysisResult> analysisResult)
        {
            var treeViewBuilder = new CSharpVersionSuggestionFilePathTreeViewBuilder(analysisResult);
            return treeViewBuilder.BuildRootTreeViewItems();
        }

        public static SharpenExtensionService Instance { get; private set; }

        public static void CreateSingleInstance(SharpenEngine sharpenEngine)
        {
            Instance = new SharpenExtensionService(sharpenEngine);
        }

        public async Task RunSolutionAnalysisAsync(VisualStudioWorkspace visualStudioWorkspace)
        {
            if (AnalysisIsRunning)
                throw new InvalidOperationException("An analysis is already running. You cannot run a new analysis until the current one is not finished.");

            AnalysisResults = Enumerable.Empty<BaseTreeViewItem>();

            AnalysisMaximumProgress = sharpenEngine.GetAnalysisMaximumProgress(visualStudioWorkspace);
            AnalysisCurrentProgress = 0;

            var progress = new Progress<int>();
            progress.ProgressChanged += UpdateAnalysisProgress;

            AnalysisIsRunning = true;

            var analysisResult = await sharpenEngine.AnalyzeAsync(visualStudioWorkspace, progress);
            AnalysisResults = CreateAnalysisResultsTreeViewItemsFrom(analysisResult);

            AnalysisMaximumProgress = 0;
            AnalysisCurrentProgress = 0;

            AnalysisIsRunning = false;
        }

        private void UpdateAnalysisProgress(object sender, int progressValue)
        {
            AnalysisCurrentProgress = progressValue;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}