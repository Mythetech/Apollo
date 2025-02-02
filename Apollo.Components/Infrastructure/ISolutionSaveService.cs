using Apollo.Components.Solutions;

namespace Apollo.Components.Infrastructure;

public interface ISolutionSaveService
{
    public Task RemoveSolutionAsync(string solutionName);
    
    public Task SaveSolutionAsync(SolutionModel solution);
    
    public Task SaveSolutionsAsync(IEnumerable<SolutionModel> solutions);

    public Task<List<SolutionModel>> GetSolutionsAsync();
    
    public Task<SolutionModel?> GetSolutionAsync(string solutionName);
}