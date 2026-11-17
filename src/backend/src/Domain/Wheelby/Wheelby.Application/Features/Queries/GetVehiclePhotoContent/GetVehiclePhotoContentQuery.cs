using Shared.Application.Abstractions;
using Wheelby.Application.DTOs;

namespace Wheelby.Application.Features.GetVehiclePhotoContent;

public sealed record GetVehiclePhotoContentQuery(Guid VehicleId, Guid DocumentId) : IQuery<FileContentDTO>;
