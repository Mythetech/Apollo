namespace Apollo.Hosting;
using Apollo.Hosting.Logging;
using Microsoft.AspNetCore.Builder;
using System;
using System.Threading;
using System.Threading.Tasks;

public static class MinimalApiTransformer
{
    public static string WrapMinimalApi(string code)
    {
        return $$"""
        using Microsoft.AspNetCore.Builder;
        
        public class Program
        {
            public static void Main(string[] args)
            {
                {{code}}
            }
        }
        """;
    }
} 