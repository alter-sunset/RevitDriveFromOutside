﻿using Autodesk.Revit.DB;
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
            using Transaction t = new(doc);
            t.Start("Delete all Links");
            t.SwallowAlert();

            ICollection<ElementId> ids = ExternalFileUtils.GetAllExternalFileReferences(doc);
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
            using TransactionGroup tGroup = new(doc);
            using Transaction t = new(doc);
            tGroup.Start("Open All Worksets");

            //list of all worksets
            FilteredWorksetCollector collectorWorkset =
                new FilteredWorksetCollector(doc).OfKind(WorksetKind.UserWorkset);

            foreach (Workset w in collectorWorkset.ToWorksets())
            {
                if (w.IsOpen)
                    continue;

                t.Start($"Open workset {w.Name}");//Creating temporary cable tray
                ElementId typeID = new FilteredElementCollector(doc)
                    .WhereElementIsElementType()
                    .OfClass(typeof(CableTrayType))
                    .ToElementIds()
                    .First();
                ElementId levelID = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .ToElementIds()
                    .First();
                CableTray ct = CableTray.Create(doc, typeID, new XYZ(0, 0, 0), new XYZ(0, 0, 1), levelID);
                ElementId elementId = ct.Id;

                //Changing workset of cable tray to workset which we want to open
                Parameter wsparam = ct.get_Parameter(BuiltInParameter.ELEM_PARTITION_PARAM);
                if (wsparam != null && !wsparam.IsReadOnly)
                    wsparam.Set(w.Id.IntegerValue);

                //This command will actualy open workset
                new UIDocument(doc).ShowElements(elementId);

                //Delete temporary cable tray
                doc.Delete(elementId);

                t.Commit();
            }
            tGroup.Assimilate();
        }
        /// <summary>
        /// Purge all unused elements in the Document
        /// </summary>
        public static void PurgeAll(this Document doc)
        {
            try
            {
                int num = 0;
                for (; ; )
                {
                    HashSet<ElementId> hashSet = doc.GetUnusedElements(new HashSet<ElementId>())
                        .Where(el => doc.GetElement(el) is not null
                            && doc.GetElement(el) is not RevitLinkType)
                        .ToHashSet();

                    if (hashSet.Count == num || hashSet.Count == 0)
                        break;

                    num = hashSet.Count;
                    using Transaction transaction = new(doc);
                    transaction.Start("Purge unused");
                    doc.Delete(hashSet);
                    transaction.Commit();
                    continue;
                }
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