using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.ListVehiclePhotos;

public sealed record ListVehiclePhotosQuery(Guid VehicleId) : IQuery<IReadOnlyList<VehiclePhotoDTO>>;
