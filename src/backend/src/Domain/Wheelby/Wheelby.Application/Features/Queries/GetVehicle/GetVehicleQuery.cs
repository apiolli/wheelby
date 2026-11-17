using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.GetVehicle;

public sealed record GetVehicleQuery(Guid Id) : IQuery<VehicleSummaryDTO>;
