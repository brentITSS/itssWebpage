# Generate BCrypt hash for password "111"
# Run this in PowerShell

Add-Type -Path "path\to\BCrypt.Net.dll"  # This won't work without the DLL

# Better approach: Use online generator or create a small C# console app
Write-Host "Please use https://bcrypt-generator.com/ to generate the hash"
Write-Host "Or run this C# code in a console app:"
Write-Host ""
Write-Host "using BCrypt.Net;"
Write-Host "var hash = BCrypt.HashPassword(""111"");"
Write-Host "Console.WriteLine(hash);"
