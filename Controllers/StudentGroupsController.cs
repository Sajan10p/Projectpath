using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Projectpath.Data;
using Projectpath.Models;
using Projectpath.Services;

namespace Projectpath.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentGroupsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly EmailService _emailService;
        private readonly NotificationService _notificationService;

        public StudentGroupsController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            EmailService emailService,
            NotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> MyGroup()
        {
            var user = await _userManager.GetUserAsync(User);

            var membership = await _context.GroupMembers
                .Include(m => m.StudentGroup)!
                    .ThenInclude(g => g.Project)
                .Include(m => m.StudentGroup)!
                    .ThenInclude(g => g.Leader)
                .Include(m => m.StudentGroup)!
                    .ThenInclude(g => g.Members)
                    .ThenInclude(m => m.Student)
                .FirstOrDefaultAsync(m => m.StudentId == user!.Id);

            var availableStudents = new List<AvailableStudentViewModel>();
            var joinRequests = new List<GroupJoinRequest>();
            var leaveRequests = new List<GroupLeaveRequest>();
            bool hasPendingLeaveRequest = false;

            if (membership != null)
            {
                hasPendingLeaveRequest = await _context.GroupLeaveRequests
                    .AnyAsync(r => r.StudentGroupId == membership.StudentGroupId &&
                                   r.StudentId == user.Id &&
                                   r.Status == "Pending");
            }

            if (membership != null && membership.IsLeader)
            {
                var allStudents = await _userManager.GetUsersInRoleAsync("Student");

                var currentGroupMemberIds = membership.StudentGroup!.Members.Select(m => m.StudentId).ToList();
                var allGroupedStudentIds = await _context.GroupMembers.Select(m => m.StudentId).ToListAsync();
                var pendingInviteStudentIds = await _context.GroupInvites
                    .Where(i => i.StudentGroupId == membership.StudentGroupId && i.Status == "Pending")
                    .Select(i => i.InvitedStudentId)
                    .ToListAsync();
                var pendingRequestStudentIds = await _context.GroupJoinRequests
                    .Where(r => r.StudentGroupId == membership.StudentGroupId && r.Status == "Pending")
                    .Select(r => r.StudentId)
                    .ToListAsync();
                var pendingLeaveStudentIds = await _context.GroupLeaveRequests
                    .Where(r => r.StudentGroupId == membership.StudentGroupId && r.Status == "Pending")
                    .Select(r => r.StudentId)
                    .ToListAsync();

                availableStudents = allStudents
                    .Where(s =>
                        s.Id != user.Id &&
                        !string.IsNullOrWhiteSpace(s.StudentNumber) &&
                        !currentGroupMemberIds.Contains(s.Id) &&
                        !allGroupedStudentIds.Contains(s.Id) &&
                        !pendingInviteStudentIds.Contains(s.Id) &&
                        !pendingRequestStudentIds.Contains(s.Id) &&
                        !pendingLeaveStudentIds.Contains(s.Id))
                    .Select(s => new AvailableStudentViewModel
                    {
                        Id = s.Id,
                        FullName = s.FullName,
                        Email = s.Email ?? "",
                        StudentNumber = s.StudentNumber ?? ""
                    })
                    .OrderBy(s => s.StudentNumber)
                    .ToList();

                joinRequests = await _context.GroupJoinRequests
                    .Include(r => r.Student)
                    .Where(r => r.StudentGroupId == membership.StudentGroupId && r.Status == "Pending")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                leaveRequests = await _context.GroupLeaveRequests
                    .Include(r => r.Student)
                    .Where(r => r.StudentGroupId == membership.StudentGroupId && r.Status == "Pending")
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();
            }

            ViewBag.AvailableStudents = availableStudents;
            ViewBag.JoinRequests = joinRequests;
            ViewBag.LeaveRequests = leaveRequests;
            ViewBag.HasPendingLeaveRequest = hasPendingLeaveRequest;

            return View(membership);
        }

        public async Task<IActionResult> AvailableGroups()
        {
            var user = await _userManager.GetUserAsync(User);
            var alreadyInGroup = await _context.GroupMembers.AnyAsync(m => m.StudentId == user!.Id);

            if (alreadyInGroup)
            {
                TempData["Error"] = "You are already in a group.";
                return RedirectToAction("MyGroup");
            }

            var pendingRequests = await _context.GroupJoinRequests
                .Where(r => r.StudentId == user.Id && r.Status == "Pending")
                .Select(r => r.StudentGroupId)
                .ToListAsync();

            var groups = await _context.StudentGroups
                .Include(g => g.Project)
                .Include(g => g.Leader)
                .Include(g => g.Members)
                    .ThenInclude(m => m.Student)
                .Where(g => g.Project!.IsApproved && g.Members.Count < 5)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync();

            ViewBag.PendingRequests = pendingRequests;
            return View(groups);
        }

        [HttpPost]
        public async Task<IActionResult> RequestToJoin(int studentGroupId)
        {
            var user = await _userManager.GetUserAsync(User);

            if (await _context.GroupMembers.AnyAsync(m => m.StudentId == user!.Id))
            {
                TempData["Error"] = "You are already in a group.";
                return RedirectToAction("AvailableGroups");
            }

            var group = await _context.StudentGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == studentGroupId);

            if (group == null) return NotFound();

            if (group.Members.Count >= 5)
            {
                TempData["Error"] = "This group is already full.";
                return RedirectToAction("AvailableGroups");
            }

            var existing = await _context.GroupJoinRequests.AnyAsync(r =>
                r.StudentGroupId == studentGroupId &&
                r.StudentId == user.Id &&
                r.Status == "Pending");

            if (existing)
            {
                TempData["Error"] = "You already requested to join this group.";
                return RedirectToAction("AvailableGroups");
            }

            _context.GroupJoinRequests.Add(new GroupJoinRequest
            {
                StudentGroupId = studentGroupId,
                StudentId = user.Id,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                group.LeaderId,
                "Join Request",
                $"{user.FullName} requested to join '{group.GroupName}'.",
                "/StudentGroups/MyGroup"
            );

            TempData["Success"] = "Join request sent.";
            return RedirectToAction("AvailableGroups");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveJoinRequest(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var request = await _context.GroupJoinRequests
                .Include(r => r.StudentGroup)!
                    .ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(r => r.Id == id &&
                                          r.StudentGroup!.LeaderId == user!.Id &&
                                          r.Status == "Pending");

            if (request == null) return NotFound();

            if (request.StudentGroup!.Members.Count >= 5)
            {
                TempData["Error"] = "This group is already full.";
                return RedirectToAction("MyGroup");
            }

            if (await _context.GroupMembers.AnyAsync(m => m.StudentId == request.StudentId))
            {
                request.Status = "Rejected";
                await _context.SaveChangesAsync();
                TempData["Error"] = "Student is already in another group.";
                return RedirectToAction("MyGroup");
            }

            request.Status = "Approved";

            _context.GroupMembers.Add(new GroupMember
            {
                StudentGroupId = request.StudentGroupId,
                StudentId = request.StudentId,
                IsLeader = false,
                JoinedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                request.StudentId,
                "Join Approved",
                $"You have been added to '{request.StudentGroup.GroupName}'."
            );

            var student = await _userManager.FindByIdAsync(request.StudentId);
            if (student != null && !string.IsNullOrWhiteSpace(student.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        student.Email,
                        "Join Approved",
                        $"You have been added to group '{request.StudentGroup.GroupName}'."
                    );
                }
                catch { }
            }

            TempData["Success"] = "Join request approved.";
            return RedirectToAction("MyGroup");
        }

        [HttpPost]
        public async Task<IActionResult> RejectJoinRequest(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var request = await _context.GroupJoinRequests
                .Include(r => r.StudentGroup)
                .FirstOrDefaultAsync(r => r.Id == id &&
                                          r.StudentGroup!.LeaderId == user!.Id &&
                                          r.Status == "Pending");

            if (request == null) return NotFound();

            request.Status = "Rejected";
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                request.StudentId,
                "Join Rejected",
                $"Your request to join '{request.StudentGroup!.GroupName}' was rejected."
            );

            TempData["Success"] = "Join request rejected.";
            return RedirectToAction("MyGroup");
        }

        [HttpGet]
        public IActionResult Create(int projectId)
        {
            return View(new CreateGroupViewModel { ProjectId = projectId });
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateGroupViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            if (!ModelState.IsValid)
                return View(model);

            var project = await _context.Projects
                .Include(p => p.StudentGroups)
                .FirstOrDefaultAsync(p => p.Id == model.ProjectId && p.IsApproved);

            if (project == null) return NotFound();

            if (await _context.GroupMembers.AnyAsync(m => m.StudentId == user!.Id))
            {
                TempData["Error"] = "You are already in a group and cannot create another group.";
                return RedirectToAction("Approved", "Projects");
            }

            if (project.StudentGroups.Count >= 2)
            {
                TempData["Error"] = "This project already has the maximum of 2 groups.";
                return RedirectToAction("Approved", "Projects");
            }

            var group = new StudentGroup
            {
                ProjectId = model.ProjectId,
                GroupName = model.GroupName,
                LeaderId = user!.Id,
                CreatedAt = DateTime.Now,
                Status = "Open"
            };

            _context.StudentGroups.Add(group);
            await _context.SaveChangesAsync();

            _context.GroupMembers.Add(new GroupMember
            {
                StudentGroupId = group.Id,
                StudentId = user.Id,
                IsLeader = true,
                JoinedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
            TempData["Success"] = "Group created successfully.";
            return RedirectToAction("MyGroup");
        }

        [HttpPost]
        public async Task<IActionResult> Invite(InviteStudentViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            var group = await _context.StudentGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == model.StudentGroupId && g.LeaderId == user!.Id);

            if (group == null) return NotFound();

            if (group.Members.Count >= 5)
            {
                TempData["Error"] = "A group can have maximum 5 students.";
                return RedirectToAction("MyGroup");
            }

            var invitedUser = await _userManager.Users
                .FirstOrDefaultAsync(u => u.StudentNumber == model.StudentNumber.Trim());

            if (invitedUser == null)
            {
                TempData["Error"] = "Student ID not found.";
                return RedirectToAction("MyGroup");
            }

            if (invitedUser.Id == user!.Id)
            {
                TempData["Error"] = "You cannot invite yourself.";
                return RedirectToAction("MyGroup");
            }

            var invitedRoles = await _userManager.GetRolesAsync(invitedUser);
            if (!invitedRoles.Contains("Student"))
            {
                TempData["Error"] = "This user is not a student.";
                return RedirectToAction("MyGroup");
            }

            if (await _context.GroupMembers.AnyAsync(m => m.StudentId == invitedUser.Id))
            {
                TempData["Error"] = "This student is already in a group.";
                return RedirectToAction("MyGroup");
            }

            if (await _context.GroupInvites.AnyAsync(i =>
                i.StudentGroupId == model.StudentGroupId &&
                i.InvitedStudentId == invitedUser.Id &&
                i.Status == "Pending"))
            {
                TempData["Error"] = "This student already has a pending invite.";
                return RedirectToAction("MyGroup");
            }

            _context.GroupInvites.Add(new GroupInvite
            {
                StudentGroupId = model.StudentGroupId,
                InvitedStudentId = invitedUser.Id,
                InvitedByStudentId = user.Id,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                invitedUser.Id,
                "Group Invitation",
                $"{user.FullName} invited you to join group '{group.GroupName}'.",
                "/StudentGroups/Invitations"
            );

            if (!string.IsNullOrWhiteSpace(invitedUser.Email))
            {
                try
                {
                    await _emailService.SendEmailAsync(
                        invitedUser.Email,
                        "Group Invitation - ProjectPath",
                        $"{user.FullName} invited you to join group '{group.GroupName}'."
                    );
                }
                catch { }
            }

            TempData["Success"] = $"Invitation sent to {invitedUser.FullName} ({invitedUser.StudentNumber}).";
            return RedirectToAction("MyGroup");
        }

        [HttpPost]
        public async Task<IActionResult> InviteByStudentNumber(int studentGroupId, string studentNumber)
        {
            return await Invite(new InviteStudentViewModel
            {
                StudentGroupId = studentGroupId,
                StudentNumber = studentNumber
            });
        }

        public async Task<IActionResult> Invitations()
        {
            var user = await _userManager.GetUserAsync(User);

            var invites = await _context.GroupInvites
                .Include(i => i.StudentGroup)!.ThenInclude(g => g.Project)
                .Include(i => i.InvitedByStudent)
                .Where(i => i.InvitedStudentId == user!.Id && i.Status == "Pending")
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return View(invites);
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInvite(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var invite = await _context.GroupInvites
                .Include(i => i.StudentGroup)!.ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(i => i.Id == id &&
                                          i.InvitedStudentId == user!.Id &&
                                          i.Status == "Pending");

            if (invite == null) return NotFound();

            if (await _context.GroupMembers.AnyAsync(m => m.StudentId == user.Id))
            {
                TempData["Error"] = "You are already in a group.";
                return RedirectToAction("Invitations");
            }

            if (invite.StudentGroup!.Members.Count >= 5)
            {
                TempData["Error"] = "This group is already full.";
                return RedirectToAction("Invitations");
            }

            invite.Status = "Accepted";

            _context.GroupMembers.Add(new GroupMember
            {
                StudentGroupId = invite.StudentGroupId,
                StudentId = user.Id,
                IsLeader = false,
                JoinedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                invite.InvitedByStudentId,
                "Invitation Accepted",
                $"{user.FullName} joined your group."
            );

            TempData["Success"] = "Invitation accepted.";
            return RedirectToAction("MyGroup");
        }

        [HttpPost]
        public async Task<IActionResult> RejectInvite(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var invite = await _context.GroupInvites
                .FirstOrDefaultAsync(i => i.Id == id &&
                                          i.InvitedStudentId == user!.Id &&
                                          i.Status == "Pending");

            if (invite == null) return NotFound();

            invite.Status = "Rejected";
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                invite.InvitedByStudentId,
                "Invitation Rejected",
                $"{user.FullName} rejected your invitation."
            );

            TempData["Success"] = "Invitation rejected.";
            return RedirectToAction("Invitations");
        }

        [HttpPost]
        public async Task<IActionResult> TransferLeadership(int studentGroupId, string newLeaderStudentId)
        {
            var user = await _userManager.GetUserAsync(User);

            var group = await _context.StudentGroups
                .Include(g => g.Members)
                .FirstOrDefaultAsync(g => g.Id == studentGroupId && g.LeaderId == user!.Id);

            if (group == null) return NotFound();

            var newLeaderMember = group.Members.FirstOrDefault(m => m.StudentId == newLeaderStudentId);
            var oldLeaderMember = group.Members.FirstOrDefault(m => m.StudentId == user.Id);

            if (newLeaderMember == null || oldLeaderMember == null)
            {
                TempData["Error"] = "Selected student is not a group member.";
                return RedirectToAction("MyGroup");
            }

            oldLeaderMember.IsLeader = false;
            newLeaderMember.IsLeader = true;
            group.LeaderId = newLeaderStudentId;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Leadership transferred successfully.";
            return RedirectToAction("MyGroup");
        }

        [HttpPost]
        public async Task<IActionResult> RequestLeaveGroup()
        {
            var user = await _userManager.GetUserAsync(User);

            var membership = await _context.GroupMembers
                .Include(m => m.StudentGroup)
                .FirstOrDefaultAsync(m => m.StudentId == user!.Id);

            if (membership == null)
            {
                TempData["Error"] = "You are not in a group.";
                return RedirectToAction("MyGroup");
            }

            if (membership.IsLeader)
            {
                TempData["Error"] = "Leader cannot leave the group until leadership is transferred.";
                return RedirectToAction("MyGroup");
            }

            var alreadyPending = await _context.GroupLeaveRequests.AnyAsync(r =>
                r.StudentGroupId == membership.StudentGroupId &&
                r.StudentId == user.Id &&
                r.Status == "Pending");

            if (alreadyPending)
            {
                TempData["Error"] = "You already have a pending leave request.";
                return RedirectToAction("MyGroup");
            }

            _context.GroupLeaveRequests.Add(new GroupLeaveRequest
            {
                StudentGroupId = membership.StudentGroupId,
                StudentId = user.Id,
                Status = "Pending",
                CreatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                membership.StudentGroup!.LeaderId,
                "Leave Request",
                $"{user.FullName} wants to leave the group."
            );

            TempData["Success"] = "Leave request sent to the group leader.";
            return RedirectToAction("MyGroup");
        }

        [HttpPost]
        public async Task<IActionResult> ApproveLeaveRequest(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var request = await _context.GroupLeaveRequests
                .Include(r => r.StudentGroup)
                .FirstOrDefaultAsync(r => r.Id == id &&
                                          r.StudentGroup!.LeaderId == user!.Id &&
                                          r.Status == "Pending");

            if (request == null) return NotFound();

            var member = await _context.GroupMembers
                .FirstOrDefaultAsync(m => m.StudentGroupId == request.StudentGroupId &&
                                          m.StudentId == request.StudentId);

            if (member != null)
            {
                _context.GroupMembers.Remove(member);
            }

            request.Status = "Approved";

            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                request.StudentId,
                "Leave Approved",
                "You have been removed from the group."
            );

            TempData["Success"] = "Leave request approved.";
            return RedirectToAction("MyGroup");
        }

        [HttpPost]
        public async Task<IActionResult> RejectLeaveRequest(int id)
        {
            var user = await _userManager.GetUserAsync(User);

            var request = await _context.GroupLeaveRequests
                .Include(r => r.StudentGroup)
                .FirstOrDefaultAsync(r => r.Id == id &&
                                          r.StudentGroup!.LeaderId == user!.Id &&
                                          r.Status == "Pending");

            if (request == null) return NotFound();

            request.Status = "Rejected";
            await _context.SaveChangesAsync();

            await _notificationService.CreateAsync(
                request.StudentId,
                "Leave Rejected",
                "Your leave request was rejected."
            );

            TempData["Success"] = "Leave request rejected.";
            return RedirectToAction("MyGroup");
        }

        [HttpPost]
        public async Task<IActionResult> LeaveGroup()
        {
            var user = await _userManager.GetUserAsync(User);

            var membership = await _context.GroupMembers
                .Include(m => m.StudentGroup)!.ThenInclude(g => g.Members)
                .FirstOrDefaultAsync(m => m.StudentId == user!.Id);

            if (membership == null)
            {
                TempData["Error"] = "You are not in a group.";
                return RedirectToAction("MyGroup");
            }

            if (membership.IsLeader)
            {
                TempData["Error"] = "Leader cannot leave the group until leadership is transferred.";
                return RedirectToAction("MyGroup");
            }

            TempData["Error"] = "Members cannot leave directly. Please submit a leave request to the leader.";
            return RedirectToAction("MyGroup");
        }
    }
}