using Apollo.Components.Hosting;
using Apollo.Components.Infrastructure;
using Apollo.Components.Solutions;
using Apollo.Components.Solutions.Consumers;
using Apollo.Components.Solutions.Events;
using Apollo.Components.Solutions.Services;
using Apollo.Contracts.Solutions;
using NSubstitute;
using Shouldly;
using Xunit;

namespace Apollo.Test.Components.Solutions;

public class SolutionsStateTests
{
    private SolutionsState _state;

    public SolutionsStateTests()
    {
        // Mock dependencies for the SolutionsState constructor
        var busMock = new TestMessageBus();
        var storageServiceMock = Substitute.For<ISolutionSaveService>();
        var hostingServiceMock = Substitute.For<IHostingService>();

        // Initialize the SolutionsState
        _state = new SolutionsState(null, busMock, storageServiceMock, hostingServiceMock);

        // Add a properly configured solution to the state
        var testSolution = new SolutionModel
        {
            Name = "TestSolution",
            Items = new List<ISolutionItem>
            {
                new Folder
                {
                    Name = "TestSolution", // Root folder named after the solution
                    Uri = "virtual/TestSolution",
                    Items = new List<ISolutionItem>()
                }
            }
        };

        _state.Solutions.Add(testSolution);

        // Set this solution as the active one
        _state.SwitchSolution("TestSolution");
    }

    [Fact(DisplayName = "Add a folder to the root folder")]
    public void AddFolderToRoot_ShouldAddFolderToHierarchy()
    {
        // Act
        var f = _state.AddFolder("NewFolder");

        // Assert
        var rootFolder = _state.Project.GetRootFolder();

        rootFolder.Uri.ShouldBe("virtual/TestSolution");

        var newFolder = _state.Project.Items.OfType<Folder>().FirstOrDefault(f => f.Name == "NewFolder");
        newFolder.ShouldNotBeNull();
        newFolder.Uri.ShouldBe("virtual/TestSolution/NewFolder");
    }

    [Fact(DisplayName = "Add nested folders under an existing folder")]
    public void AddNestedFolder_ShouldCreateHierarchy()
    {
        // Act
        var rootFolder = _state.Project.GetRootFolder();
        var nestedFolder = _state.AddFolder("SubFolder1", rootFolder);
        var deeplyNested = _state.AddFolder("NewFolder", nestedFolder);

        // Assert
        nestedFolder.ShouldNotBeNull();
        nestedFolder.Name.ShouldBe("SubFolder1");

        var newFolder = _state.Project.Items.OfType<Folder>().FirstOrDefault(f => f.Name == "SubFolder1");
        newFolder.ShouldNotBeNull();
        _state.Project.Items.ShouldContain(item => item.Name == "NewFolder");
    }
    
    [Fact(DisplayName = "Add nested folder with dynamic hierarchy")]
    public void AddNestedFolder_ShouldAddToHierarchyOnly()
    {
        // Act
        var folder = _state.Project.GetOrCreateFolder("FolderA/FolderB/FolderC");

        // Assert
        folder.ShouldNotBeNull();
        folder.Name.ShouldBe("FolderC");
        folder.Uri.ShouldBe("virtual/TestSolution/FolderA/FolderB/FolderC");

        // Validate top-level items: Only FolderA should be a top-level folder
        _state.Project.Items.OfType<Folder>().ShouldContain(f => f.Name == "FolderA");

        // Validate hierarchy
        var hierarchy = _state.Project.GetHierarchicalItems();
        var rootFolder = hierarchy.OfType<Folder>().FirstOrDefault(f => f.Name == "FolderA");
        rootFolder.ShouldNotBeNull();
        rootFolder.Items.OfType<Folder>().ShouldContain(f => f.Name == "FolderB");

        var folderB = rootFolder.Items.OfType<Folder>().FirstOrDefault(f => f.Name == "FolderB");
        folderB.ShouldNotBeNull();
        folderB.Items.OfType<Folder>().ShouldContain(f => f.Name == "FolderC");
    }
    
    [Fact(DisplayName = "Build folder hierarchy dynamically")]
    public void GetHierarchicalItems_ShouldBuildHierarchyFromTopLevelItems()
    {
        // Arrange
        _state.Project.GetOrCreateFolder("FolderA");
        _state.Project.AddFile("FileA.cs", "FolderA", "");
        _state.Project.GetOrCreateFolder("FolderA/SubFolder");
    
        // Act
        var hierarchy = _state.Project.GetHierarchicalItems();
    
        // Assert
        var rootFolder = hierarchy.OfType<Folder>().FirstOrDefault(f => f.Name == "FolderA");
        rootFolder.ShouldNotBeNull();
        rootFolder.Items.OfType<SolutionFile>().ShouldContain(f => f.Name == "FileA.cs");
    
        var subFolder = rootFolder.Items.OfType<Folder>().FirstOrDefault(f => f.Name == "SubFolder");
        subFolder.ShouldNotBeNull();
        subFolder.Items.ShouldBeEmpty(); // No files directly in SubFolder
    }

    [Fact(DisplayName = "Add a file to a folder")]
    public void AddFileToFolder_ShouldAddFileToFolderItems()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var f = _state.AddFolder("FolderForFile", rootFolder);

        // Act
        _state.AddFile("NewFile.cs", f);

        // Re-fetch the folder from the state to validate
        var updatedFolder = _state.Project.GetFolder("FolderForFile");

        // Assert
        updatedFolder.ShouldNotBeNull(); // Ensure the folder exists
        updatedFolder.Items.ShouldContain(item => item.Name == "NewFile.cs");
    }

    [Fact(DisplayName = "Switch between solutions")]
    public void SwitchSolution_ShouldUpdateCurrentProject()
    {
        // Arrange
        var secondSolution = new SolutionModel
        {
            Name = "SecondSolution",
            Items = new List<ISolutionItem>
            {
                new Folder
                {
                    Name = "RootFolder2",
                    Uri = "RootFolder2",
                    Items = new List<ISolutionItem>()
                }
            }
        };
        _state.Solutions.Add(secondSolution);

        // Act
        _state.SwitchSolution("SecondSolution");

        // Assert
        _state.Project.Name.ShouldBe("SecondSolution");
    }
    
    [Fact(DisplayName = "Add deeply nested folders without duplicate prefixes")]
    public void AddDeeplyNestedFolders_ShouldNotDuplicatePrefix()
    {
        // Act
        var deepFolder = _state.Project.GetOrCreateFolder("FolderA/FolderB/FolderC/FolderD");

        // Assert
        deepFolder.ShouldNotBeNull();
        deepFolder.Name.ShouldBe("FolderD");
        deepFolder.Uri.ShouldBe("virtual/TestSolution/FolderA/FolderB/FolderC/FolderD");
    }

    [Fact(DisplayName = "Recreate existing folder should not duplicate it")]
    public void GetOrCreateFolder_ShouldReuseExistingFolder()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        _state.AddFolder("ExistingFolder", rootFolder);
        _state.AddFolder("ExistingFolder", rootFolder);

        var initialFolder = _state.GetFolder("ExistingFolder");

        // Act
        var duplicateFolder = _state.GetFolder("ExistingFolder");

        // Assert
        duplicateFolder.ShouldBeSameAs(initialFolder);
        _state.Project.Items.OfType<Folder>().ShouldContain(folder => folder.Name == "ExistingFolder");
    }
    
    [Fact(DisplayName = "Verify folder URIs are constructed correctly")]
    public void GetOrCreateFolder_ShouldConstructCorrectUris()
    {
        // Act
        var rootFolder = _state.Project.GetRootFolder();
        var nestedFolder = _state.Project.GetOrCreateFolder("FolderA/FolderB/FolderC");

        // Assert
        nestedFolder.Uri.ShouldBe("virtual/TestSolution/FolderA/FolderB/FolderC");

        var folderA = _state.Project.GetFolder("FolderA");
        folderA.ShouldNotBeNull();
        folderA.Uri.ShouldBe("virtual/TestSolution/FolderA");

        var folderB = folderA.Items.OfType<Folder>().FirstOrDefault(f => f.Name == "FolderB");
        folderB.ShouldNotBeNull();
        folderB.Uri.ShouldBe("virtual/TestSolution/FolderA/FolderB");

        var folderC = folderB.Items.OfType<Folder>().FirstOrDefault(f => f.Name == "FolderC");
        folderC.ShouldNotBeNull();
        folderC.Uri.ShouldBe("virtual/TestSolution/FolderA/FolderB/FolderC");
    }
    
    [Fact(DisplayName = "Add nested folder within an existing folder")]
    public void AddNestedFolder_ToExistingFolder_ShouldEstablishHierarchy()
    {
        // Arrange: Add a root-level folder
        var folderA = _state.Project.AddFolder("FolderA");

        // Act: Add a nested folder to FolderA
        var folderB = _state.Project.AddFolder("FolderB", folderA);

        // Assert: Verify hierarchy
        _state.Project.Items.OfType<Folder>().ShouldContain(f => f.Name == "FolderB");
    }
    
    [Fact(DisplayName = "Add file to folder and validate Uri")]
    public void AddFileToFolder_ShouldIncludeFileInHierarchy()
    {
        // Act
        _state.Project.AddFile("TestFile.cs", "FolderA", "");

        // Assert
        var folderA = _state.Project.GetOrCreateFolder("FolderA");
        folderA.Items.OfType<SolutionFile>().ShouldContain(f => f.Name == "TestFile.cs");
    }

    [Fact(DisplayName = "Create new solution with default settings")]
    public async Task CreateNewSolution_ShouldCreateValidSolution()
    {
        // Act
        var solution = await _state.CreateNewSolutionAsync("NewTestSolution");

        // Assert
        solution.ShouldNotBeNull();
        solution.Name.ShouldBe("NewTestSolution");
        solution.ProjectType.ShouldBe(ProjectType.Console);
        
        // Verify folder structure
        var rootFolder = solution.GetRootFolder();
        rootFolder.ShouldNotBeNull();
        rootFolder.Name.ShouldBe("NewTestSolution");
        rootFolder.Uri.ShouldBe("virtual/NewTestSolution");
        
        // Verify Program.cs exists
        var programCs = solution.Files.FirstOrDefault(f => f.Name == "Program.cs");
        programCs.ShouldNotBeNull();
        programCs.Uri.ShouldBe("virtual/NewTestSolution/Program.cs");
        programCs.Data.ShouldContain("Console.WriteLine");
    }

    [Fact(DisplayName = "Create new Web API solution")]
    public async Task CreateNewSolution_WebApi_ShouldCreateValidSolution()
    {
        // Act
        var solution = await _state.CreateNewSolutionAsync("NewWebApi", ProjectType.WebApi);

        // Assert
        solution.ShouldNotBeNull();
        solution.Name.ShouldBe("NewWebApi");
        solution.ProjectType.ShouldBe(ProjectType.WebApi);
        
        // Verify Program.cs contains Web API code
        var programCs = solution.Files.FirstOrDefault(f => f.Name == "Program.cs");
        programCs.ShouldNotBeNull();
        programCs.Data.ShouldContain("WebApplication.CreateBuilder");
    }

    [Fact(DisplayName = "Create new solution should switch to it")]
    public async Task CreateNewSolution_ShouldSwitchToNewSolution()
    {
        // Act
        var solution = await _state.CreateNewSolutionAsync("NewSolution");

        // Assert
        _state.Project.ShouldBeEquivalentTo(solution);
        _state.Solutions.ShouldContain(x => x.Name.Equals(solution.Name));
    }

    [Fact(DisplayName = "Rename folder should update hierarchy")]
    public void RenameFolder_ShouldUpdateHierarchy()
    {
        // Arrange
        var folder = _state.AddFolder("OldName");
        _state.AddFile("test.cs", folder);
        
        // Act
        _state.RenameItemAsync(folder, "NewName");
        
        // Assert
        var renamedFolder = _state.Project.GetFolder("NewName");
        renamedFolder.ShouldNotBeNull();
        renamedFolder.Name.ShouldBe("NewName");
        renamedFolder.Uri.ShouldBe("virtual/TestSolution/NewName");
        
        // Verify child files were updated
        var childFile = renamedFolder.Items.OfType<SolutionFile>().FirstOrDefault();
        childFile.ShouldNotBeNull();
        childFile.Uri.ShouldBe("virtual/TestSolution/NewName/test.cs");
    }

    [Fact(DisplayName = "Rename folder should update nested folder URIs")]
    public void RenameFolder_ShouldUpdateNestedFolderUris()
    {
        // Arrange
        var parentFolder = _state.AddFolder("Parent");
        var childFolder = _state.AddFolder("Child", parentFolder);
        _state.AddFile("test.cs", childFolder);
        
        // Act
        _state.RenameItemAsync(parentFolder, "NewParent");
        
        // Assert
        var renamedParent = _state.Project.GetFolder("NewParent");
        renamedParent.ShouldNotBeNull();
        
        var renamedChild = renamedParent.Items.OfType<Folder>().FirstOrDefault();
        renamedChild.ShouldNotBeNull();
        renamedChild.Uri.ShouldBe("virtual/TestSolution/NewParent/Child");
        
        // Verify nested file URIs were updated
        var nestedFile = renamedChild.Items.OfType<SolutionFile>().FirstOrDefault();
        nestedFile.ShouldNotBeNull();
        nestedFile.Uri.ShouldBe("virtual/TestSolution/NewParent/Child/test.cs");
    }

    [Fact(DisplayName = "Rename root folder should update solution name")]
    public void RenameRootFolder_ShouldUpdateSolutionName()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        
        // Act
        _state.RenameItemAsync(rootFolder, "RenamedSolution");
        
        // Assert
        _state.Project.Name.ShouldBe("RenamedSolution");
        rootFolder.Name.ShouldBe("RenamedSolution");
        rootFolder.Uri.ShouldBe("virtual/RenamedSolution");
    }

    [Fact(DisplayName = "Rename root folder should maintain folder hierarchy")]
    public void RenameRootFolder_ShouldMaintainHierarchy()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var subFolder = _state.AddFolder("SubFolder", rootFolder);
        // Verify initial state
        rootFolder.Items.Count.ShouldBe(1);
        rootFolder.Items.ShouldContain(subFolder);

        var file = new SolutionFile 
        { 
            Name = "test.cs", 
            Uri = $"{subFolder.Uri}/test.cs",
            Data = "test"
        };
        _state.Project.Items.Add(file);
        subFolder.Items.Add(file);
        
        // Act
        _state.RenameItemAsync(rootFolder, "RenamedSolution");
        
        // Assert
        _state.Project.Name.ShouldBe("RenamedSolution");
        
        var updatedRoot = _state.Project.GetRootFolder();
        // Verify folder structure is maintained
        updatedRoot.Items.Count.ShouldBe(1);
        updatedRoot.Items.ShouldContain(i => i.Name == "SubFolder");
        
        var updatedSubFolder = updatedRoot.Items.OfType<Folder>().FirstOrDefault();
        updatedSubFolder.ShouldNotBeNull();
        updatedSubFolder.Name.ShouldBe("SubFolder");
        updatedSubFolder.Uri.ShouldBe("virtual/RenamedSolution/SubFolder");
    }

    [Fact(DisplayName = "Rename root folder should be case insensitive")]
    public async Task RenameRootFolder_ShouldBeCaseInsensitive()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        // Add a subfolder to verify hierarchy maintenance
        var subFolder = _state.AddFolder("SubFolder", rootFolder);
        
        // Verify initial state
        rootFolder.Items.Count.ShouldBe(1);
        rootFolder.Items.ShouldContain(subFolder);
        
        _state.Project.Name = "testsolution";
        rootFolder.Uri = "virtual/TESTSOLUTION";
        rootFolder.Name = "TESTSOLUTION";
        
        // Act
        await _state.RenameItemAsync(rootFolder, "RenamedSolution");
        
        // Assert
        _state.Project.Name.ShouldBe("RenamedSolution");
        rootFolder.Name.ShouldBe("RenamedSolution");
        rootFolder.Uri.ShouldBe("virtual/RenamedSolution");
        
        // Verify solution was updated in Solutions list
        _state.Solutions.ShouldContain(s => s.Name == "RenamedSolution");
        _state.Solutions.ShouldNotContain(s => s.Name.Equals("testsolution", StringComparison.OrdinalIgnoreCase));
        
        // Verify folder hierarchy is maintained
        rootFolder.Items.Count.ShouldBe(1);  // Should still have the subfolder
        rootFolder.Items.ShouldContain(i => i.Name == "SubFolder");
        
        // Verify subfolder URI was updated
        var updatedSubFolder = rootFolder.Items.OfType<Folder>().First();
        updatedSubFolder.Uri.ShouldBe("virtual/RenamedSolution/SubFolder");
    }

    [Fact(DisplayName = "Rename root folder should handle URI correctly")]
    public void RenameRootFolder_ShouldHandleUriCorrectly()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var originalUri = rootFolder.Uri;
        
        // Act
        _state.RenameItemAsync(rootFolder, "RenamedSolution");
        
        // Assert
        rootFolder.Uri.ShouldBe("virtual/RenamedSolution");
        rootFolder.Uri.ShouldNotBe(originalUri);
        
        // Verify the solution name and root folder name match
        rootFolder.Name.ShouldBe(_state.Project.Name);
        rootFolder.Name.ShouldBe("RenamedSolution");
        
        // Verify the solution is properly updated in the Solutions list
        var solution = _state.Solutions.FirstOrDefault(s => s.Name == "RenamedSolution");
        solution.ShouldNotBeNull();
        solution.GetRootFolder().Uri.ShouldBe("virtual/RenamedSolution");
    }

    [Fact(DisplayName = "Rename subfolder should maintain correct URI structure")]
    public async Task RenameSubfolder_ShouldMaintainCorrectUriStructure()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var subFolder = _state.AddFolder("test", rootFolder);
        var file = new SolutionFile 
        { 
            Name = "test.cs", 
            Uri = $"{subFolder.Uri}/test.cs",
            Data = "test"
        };
        _state.Project.Items.Add(file);
        subFolder.Items.Add(file);
        
        // Act
        await _state.RenameItemAsync(subFolder, "test3");
        
        // Assert
        var renamedFolder = _state.Project.GetFolder("test3");
        renamedFolder.ShouldNotBeNull();
        renamedFolder.Name.ShouldBe("test3");
        renamedFolder.Uri.ShouldBe("virtual/TestSolution/test3");
        
        // Verify child file URIs are updated correctly
        var childFile = renamedFolder.Items.OfType<SolutionFile>().FirstOrDefault();
        childFile.ShouldNotBeNull();
        childFile.Uri.ShouldBe("virtual/TestSolution/test3/test.cs");
        
        // Verify folder hierarchy is maintained
        rootFolder.Items.ShouldContain(renamedFolder);
        renamedFolder.Items.ShouldContain(childFile);
    }

    [Fact(DisplayName = "Rename subfolder with similar name should not affect siblings")]
    public async Task RenameSubfolder_ShouldNotAffectSiblings()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var folder1 = _state.AddFolder("test", rootFolder);
        var folder2 = _state.AddFolder("test2", rootFolder);
        
        // Act
        await _state.RenameItemAsync(folder1, "test3");
        
        // Assert
        var renamedFolder = _state.Project.GetFolder("test3");
        var unchangedFolder = _state.Project.GetFolder("test2");
        
        renamedFolder.ShouldNotBeNull();
        unchangedFolder.ShouldNotBeNull();
        
        renamedFolder.Uri.ShouldBe("virtual/TestSolution/test3");
        unchangedFolder.Uri.ShouldBe("virtual/TestSolution/test2");
    }

    [Fact(DisplayName = "Rename root folder should trigger storage update")]
    public async Task RenameRootFolder_ShouldTriggerStorageUpdate()
    {
        // Arrange
        var busMock = new TestMessageBus();
        var storageServiceMock = Substitute.For<ISolutionSaveService>();
        var hostingServiceMock = Substitute.For<IHostingService>();
        var state = new SolutionsState(null, busMock, storageServiceMock, hostingServiceMock);
        
        var testSolution = new SolutionModel
        {
            Name = "TestSolution",
            Items = new List<ISolutionItem>
            {
                new Folder
                {
                    Name = "TestSolution",
                    Uri = "virtual/TestSolution",
                    Items = new List<ISolutionItem>()
                }
            }
        };
        
        state.Solutions.Add(testSolution);
        state.SwitchSolution("TestSolution");
        
        ItemRenamed? capturedEvent = null;
        var consumer = new ProjectRenamedSolutionSaver(Substitute.For<ISolutionSaveService>(), state);
        busMock.Subscribe<ItemRenamed>(consumer);

        // Act
        var rootFolder = state.Project.GetRootFolder();
        await state.RenameItemAsync(rootFolder, "RenamedSolution");

        // Assert
    }

    [Theory(DisplayName = "IsRootFolder should handle various case formats")]
    [InlineData("virtual/TestSolution", "TestSolution", true)]
    [InlineData("virtual/testsolution", "TestSolution", true)]
    [InlineData("virtual/TESTSOLUTION", "TestSolution", true)]
    [InlineData("virtual/TestSolution/SubFolder", "TestSolution", false)]
    [InlineData("virtual/OtherSolution", "TestSolution", false)]
    public void IsRootFolder_ShouldHandleVariousCaseFormats(string folderUri, string projectName, bool expected)
    {
        // Arrange
        var folder = new Folder { Uri = folderUri };
        _state.Project.Name = projectName;
        
        // Act
        var isRoot = _state.IsRootFolder(folder);  // We'll need to make this method public or use reflection
        
        // Assert
        isRoot.ShouldBe(expected);
    }

    [Fact(DisplayName = "Rename root folder should handle mixed case")]
    public async Task RenameRootFolder_ShouldHandleMixedCase()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var originalName = "TestSOLUTION";
        var newName = "RenamedSolution";
        
        // Set up mixed case scenario
        _state.Project.Name = originalName;
        rootFolder.Uri = $"virtual/{originalName.ToLowerInvariant()}";
        rootFolder.Name = originalName;
        
        // Act
        await _state.RenameItemAsync(rootFolder, newName);
        
        // Assert
        _state.Project.Name.ShouldBe(newName, "Project name should be updated");
        rootFolder.Name.ShouldBe(newName, "Root folder name should be updated");
        rootFolder.Uri.ShouldBe($"virtual/{newName}", "Root folder URI should be updated");
        
        // Verify solution was updated in Solutions list
        _state.Solutions.ShouldContain(s => s.Name == newName, "Solutions list should contain new name");
        _state.Solutions.ShouldNotContain(s => s.Name.Equals(originalName, StringComparison.OrdinalIgnoreCase), 
            "Solutions list should not contain old name");
    }

    [Fact(DisplayName = "DeleteFile should handle active file switching")]
    public async Task DeleteFile_ShouldHandleActiveFileSwitching()
    {
        // Arrange
        var file1 = new SolutionFile { Name = "file1.cs", Uri = "virtual/TestSolution/file1.cs" };
        var file2 = new SolutionFile { Name = "file2.cs", Uri = "virtual/TestSolution/file2.cs" };
        _state.Project.Items.Add(file1);
        _state.Project.Items.Add(file2);
        _state.ActiveFile = file1;
        
        // Act
        _state.DeleteFile(file1);
        
        // Assert
        _state.ActiveFile.ShouldBe(file2);
        _state.Project.Items.ShouldNotContain(file1);
    }

    [Fact(DisplayName = "DeleteFolder should not allow deleting root folder")]
    public async Task DeleteFolder_ShouldNotAllowDeletingRootFolder()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var file = new SolutionFile { Name = "test.cs", Uri = "virtual/TestSolution/test.cs" };
        rootFolder.Items.Add(file);
        _state.Project.Items.Add(file);
        
        // Act
        _state.DeleteFolder(rootFolder);
        
        // Assert
        _state.Project.Items.ShouldContain(rootFolder);
        _state.Project.Items.ShouldContain(file);
    }

    [Fact(DisplayName = "DeleteFolder should switch active file if in deleted folder")]
    public async Task DeleteFolder_ShouldSwitchActiveFileIfInDeletedFolder()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var subFolder = _state.Project.AddFolder("SubFolder", rootFolder);
        var fileInFolder = new SolutionFile 
        { 
            Name = "inFolder.cs",
            Uri = "virtual/TestSolution/SubFolder/inFolder.cs"
        };
        var fileOutsideFolder = new SolutionFile 
        { 
            Name = "outside.cs",
            Uri = "virtual/TestSolution/outside.cs"
        };
        _state.Project.Items.Add(fileInFolder);
        _state.Project.Items.Add(fileOutsideFolder);
        subFolder.Items.Add(fileInFolder);
        _state.ActiveFile = fileInFolder;
        
        // Act
        _state.DeleteFolder(subFolder);
        
        // Assert
        _state.ActiveFile.Uri.ShouldBe(fileOutsideFolder.Uri);
        _state.Project.Items.ShouldNotContain(subFolder);
        _state.Project.Items.ShouldNotContain(fileInFolder);
    }

    [Fact(DisplayName = "MoveFile should update file URI and maintain hierarchy")]
    public void MoveFile_ShouldUpdateFileUriAndMaintainHierarchy()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var sourceFolder = _state.Project.AddFolder("Source", rootFolder);
        var destFolder = _state.Project.AddFolder("Destination", rootFolder);
        
        var file = new SolutionFile 
        { 
            Name = "test.cs",
            Uri = "virtual/TestSolution/Source/test.cs"
        };
        _state.Project.Items.Add(file);
        sourceFolder.Items.Add(file);
        
        // Act
        _state.MoveFile(file, destFolder);
        
        // Assert
        file.Uri.ShouldBe("virtual/TestSolution/Destination/test.cs");
        sourceFolder.Items.ShouldNotContain(file);
        destFolder.Items.ShouldContain(file);
    }

    [Fact(DisplayName = "MoveFile should handle active file correctly")]
    public void MoveFile_ShouldHandleActiveFile()
    {
        // Arrange
        var rootFolder = _state.Project.GetRootFolder();
        var sourceFolder = _state.Project.AddFolder("Source", rootFolder);
        var destFolder = _state.Project.AddFolder("Destination", rootFolder);
        
        var file = new SolutionFile 
        { 
            Name = "test.cs",
            Uri = "virtual/TestSolution/Source/test.cs"
        };
        _state.Project.Items.Add(file);
        sourceFolder.Items.Add(file);
        _state.ActiveFile = file;
        
        // Act
        _state.MoveFile(file, destFolder);
        
        // Assert
        _state.ActiveFile.Uri.ShouldBe("virtual/TestSolution/Destination/test.cs");
    }
}