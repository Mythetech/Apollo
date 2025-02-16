using System.ComponentModel.DataAnnotations;

namespace Apollo.Contracts.Solutions;

public enum ProjectType
{
    [Display(Name = "Console")]
    Console,
        
    [Display(Name = "Web API")]
    WebApi,

    [Display(Name = "Class Library")]
    ClassLibrary,

    [Display(Name = "Razor Class Library")]
    RazorClassLibrary
}