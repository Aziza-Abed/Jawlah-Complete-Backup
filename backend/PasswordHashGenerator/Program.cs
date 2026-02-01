using System;
using Microsoft.AspNetCore.Identity;
using FollowUp.Core.Entities;

namespace PasswordHashGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            var hasher = new PasswordHasher<User>();
            var dummyUser = new User(); // PasswordHasher needs a user instance

            string adminPassword = "Admin123!";
            string supervisorPassword = "Super123!";
            string workerPassword = "Worker123!";

            string adminHash = hasher.HashPassword(dummyUser, adminPassword);
            string supervisorHash = hasher.HashPassword(dummyUser, supervisorPassword);
            string workerHash = hasher.HashPassword(dummyUser, workerPassword);

            Console.WriteLine("=== PASSWORD HASHES FOR SEED DATA ===");
            Console.WriteLine();
            Console.WriteLine("Admin Password: " + adminPassword);
            Console.WriteLine("Admin Hash:");
            Console.WriteLine(adminHash);
            Console.WriteLine();
            Console.WriteLine("Supervisor Password: " + supervisorPassword);
            Console.WriteLine("Supervisor Hash:");
            Console.WriteLine(supervisorHash);
            Console.WriteLine();
            Console.WriteLine("Worker Password: " + workerPassword);
            Console.WriteLine("Worker Hash:");
            Console.WriteLine(workerHash);
            Console.WriteLine();
            Console.WriteLine("=== SQL DECLARATIONS ===");
            Console.WriteLine();
            Console.WriteLine($"DECLARE @AdminPasswordHash NVARCHAR(MAX) = '{adminHash}';");
            Console.WriteLine($"DECLARE @SupervisorPasswordHash NVARCHAR(MAX) = '{supervisorHash}';");
            Console.WriteLine($"DECLARE @WorkerPasswordHash NVARCHAR(MAX) = '{workerHash}';");

            // Test verification
            Console.WriteLine();
            Console.WriteLine("=== VERIFICATION TEST ===");
            var adminVerify = hasher.VerifyHashedPassword(dummyUser, adminHash, adminPassword);
            var supVerify = hasher.VerifyHashedPassword(dummyUser, supervisorHash, supervisorPassword);
            var workerVerify = hasher.VerifyHashedPassword(dummyUser, workerHash, workerPassword);
            Console.WriteLine($"Admin verify: {adminVerify}");
            Console.WriteLine($"Supervisor verify: {supVerify}");
            Console.WriteLine($"Worker verify: {workerVerify}");
        }
    }
}
