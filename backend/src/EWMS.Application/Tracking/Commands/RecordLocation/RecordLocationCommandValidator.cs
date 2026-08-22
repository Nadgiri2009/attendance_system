using EWMS.Application.Common.Interfaces;
using EWMS.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EWMS.Application.Tracking.Commands.RecordLocation;

public class RecordLocationCommandValidator : AbstractValidator<RecordLocationCommand>
{
    private readonly IApplicationDbContext _context;

    public RecordLocationCommandValidator(IApplicationDbContext context)
    {
        _context = context;

        RuleFor(x => x.TrackingSessionId).NotEmpty().WithMessage("Tracking session is required.");
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180);

        // "Stop tracking immediately after Check-Out" (the other half of
        // this rule): once a session is Stopped, the server rejects any
        // further location points for it even if a background timer is
        // still firing on a stale client.
        RuleFor(x => x.TrackingSessionId)
            .MustAsync(SessionIsActive)
            .WithMessage("This tracking session is not active. Location points are only accepted while tracking is active.")
            .When(x => x.TrackingSessionId != Guid.Empty);
    }

    private async Task<bool> SessionIsActive(Guid trackingSessionId, CancellationToken cancellationToken) =>
        await _context.TrackingSessions.AnyAsync(
            t => t.Id == trackingSessionId && t.Status == TrackingSessionStatus.Active, cancellationToken);
}
