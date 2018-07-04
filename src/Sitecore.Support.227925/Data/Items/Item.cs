namespace Sitecore.Support.Data.Items
{
    using Sitecore;
    using Sitecore.Caching;
    using Sitecore.Configuration;
    using Sitecore.Data;
    using Sitecore.Data.Managers;
    using Sitecore.Data.Templates;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class Item : Sitecore.Data.Items.Item
    {
        private readonly ID _itemID;
        private ItemData _innerData;
        private Database _database;
        public Item(ID itemID, ItemData data, Database database) : base(itemID, data, database)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull((object)itemID, nameof(itemID));
            Sitecore.Diagnostics.Assert.ArgumentNotNull((object)data, nameof(data));
            Sitecore.Diagnostics.Assert.ArgumentNotNull((object)database, nameof(database));
            this._itemID = itemID;
            this._innerData = data;
            this._database = database;
        }
        public override void Delete()                                                                           // 227925 fix start
        {
            this.Delete(false);
        }                                                                                                       // 227925 fix end
        internal void Delete(bool removeBlobs)
        {
            Database database = this.Database;
            Sitecore.Data.Templates.Template template = TemplateManager.GetTemplate(this);
            List<Guid> guidList = new List<Guid>();
            bool flag1 = removeBlobs && template != null;
            if (flag1)
                guidList = ((IEnumerable<TemplateField>)template.GetFields(true)).Where<TemplateField>((Func<TemplateField, bool>)(f => f.IsBlob)).Select<TemplateField, ID>((Func<TemplateField, ID>)(f => f.ID)).Select<ID, Guid>((Func<ID, Guid>)(id => MainUtil.GetGuid((object)this[id]))).Where<Guid>((Func<Guid, bool>)(guid => guid != Guid.Empty)).ToList<Guid>();
            bool flag2 = ItemManager.DeleteItem(this);
            Item.RemoveItemFromCloningCache(this);
            if (!flag1 || !flag2)
                return;
            foreach (Guid blobId in guidList)
                ItemManager.RemoveBlobStream(blobId, database);
        }
        internal static void RemoveItemFromCloningCache(Item item)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull((object)item, nameof(item));
            Item.ItemCloningRelations.Value.Remove(Item.GetCacheKey(item));
        }
        private static readonly Lazy<ICache> ItemCloningRelations = new Lazy<ICache>((Func<ICache>)(() => CacheManager.GetNamedInstance(nameof(ItemCloningRelations), Settings.Caching.DefaultDataCacheSize, true)));
        private static string GetCacheKey(Item item)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull((object)item, nameof(item));
            return item.ID.ToString() + item.Database.Name;
        }
    }
}