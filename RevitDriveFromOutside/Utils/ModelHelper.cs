using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI;

namespace RevitDriveFromOutside.Utils
{
    public static class ModelHelper
    {
        /// <summary>
        /// Checks whether given view has no visible objects 
        /// </summary>
        /// <param name="element">View</param>
        /// <returns>true if view is empty</returns>
        public static bool IsViewEmpty(this Document doc, Element element)
        {
            View3D view = element as View3D;
            try
            {
                using FilteredElementCollector collector = new(doc, view.Id);
                return !collector.Where(e => e.Category != null
                        && e.GetType() != typeof(RevitLinkInstance))
                    .Any(e => e.CanBeHidden(view));
            }
            catch
            {
                return true;
            }
        }
        /// <summary>
        /// Relinquish ownership of all possible elements in the document
        /// </summary>
        public static void FreeTheModel(this Document doc)
        {
            RelinquishOptions relinquishOptions = new(true);
            TransactWithCentralOptions transactWithCentralOptions = new();
            try
            {
                WorksharingUtils.RelinquishOwnership(doc, relinquishOptions, transactWithCentralOptions);
            }
            catch { }
        }
        /// <summary>
        /// Delete all possible links from the document 
        /// </summary>
        public static void DeleteAllLinks(this Document doc)
        {
            ICollection<ElementId> ids = ExternalFileUtils.GetAllExternalFileReferences(doc);
            if (ids.Count == 0) return;

            using Transaction t = new(doc, "Delete all Links");
            t.Start();
            t.SwallowAlert();

            foreach (ElementId id in ids)
            {
                try
                {
                    doc.Delete(id);
                }
                catch { }
            }
            t.Commit();
        }
        /// <summary>
        /// Open all worksets in a document in a very crippled way
        /// </summary>
        public static void OpenAllWorksets(this Document doc)
        {
            // List of all user worksets
            IList<Workset> collectorWorkset = new FilteredWorksetCollector(doc)
                .OfKind(WorksetKind.UserWorkset)
                .ToWorksets();

            ElementId typeId = new FilteredElementCollector(doc)
                .WhereElementIsElementType()
                .OfClass(typeof(CableTrayType))
                .ToElementIds()
                .FirstOrDefault();

            ElementId levelId = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .ToElementIds()
                .FirstOrDefault();

            if (typeId is null || levelId is null) return;

            using Transaction t = new(doc, "Open Worksets");
            t.Start();

            // Create a temporary cable tray
            CableTray ct = CableTray.Create(doc, typeId, new XYZ(0, 0, 0), new XYZ(0, 0, 1), levelId);

            foreach (Workset workset in collectorWorkset)
            {
                if (workset.IsOpen) continue;

                // Change the workset of the cable tray
                Parameter wsParam = ct.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                if (wsParam is not null && !wsParam.IsReadOnly)
                    wsParam.Set(workset.Id.IntegerValue);

                // Show the cable tray to open the workset
                new UIDocument(doc).ShowElements(ct.Id);
            }

            // Delete the temporary cable tray
            doc.Delete(ct.Id);

            t.Commit();
        }
        /// <summary>
        /// Purge all unused elements in the Document
        /// </summary>
        public static void PurgeAll(this Document doc)
        {
            try
            {
                int previousCount;
                do
                {
                    HashSet<ElementId> unusedElements = doc.GetUnusedElements(new HashSet<ElementId>())
                        .Where(el => doc.GetElement(el) is not null
                            && doc.GetElement(el) is not RevitLinkType)
                        .ToHashSet();

                    previousCount = unusedElements.Count;

                    if (previousCount == 0) break;

                    using Transaction transaction = new(doc, "Purge unused");
                    transaction.Start();
                    doc.Delete(unusedElements);
                    transaction.Commit();
                } while (previousCount > 0);
            }
            catch { }
        }
        public static void RemoveEmptyWorksets(this Document document)
        {
            ICollection<WorksetId> worksets = new FilteredWorksetCollector(document)
                    .OfKind(WorksetKind.UserWorkset)
                    .ToWorksetIds();

            using Transaction transaction = new(document);
            transaction.Start("Remove empty worksets");
            foreach (WorksetId workset in worksets)
            {
                int elements = new FilteredElementCollector(document)
                    .WherePasses(new ElementWorksetFilter(workset))
                    .Count();

                if (elements == 0)
                    WorksetTable.DeleteWorkset(document, workset, new DeleteWorksetSettings());
            }
            transaction.Commit();
        }
    }
}