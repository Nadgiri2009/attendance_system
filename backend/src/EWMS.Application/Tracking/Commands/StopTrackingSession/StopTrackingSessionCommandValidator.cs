using FluentValidation;

namespace EWMS.Application.Tracking.Commands.StopTrackingSession;

public class StopTrackingSessionCommandValidator : AbstractValidator<StopTrackingSessionCommand>
{
    public StopTrackingSessionCommandValidator()
    {
        RuleFor(x => x.TrackingSessionId).NotEmpty().WithMessage("Tracking session is required.");
        RuleFor(x => x.EndLatitude).InclusiveBetween(-90, 90).When(x => x.EndLatitude.HasValue);
        RuleFor(x => x.EndLongitude).InclusiveBetween(-180, 180).When(x => x.EndLongitude.HasValue);
    }
}
