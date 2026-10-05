using AccessControl.Application.Abstractions;
using AccessControl.Application.Common;
using AccessControl.Application.Features.Login;
using AccessControl.Contracts;
using AccessControl.Domain.Sessions.Exceptions;
using MediatR;
using Shared.Application.Abstractions;

namespace AccessControl.Application.Features.Refresh;

public sealed record RefreshSessionCommand : ICommand<LoginResultDTO>;
