using Autodesk.Revit.DB;

namespace RevitDriveFromOutside.Utils
{
    class CopyWatchAlertSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
        {
            List<FailureMessageAccessor> failures = a.GetFailureMessages()
               .Where(f => f.GetFailureDefinitionId() == BuiltInFailures.CopyMonitorFailures.CopyWatchAlert)
               .ToList();

            failures.ForEach(a.DeleteWarning);

            return FailureProcessingResult.Continue;
        }
    }
}