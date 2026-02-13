using EntryPointApp.Api.Data.Context;
using EntryPointApp.Api.Models.Dtos.Users;
using EntryPointApp.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EntryPointApp.Api.Services.Admin
{
    public class AdminService(
        ApplicationDbContext context,
        ILogger<AdminService> logger) : IAdminService
    {
        private readonly ApplicationDbContext _context = context;
        private readonly ILogger<AdminService> _logger = logger;

        public async Task<UserListResult> GetAllUsersAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all users");

                var users = await _context.Users
                    .Include(u => u.Manager)
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Email)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role.ToString(),
                        ManagerId = u.ManagerId,
                        ManagerName = u.Manager != null
                            ? $"{u.Manager.FirstName} {u.Manager.LastName}".Trim()
                            : null,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt
                    })
                    .ToListAsync();

                _logger.LogInformation("Successfully retrieved {Count} users", users.Count);

                return new UserListResult
                {
                    Success = true,
                    Message = "Users retrieved successfully!",
                    Data = users
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving users");
                return new UserListResult
                {
                    Success = false,
                    Message = "Failed to retrieve users",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<UserResult> GetUserByIdAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Retrieving user {UserId}", userId);

                var user = await _context.Users
                    .Include(u => u.Manager)
                    .Where(u => u.Id == userId)
                    .Select(u => new UserDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        FirstName = u.FirstName,
                        LastName = u.LastName,
                        Role = u.Role.ToString(),
                        ManagerId = u.ManagerId,
                        ManagerName = u.Manager != null
                            ? $"{u.Manager.FirstName} {u.Manager.LastName}".Trim()
                            : null,
                        IsActive = u.IsActive,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return new UserResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                _logger.LogInformation("Successfully retrieved user {UserId}", userId);

                return new UserResult
                {
                    Success = true,
                    Message = "User retrieved successfully!",
                    Data = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user {UserId}", userId);
                return new UserResult
                {
                    Success = false,
                    Message = "Failed to retrieve user",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<UserResult> UpdateUserRoleAsync(int userId, UserRole newRole)
        {
            try
            {
                _logger.LogInformation("Updating role for user {UserId} to {NewRole}", userId, newRole);

                var user = await _context.Users
                    .Include(u => u.Manager)
                    .Include(u => u.ManagedUsers)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return new UserResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                if (user.Role == newRole)
                {
                    return new UserResult
                    {
                        Success = false,
                        Message = "No change needed",
                        Errors = [$"User already has role {newRole}"]
                    };
                }

                // If demoting from Manager to User, check if they have managed users
                if (user.Role == UserRole.Manager && newRole == UserRole.User)
                {
                    var hasManagedUsers = user.ManagedUsers.Any(u => u.IsActive);
                    if (hasManagedUsers)
                    {
                        return new UserResult
                        {
                            Success = false,
                            Message = "Cannot change role",
                            Errors = ["Cannot demote manager who has active managed users. Reassign their users first."]
                        };
                    }
                    user.IsManager = false;
                }

                if (newRole == UserRole.Manager)
                {
                    user.IsManager = true;
                    user.ManagerId = null;
                }

                if (newRole == UserRole.Admin)
                {
                    user.IsManager = false;
                    user.ManagerId = null;
                }

                if (newRole == UserRole.User)
                {
                    user.IsManager = false;
                }

                user.Role = newRole;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedUser = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    ManagerId = user.ManagerId,
                    ManagerName = user.Manager != null
                        ? $"{user.Manager.FirstName} {user.Manager.LastName}".Trim()
                        : null,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                _logger.LogInformation("Successfully updated user {UserId} role to {NewRole}", userId, newRole);

                return new UserResult
                {
                    Success = true,
                    Message = $"User role updated to {newRole} successfully!",
                    Data = updatedUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role for user {UserId}", userId);
                return new UserResult
                {
                    Success = false,
                    Message = "Failed to update user role",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<UserResult> AssignManagerAsync(int userId, int managerId)
        {
            try
            {
                _logger.LogInformation("Assigning manager {ManagerId} to user {UserId}", managerId, userId);

                if (userId == managerId)
                {
                    return new UserResult
                    {
                        Success = false,
                        Message = "Invalid assignment",
                        Errors = ["A user cannot be their own manager"]
                    };
                }

                var user = await _context.Users
                    .Include(u => u.Manager)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return new UserResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                // Only Users can have managers
                if (user.Role != UserRole.User)
                {
                    return new UserResult
                    {
                        Success = false,
                        Message = "Invalid assignment",
                        Errors = [$"Only users with role 'User' can be assigned a manager. This user has role '{user.Role}'."]
                    };
                }

                var manager = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == managerId);

                if (manager == null)
                {
                    _logger.LogWarning("Manager {ManagerId} not found", managerId);
                    return new UserResult
                    {
                        Success = false,
                        Message = "Manager not found",
                        Errors = ["The specified manager does not exist"]
                    };
                }

                if (manager.Role != UserRole.Manager)
                {
                    return new UserResult
                    {
                        Success = false,
                        Message = "Invalid manager",
                        Errors = ["The specified user is not a manager"]
                    };
                }

                if (!manager.IsActive)
                {
                    return new UserResult
                    {
                        Success = false,
                        Message = "Invalid manager",
                        Errors = ["The specified manager is not active"]
                    };
                }

                user.ManagerId = managerId;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                user = await _context.Users
                    .Include(u => u.Manager)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                var updatedUser = new UserDto
                {
                    Id = user!.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    ManagerId = user.ManagerId,
                    ManagerName = user.Manager != null
                        ? $"{user.Manager.FirstName} {user.Manager.LastName}".Trim()
                        : null,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                _logger.LogInformation("Successfully assigned manager {ManagerId} to user {UserId}", managerId, userId);

                return new UserResult
                {
                    Success = true,
                    Message = "Manager assigned successfully!",
                    Data = updatedUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning manager {ManagerId} to user {UserId}", managerId, userId);
                return new UserResult
                {
                    Success = false,
                    Message = "Failed to assign manager",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<UserResult> RemoveManagerAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Removing manager from user {UserId}", userId);

                var user = await _context.Users
                    .Include(u => u.Manager)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return new UserResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                if (user.ManagerId == null)
                {
                    return new UserResult
                    {
                        Success = false,
                        Message = "No manager assigned",
                        Errors = ["This user does not have a manager to remove"]
                    };
                }

                user.ManagerId = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedUser = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    ManagerId = null,
                    ManagerName = null,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                _logger.LogInformation("Successfully removed manager from user {UserId}", userId);

                return new UserResult
                {
                    Success = true,
                    Message = "Manager removed successfully!",
                    Data = updatedUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing manager from user {UserId}", userId);
                return new UserResult
                {
                    Success = false,
                    Message = "Failed to remove manager",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<UserResult> DeactivateUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Deactivating user {UserId}", userId);

                var user = await _context.Users
                    .Include(u => u.Manager)
                    .Include(u => u.ManagedUsers)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return new UserResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                if (!user.IsActive)
                {
                    return new UserResult
                    {
                        Success = false,
                        Message = "Already inactive",
                        Errors = ["This user is already deactivated"]
                    };
                }

                // Check if manager has active managed users
                if (user.Role == UserRole.Manager || user.IsManager)
                {
                    var hasManagedUsers = user.ManagedUsers.Any(u => u.IsActive);
                    if (hasManagedUsers)
                    {
                        return new UserResult
                        {
                            Success = false,
                            Message = "Cannot deactivate manager",
                            Errors = ["Cannot deactivate manager who has active managed users. Reassign their users first."]
                        };
                    }
                }

                user.IsActive = false;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedUser = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    ManagerId = user.ManagerId,
                    ManagerName = user.Manager != null
                        ? $"{user.Manager.FirstName} {user.Manager.LastName}".Trim()
                        : null,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                _logger.LogInformation("Successfully deactivated user {UserId}", userId);

                return new UserResult
                {
                    Success = true,
                    Message = "User deactivated successfully!",
                    Data = updatedUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating user {UserId}", userId);
                return new UserResult
                {
                    Success = false,
                    Message = "Failed to deactivate user",
                    Errors = [ex.Message]
                };
            }
        }

        public async Task<UserResult> ActivateUserAsync(int userId)
        {
            try
            {
                _logger.LogInformation("Activating user {UserId}", userId);

                var user = await _context.Users
                    .Include(u => u.Manager)
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    _logger.LogWarning("User {UserId} not found", userId);
                    return new UserResult
                    {
                        Success = false,
                        Message = "User not found",
                        Errors = ["The requested user does not exist"]
                    };
                }

                if (user.IsActive)
                {
                    return new UserResult
                    {
                        Success = false,
                        Message = "Already active",
                        Errors = ["This user is already active"]
                    };
                }

                user.IsActive = true;
                user.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var updatedUser = new UserDto
                {
                    Id = user.Id,
                    Email = user.Email,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Role = user.Role.ToString(),
                    ManagerId = user.ManagerId,
                    ManagerName = user.Manager != null
                        ? $"{user.Manager.FirstName} {user.Manager.LastName}".Trim()
                        : null,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt
                };

                _logger.LogInformation("Successfully activated user {UserId}", userId);

                return new UserResult
                {
                    Success = true,
                    Message = "User activated successfully!",
                    Data = updatedUser
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating user {UserId}", userId);
                return new UserResult
                {
                    Success = false,
                    Message = "Failed to activate user",
                    Errors = [ex.Message]
                };
            }
        }
    }
}