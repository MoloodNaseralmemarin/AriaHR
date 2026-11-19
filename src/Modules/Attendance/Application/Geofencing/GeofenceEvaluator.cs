namespace AriaHR.Modules.Attendance.Application.Geofencing;

public enum GeofenceOutcome
{
    Inside,
    UncertainInside,
    Outside
}

public readonly record struct GeofenceEvaluation(
    double DistanceInMeters,
    double UncertaintyCreditMeters,
    double EffectiveDistanceInMeters,
    GeofenceOutcome Outcome);

/// <summary>
/// Server-side geofence membership decision.
/// The decision boundary stays the work location's allowed radius; the reported GPS accuracy is only
/// used as a bounded credit for measurement uncertainty, never as a replacement for the radius.
/// </summary>
public static class GeofenceEvaluator
{
    public static GeofenceEvaluation Evaluate(
        double distanceInMeters,
        double gpsAccuracyInMeters,
        double allowedRadiusInMeters,
        double maxAssumedUncertaintyMeters)
    {
        if (double.IsNaN(distanceInMeters) || double.IsInfinity(distanceInMeters) || distanceInMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distanceInMeters), "Distance must be a finite non-negative number.");
        }

        if (double.IsNaN(gpsAccuracyInMeters) || double.IsInfinity(gpsAccuracyInMeters) || gpsAccuracyInMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(gpsAccuracyInMeters), "Accuracy must be a finite non-negative number.");
        }

        if (double.IsNaN(allowedRadiusInMeters) || double.IsInfinity(allowedRadiusInMeters) || allowedRadiusInMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(allowedRadiusInMeters), "Allowed radius must be a finite non-negative number.");
        }

        if (double.IsNaN(maxAssumedUncertaintyMeters) || double.IsInfinity(maxAssumedUncertaintyMeters) || maxAssumedUncertaintyMeters < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxAssumedUncertaintyMeters), "Uncertainty cap must be a finite non-negative number.");
        }

        double uncertaintyCredit = Math.Min(gpsAccuracyInMeters, maxAssumedUncertaintyMeters);
        double effectiveDistance = distanceInMeters - uncertaintyCredit;

        GeofenceOutcome outcome = distanceInMeters <= allowedRadiusInMeters
            ? GeofenceOutcome.Inside
            : effectiveDistance <= allowedRadiusInMeters
                ? GeofenceOutcome.UncertainInside
                : GeofenceOutcome.Outside;

        return new GeofenceEvaluation(distanceInMeters, uncertaintyCredit, effectiveDistance, outcome);
    }
}
