using Apollo.Components.Infrastructure;
using Blazored.LocalStorage;
using Microsoft.Extensions.Logging;

namespace Apollo.Components.Solutions.Services;

public class SolutionsStorageService : ISolutionSaveService
{
    private readonly ILocalStorageService _localStorageService;
    private readonly ILogger<SolutionsStorageService> _logger;
    private const string SolutionsPrefix = "__apollo_project"; 
    public SolutionsStorageService(ILocalStorageService localStorageService, ILogger<SolutionsStorageService> logger)
    {
        _localStorageService = localStorageService;
        _logger = logger;
    }

    public async Task RemoveSolutionAsync(string solutionName)
    {
        try
        {
            await _localStorageService.RemoveItemAsync($"{SolutionsPrefix}_{solutionName}");
        }
        catch (Exception e)
        {
            _logger.LogError(e.Message);
        }
    }

    public async Task SaveSolutionAsync(SolutionModel? solution)
    {
        if(solution == null) return;    
        
        await _localStorageService.SetItemAsync($"{SolutionsPrefix}_{solution.Name}" , solution);
    }

    public async Task SaveSolutionsAsync(IEnumerable<SolutionModel> solutions)
    {
        await Task.WhenAll(solutions.Select(SaveSolutionAsync));
    }

    public async Task<List<SolutionModel>> GetSolutionsAsync()
    {
        try
        {
            var keys = await _localStorageService.KeysAsync();
            var solutions = new List<SolutionModel>();

            foreach (string key in keys.Where(key => key.StartsWith(SolutionsPrefix)))
            {
                var solution = await _localStorageService.GetItemAsync<SolutionModel>(key);

                if (solution != null)
                    solutions.Add(solution);
            }

            return solutions;
        }
        catch (Exception)
        {
            await _localStorageService.ClearAsync();
            return [];
        }
    }

    public async Task<SolutionModel?> GetSolutionAsync(string solutionName)
    {
        return await _localStorageService.GetItemAsync<SolutionModel>($"{SolutionsPrefix}_{solutionName}");
    }
}