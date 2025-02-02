using Apollo.Components.Solutions;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Solutions;

public class SolutionModelTests
{
    private SolutionModel _solution;
    
    public SolutionModelTests()
    {
        _solution = new SolutionModel();
    }
    
    [Fact(DisplayName = "GetOrCreateFolder - Add Nested Folder")]
    public void GetOrCreateFolder_ShouldAddNestedFolder()
    {
        // Act
        var folder = _solution.GetOrCreateFolder("FolderA/FolderB/FolderC");

        // Assert
        folder.ShouldNotBeNull();
        folder.Name.ShouldBe("FolderC");
        folder.Uri.ShouldBe("virtual/FolderA/FolderB/FolderC");

        var rootFolder = _solution.GetFolder("FolderA");
        rootFolder.ShouldNotBeNull();
        rootFolder.Items.ShouldContain(f => f.Name == "FolderB");
    }
    
    [Fact(DisplayName = "AddFile - Add File to Folder")]
    public void AddFile_ShouldAddFileToFolder()
    {
        // Arrange
        var solution = new SolutionModel("Test");
        var folder = solution.GetOrCreateFolder("TestFolder");

        // Act
        solution.AddFile("TestFile.cs", folder, "content");

        // Assert
        folder.Items.ShouldContain(f => f.Name == "TestFile.cs");
    }
    
    [Fact(DisplayName = "GetHierarchicalItems - Validate Structure")]
    public void GetHierarchicalItems_ShouldReturnCorrectStructure()
    {
        // Arrange
        var folderA = _solution.GetOrCreateFolder("FolderA");
        _solution.AddFile("FileA.cs", folderA);
        
        var sf = _solution.AddFolder("SubFolder", folderA);
        _solution.AddFile("FileB.cs", sf);

        // Act
        var items = _solution.GetHierarchicalItems();

        // Assert
        var rootFolder = items.OfType<Folder>().FirstOrDefault(f => f.Name == "FolderA");
        rootFolder.ShouldNotBeNull();
        rootFolder.Items.ShouldContain(f => f.Name == "FileA.cs");

        var subFolder = items.OfType<Folder>().FirstOrDefault(f => f.Name == "SubFolder");
        subFolder.ShouldNotBeNull();
        subFolder.Items.ShouldContain(f => f .Name == "FileB.cs");
    }

    [Fact(DisplayName = "DeleteFile should remove file from Items")]
    public void DeleteFile_ShouldRemoveFileFromItems()
    {
        // Arrange
        var solution = new SolutionModel { Name = "TestSolution" };
        var file = new SolutionFile 
        { 
            Name = "test.cs",
            Uri = "virtual/TestSolution/test.cs"
        };
        solution.Items.Add(file);
        
        // Act
        solution.DeleteFile(file);
        
        // Assert
        solution.Items.ShouldNotContain(file);
    }

    [Fact(DisplayName = "DeleteFolder should remove folder and all contained items")]
    public void DeleteFolder_ShouldRemoveFolderAndContents()
    {
        // Arrange
        var solution = new SolutionModel { Name = "TestSolution" };
        var rootFolder = solution.GetRootFolder();
        var subFolder = solution.AddFolder("SubFolder", rootFolder);
        var file = new SolutionFile 
        { 
            Name = "test.cs",
            Uri = "virtual/TestSolution/SubFolder/test.cs"
        };
        solution.Items.Add(file);
        subFolder.Items.Add(file);
        
        // Act
        solution.DeleteFolder(subFolder);
        
        // Assert
        solution.Items.ShouldNotContain(subFolder);
        solution.Items.ShouldNotContain(file);
    }
}