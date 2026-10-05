using AccessControl.Application.Abstractions;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<UserProfileDTO>;
