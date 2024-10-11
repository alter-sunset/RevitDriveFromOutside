using Autodesk.Revit.DB;

namespace RevitDriveFromOutside.Utils
{
    class CopyWatchAlertSwallower : IFailuresPreprocessor
    {
        public FailureProcessingResult PreprocessFailures(FailuresAccessor a)
        {
            IEnumerable<FailureMessageAccessor> failures = a.GetFailureMessages()
               .Where(f => f.GetFailureDefinitionId() ==
                   BuiltInFailures.CopyMonitorFailures.CopyWatchAlert);
            foreach (FailureMessageAccessor f in failures)
            {
                a.DeleteWarning(f);
            }
            return FailureProcessingResult.Continue;
        }
    }
}