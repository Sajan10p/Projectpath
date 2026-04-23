using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Projectpath.Models;

namespace Projectpath.Data
{
    public static class DbSeeder
    {
        public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

            // ================= ROLES =================
            string[] roles = { "Admin", "Student", "Tutor", "Company" };

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // ================= ADMIN =================
            var adminEmail = "admin@projectpath.com";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var user = new ApplicationUser
                {
                    FullName = "System Admin",
                    UserName = adminEmail,
                    Email = adminEmail,
                    UserRole = "Admin",
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, "Password1!");

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Admin");
                }
            }

            // ================= EXISTING COMPANY USER =================
            var companyEmail = "Company1@gmail.com";
            var companyUser = await userManager.FindByEmailAsync(companyEmail);

            if (companyUser == null)
            {
                return;
            }

            if (!await userManager.IsInRoleAsync(companyUser, "Company"))
            {
                await userManager.AddToRoleAsync(companyUser, "Company");
            }

            // ================= NEW DEMO PROJECTS =================
            var demoProjects = new List<Project>
            {
                new Project
                {
                    Title = "Smart Internship Matching Platform",
                    Description = "Develop a platform that matches students with internship opportunities based on skills, interests, and academic background.",
                    Requirements = "Student profile management, company posting system, matching algorithm, admin dashboard",
                    CompanyId = companyUser.Id,
                    IsApproved = true,
                    Status = "Approved",
                    ContactPersonName = "Olivia Carter",
                    ContactPhone = "0412348801",
                    ContactPersonEmail = "olivia@internmatch.com",
                    ExpectedDurationWeeks = 8,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "Clinic Appointment Booking System",
                    Description = "Create a web-based system for patients to book appointments, manage schedules, and receive confirmations.",
                    Requirements = "Appointment calendar, patient registration, doctor schedule management, email confirmation",
                    CompanyId = companyUser.Id,
                    IsApproved = true,
                    Status = "Approved",
                    ContactPersonName = "Dr Sarah Ahmed",
                    ContactPhone = "0412456721",
                    ContactPersonEmail = "sarah@cityclinic.com",
                    ExpectedDurationWeeks = 6,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "Construction Project Tracking Dashboard",
                    Description = "Build a dashboard for tracking construction tasks, deadlines, milestones, and team updates.",
                    Requirements = "Task management, progress tracking, milestone updates, reporting dashboard",
                    CompanyId = companyUser.Id,
                    IsApproved = true,
                    Status = "Approved",
                    ContactPersonName = "Michael Thomson",
                    ContactPhone = "0420112233",
                    ContactPersonEmail = "michael@buildtrack.com",
                    ExpectedDurationWeeks = 10,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "Online Event Ticketing Portal",
                    Description = "Develop a platform for event organizers to publish events and users to book tickets online.",
                    Requirements = "Event creation, ticket booking, payment integration, admin reporting",
                    CompanyId = companyUser.Id,
                    IsApproved = false,
                    Status = "Pending",
                    ContactPersonName = "Emma Lewis",
                    ContactPhone = "0433556611",
                    ContactPersonEmail = "emma@eventhub.com",
                    ExpectedDurationWeeks = 7,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "Library Resource Management System",
                    Description = "Create a system to manage books, digital resources, borrowing records, and overdue notices.",
                    Requirements = "Book catalog, borrowing system, overdue tracking, librarian dashboard",
                    CompanyId = companyUser.Id,
                    IsApproved = true,
                    Status = "Approved",
                    ContactPersonName = "Daniel Brooks",
                    ContactPhone = "0411994433",
                    ContactPersonEmail = "daniel@libsmart.com",
                    ExpectedDurationWeeks = 5,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "Hotel Reservation and Check-in System",
                    Description = "Develop a hotel management platform for room booking, guest check-in, and billing.",
                    Requirements = "Room booking, guest records, billing module, receptionist dashboard",
                    CompanyId = companyUser.Id,
                    IsApproved = false,
                    Status = "Pending",
                    ContactPersonName = "Sophia Green",
                    ContactPhone = "0425678901",
                    ContactPersonEmail = "sophia@stayeasy.com",
                    ExpectedDurationWeeks = 8,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "Warehouse Inventory Monitoring System",
                    Description = "Build a system for tracking warehouse stock levels, incoming shipments, and dispatch records.",
                    Requirements = "Inventory tracking, shipment logs, alerts for low stock, reporting",
                    CompanyId = companyUser.Id,
                    IsApproved = true,
                    Status = "Assigned",
                    ContactPersonName = "Lucas Martin",
                    ContactPhone = "0413789456",
                    ContactPersonEmail = "lucas@stockflow.com",
                    ExpectedDurationWeeks = 9,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "School Parent Communication Portal",
                    Description = "Create a communication platform for schools to share notices, attendance, and progress updates with parents.",
                    Requirements = "Notice board, attendance updates, student progress reports, parent login",
                    CompanyId = companyUser.Id,
                    IsApproved = true,
                    Status = "Approved",
                    ContactPersonName = "Rachel Moore",
                    ContactPhone = "0414442200",
                    ContactPersonEmail = "rachel@schoolconnect.com",
                    ExpectedDurationWeeks = 6,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "Gym Membership and Trainer Booking System",
                    Description = "Develop a platform for managing gym memberships, trainer bookings, and fitness session schedules.",
                    Requirements = "Membership plans, booking system, trainer schedule, payment records",
                    CompanyId = companyUser.Id,
                    IsApproved = false,
                    Status = "Pending",
                    ContactPersonName = "Nathan Cole",
                    ContactPhone = "0429981122",
                    ContactPersonEmail = "nathan@fitzone.com",
                    ExpectedDurationWeeks = 7,
                    CreatedAt = DateTime.Now
                },

                new Project
                {
                    Title = "NGO Volunteer Coordination Platform",
                    Description = "Build a volunteer management system for NGOs to manage campaigns, volunteers, and task assignments.",
                    Requirements = "Volunteer registration, campaign tracking, task assignment, notifications",
                    CompanyId = companyUser.Id,
                    IsApproved = true,
                    Status = "Approved",
                    ContactPersonName = "Grace Wilson",
                    ContactPhone = "0418234567",
                    ContactPersonEmail = "grace@helpinghands.org",
                    ExpectedDurationWeeks = 8,
                    CreatedAt = DateTime.Now
                }
            };

            

            context.Projects.AddRange(demoProjects);
            await context.SaveChangesAsync();
        }
    }
}