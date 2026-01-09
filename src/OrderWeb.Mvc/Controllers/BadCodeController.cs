// ⚠️ INTENTIONALLY BAD CODE FOR TESTING PIPELINE
// This file contains code quality issues that should be caught by the pipeline

using Microsoft.AspNetCore.Mvc;
using System;

namespace OrderWeb.Mvc.Controllers;

public class BadCodeController : Controller
{

    // ⚠️ WARNING: Unused field
    private string _unusedField = "This field is never used";

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

    // ⚠️ WARNING: Method with code quality issues
    public IActionResult MethodWithSyntaxError()
    {
        if (true)
        {
            return View();
        }
        return View();
    }

    // ⚠️ WARNING: Nullable reference type without null check
    public IActionResult MethodWithNullIssue(string? nullableString)
    {
        int length = nullableString.Length; // Potential null reference
        return View();
    }


    // ⚠️ WARNING: Async method without await
    public async Task<IActionResult> AsyncMethodWithoutAwait()
    {
        Task.Delay(1000); // Should use await
        return View();
    }

    // ⚠️ WARNING: Method parameter never used
    public IActionResult MethodWithUnusedParameter(int unusedParam)
    {
        return View();
    }
}

