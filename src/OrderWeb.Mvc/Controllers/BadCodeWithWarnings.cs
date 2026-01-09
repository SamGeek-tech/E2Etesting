// ⚠️ INTENTIONALLY BAD CODE FOR TESTING PIPELINE
// This file contains code quality WARNINGS (compiles but has issues)

using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace OrderWeb.Mvc.Controllers;

public class BadCodeWithWarnings : Controller
{
    // ⚠️ WARNING: Unused field
    private string _unusedField = "This field is never used";
    private int _anotherUnusedField = 42;

    // ⚠️ WARNING: Obsolete method
    [Obsolete("This method is deprecated and should not be used")]
    public IActionResult OldMethod()
    {
        return View();
    }

    // ⚠️ WARNING: Unused variable
    public IActionResult MethodWithUnusedVariable()
    {
        int unusedVariable = 42;
        string anotherUnused = "test";
        return View();
    }

    // ⚠️ WARNING: Variable assigned but never used
    public IActionResult MethodWithUnusedAssignment()
    {
        int x = 10;
        x = 20; // Assigned but never read
        return View();
    }

    // ⚠️ WARNING: Nullable reference type without null check
    public IActionResult MethodWithNullIssue(string? nullableString)
    {
        // This will produce a warning about potential null reference
        if (nullableString != null)
        {
            int length = nullableString.Length;
            return View();
        }
        return View();
    }

    // ⚠️ WARNING: Async method without await
    public async Task<IActionResult> AsyncMethodWithoutAwait()
    {
        Task.Delay(1000); // Should use await - will produce warning
        return View();
    }

    // ⚠️ WARNING: Method parameter never used
    public IActionResult MethodWithUnusedParameter(int unusedParam)
    {
        return View();
    }

    // ⚠️ WARNING: Unreachable code
    public IActionResult MethodWithUnreachableCode()
    {
        return View();
        int x = 10; // Unreachable code
        string y = "test"; // Unreachable code
    }

    // ⚠️ WARNING: Empty catch block
    public IActionResult MethodWithEmptyCatch()
    {
        try
        {
            int x = int.Parse("test");
        }
        catch
        {
            // Empty catch block - swallows exception
        }
        return View();
    }
}

