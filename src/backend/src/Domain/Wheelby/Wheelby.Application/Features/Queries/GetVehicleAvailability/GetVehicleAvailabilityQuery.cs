using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.GetVehicleAvailability;

public sealed record GetVehicleAvailabilityQuery(
    Guid VehicleId,
    DateTime From,
    DateTime To) : IQuery<VehicleAvailabilityDTO>;
