using Sitecore;
using Sitecore.Caching;
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.Data.Proxies;
using Sitecore.Data.Templates;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.Links;
using Sitecore.SecurityModel;
using Sitecore.Tasks;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;

namespace Sitecore.Support.Shell.Framework.Pipelines
{
    public class DeleteItems : Sitecore.Shell.Framework.Pipelines.DeleteItems
    {
        public override void Execute(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            List<Item> items = GetItems(args);
            Context.ClientPage.Modified = false;
            try
            {
                DeleteItems.Delete(items);
            }
            catch (Exception ex)
            {
                Log.Error("Error while deleting items", ex, this);
                HttpUnhandledException ex2 = new HttpUnhandledException(ex.Message, ex);
                string htmlErrorMessage = ex2.GetHtmlErrorMessage();
                UrlString urlString = new UrlString("/sitecore/shell/controls/error.htm");
                Context.ClientPage.ClientResponse.ShowModalDialog(new ModalDialogOptions(urlString.ToString())
                {
                    Message = htmlErrorMessage,
                    Header = "Error",
                    MinHeight = "150",
                    MinWidth = "250"
                });
            }
            if (items.Count == 1)
            {
                Context.ClientPage.ClientResponse.Eval("if(this.Content && this.Content.searchWithSameRoot){this.Content.searchWithSameRoot()}");
            }
        }
        private static List<Item> GetItems(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Database database = GetDatabase(args);
            List<Item> list = new List<Item>();
            ListString listString = new ListString(args.Parameters["items"], '|');
            foreach (string item2 in listString)
            {
                Item item = database.GetItem(item2, Language.Parse(args.Parameters["language"]));
                if (item != null)
                {
                    list.Add(MapToRealItem(item));
                }
            }
            return Assert.ResultNotNull(list);
        }
        private static Item MapToRealItem(Item item)
        {
            Assert.ArgumentNotNull(item, "item");
            return ProxyManager.GetRealItem(item, false);
        }
        private static Database GetDatabase(ClientPipelineArgs args)
        {
            Assert.ArgumentNotNull(args, "args");
            Database database = Factory.GetDatabase(args.Parameters["database"]);
            Assert.IsNotNull(database, typeof(Database), "Name: {0}", args.Parameters["database"]);
            return Assert.ResultNotNull(database);
        }

        public new static void Delete(List<Item> items)
        {
            using (new TaskContext("DeleteItems pipeline"))
            {
                foreach (Item item in items)
                {
                    if (Settings.RecycleBinActive)
                    {
                        Log.Audit(typeof(DeleteItems), "Recycle item: {0}", AuditFormatter.FormatItem(item));
                        item.Recycle();
                    }
                    else if (!Settings.RecycleBinActive && BlobShared(item))                                   // 227925 fix start
                    {
                        Log.Audit(typeof(DeleteItems), "Support 227925: Delete item but not blob: {0}", AuditFormatter.FormatItem(item));
                        var myDel = new Sitecore.Support.Data.Items.Item(item.ID, item.InnerData, item.Database);
                        myDel.Delete();
                    }                                                                                          // 227925 fix end
                    else
                    {
                        Log.Audit(typeof(DeleteItems), "Delete item: {0}", AuditFormatter.FormatItem(item));
                        item.Delete();
                    }
                }
            }
        }
        private static bool BlobShared(Item itemToDelete)                                                       // 227925 fix start
        {
            Assert.IsNotNull(itemToDelete, "DeleteItem item is null.");
            Database db = Sitecore.Configuration.Factory.GetDatabase("master");
            Item mediaRoot = db.GetItem("/sitecore/media library");
            string mediaToDelete = Sitecore.Resources.Media.MediaManager.GetMedia(itemToDelete).MediaData.MediaId;
            if (!String.IsNullOrEmpty(mediaToDelete))// If only it's a valid Media
            {
                foreach (Item media in mediaRoot.Axes.GetDescendants())
                {
                    if (Sitecore.Resources.Media.MediaManager.GetMedia(media).MediaData.MediaId
                        .Equals(mediaToDelete))
                    {
                        if (!media.ID.Equals(itemToDelete.ID))
                        {
                            return true; // Returns true when blob is shared, thus deleting item but not blob.
                        }
                    }
                }
            }
            return false;
        }
    }                                                                                                           // 227925 fix end
}