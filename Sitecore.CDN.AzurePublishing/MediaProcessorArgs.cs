using System.Collections.Generic;
using Sitecore.Data.Items;
using Sitecore.Pipelines;

namespace Sitecore.CDN.AzurePublishing
{
    class MediaProcessorArgs : PipelineArgs
    {
        public IEnumerable<Item> UploadedItems { get; set; }
    }
}
