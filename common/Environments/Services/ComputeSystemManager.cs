﻿// Copyright(c) Microsoft Corporation and Contributors
// Licensed under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevHome.Common.Contracts.Services;
using DevHome.Common.Environments.Models;
using DevHome.Common.Helpers;
using DevHome.Common.Models;
using DevHome.Common.Services;
using Microsoft.Windows.DevHome.SDK;

namespace DevHome.Common.Environments.Services;

/// <summary>
/// Service thats used to get the ComputeSystems from the providers so they can be loaded into the UI.
/// This class is also used to keep track of the ComputeSystem that a configuration file will be applied to.
/// </summary>
public class ComputeSystemManager : IComputeSystemManager
{
    private readonly IComputeSystemService _computeSystemService;

    // Used in the setup flow to store the ComputeSystem needed to configure.
    public ComputeSystemReviewItem? ComputeSystemSetupItem { get;  set; }

    public ComputeSystemManager(IComputeSystemService computeSystemService)
    {
        _computeSystemService = computeSystemService;
    }

    /// <summary>
    /// This method gets the ComputeSystems from the providers in parallel.
    /// </summary>
    public async Task GetComputeSystemsAsync(Func<ComputeSystemsLoadedData, Task> callback)
    {
        // Create a cancellation token that will cancel the task after 2 minute.
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(2));
        var token = cancellationTokenSource.Token;
        var computeSystemsProviders = await _computeSystemService.GetComputeSystemProvidersAsync();

        try
        {
            // get compute systems from providers in parallel.
            await Parallel.ForEachAsync(computeSystemsProviders, async (providerAndDevIdKeyPair, token) =>
            {
                var provider = new ComputeSystemProvider(providerAndDevIdKeyPair.Key);
                var devIdWrappers = new List<DeveloperIdWrapper>();
                var results = new List<ComputeSystemsResult>();
                var wrapperDictionary = new Dictionary<ComputeSystemProvider, List<DeveloperIdWrapper>>
                {
                    { provider, new () },
                };

                foreach (var devId in providerAndDevIdKeyPair.Value)
                {
                    var result = await providerAndDevIdKeyPair.Key.GetComputeSystemsAsync(devId, string.Empty);
                    devIdWrappers.Add(new DeveloperIdWrapper(devId));
                    wrapperDictionary[provider].Add(new DeveloperIdWrapper(devId));
                    results.Add(result);
                }

                var loadedData = new ComputeSystemsLoadedData(wrapperDictionary.First(), results);
                await callback(loadedData);
            });
        }
        catch (AggregateException aggregateEx)
        {
            foreach (var innerEx in aggregateEx.InnerExceptions)
            {
                if (innerEx is TaskCanceledException)
                {
                    Log.Logger()?.ReportError($"Failed to get retrieve all compute systems from all compute system providers due to cancellation", innerEx);
                }
                else
                {
                    Log.Logger()?.ReportError($"Failed to get retrieve all compute systems from all compute system providers ", innerEx);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger()?.ReportError($"Failed to get retrieve all compute systems from all compute system providers ", ex);
        }
    }
}
