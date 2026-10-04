using AccessControl.Application.Abstractions;
using AccessControl.Contracts;
using AccessControl.Domain.Sessions.Exceptions;
using MediatR;

namespace AccessControl.Application.Features.GetCurrentUser;

internal sealed class GetCurrentUserQueryHandler(
    ICurrentUser currentUser,
    IUserReadStore reader) : IRequestHandler<GetCurrentUserQuery, UserProfileDTO>
{
    public async Task<UserProfileDTO> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
        => await reader.GetProfileAsync(currentUser.Id, cancellationToken)
           ?? throw new InvalidSessionException();
}