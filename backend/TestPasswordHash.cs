using System;

class TestPasswordHash
{
    static void Main()
    {
        string password = "Admin@123";
        string hashFromSeedData = "$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6";

        Console.WriteLine("Testing BCrypt password verification...");
        Console.WriteLine($"Password: {password}");
        Console.WriteLine($"Hash: {hashFromSeedData}");

        bool isValid = BCrypt.Net.BCrypt.Verify(password, hashFromSeedData);

        Console.WriteLine($"Verification result: {isValid}");

        if (!isValid)
        {
            Console.WriteLine("\nGenerating correct hash for 'Admin@123':");
            string correctHash = BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
            Console.WriteLine($"Correct hash: {correctHash}");
        }
    }
}
