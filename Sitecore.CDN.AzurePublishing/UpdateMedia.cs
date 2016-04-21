using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.CDN.AzurePublishing;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Pipelines.Attach;
using Sitecore.SecurityModel;

namespace Sitecore.CDN.AzurePublishing
{
  public  class UpdateMedia
    {
        public void Process(AttachArgs args)
        {
            Assert.ArgumentNotNull(args, "args");

            UpdateItem(new List<Item>
            {
                args.MediaItem
            });
        }

        public void UpdateItem(IEnumerable<Item> uploadedItems)
        {
            
            try
            {
                var args = new MediaProcessorArgs
                {
                    UploadedItems = uploadedItems
                };

                foreach (Item file in args.UploadedItems.Where(file => file.Paths.IsMediaItem))
                {
                    using (new EditContext(file, SecurityCheck.Disable))
                    {
                        file["UploadedToCDN"] = string.Empty;

                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message, exception);
            }
        }
    }
}