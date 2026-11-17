using MediatR;
using SharedKernel.Exceptions;
using Wheelby.Application.Abstractions;
using Wheelby.Application.Common;
using Wheelby.Application.DTOs;
using Wheelby.Domain.Errors;

namespace Wheelby.Application.Features.ListVehicleReviews;

// Reseñas de inquilinos sobre el vehículo, para la ficha. Las del anfitrión
// sobre el inquilino no se publican aquí.
internal sealed class ListVehicleReviewsQueryHandler(IVehicleReadStore vehicles)
    : IRequestHandler<ListVehicleReviewsQuery, PagedResult<ReviewDTO>>
{
    public async Task<PagedResult<ReviewDTO>> Handle(ListVehicleReviewsQuery query, CancellationToken cancellationToken)
    {
        _ = await vehicles.GetByIdAsync(query.VehicleId, cancellationToken)
            ?? throw new NotFoundException("El vehículo no existe.", WheelbyErrorCodes.VehicleNotFound);

        return await vehicles.ListReviewsAsync(query.VehicleId, query.Page, query.PageSize, cancellationToken);
    }
}
