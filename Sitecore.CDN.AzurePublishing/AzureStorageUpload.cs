using System.Configuration;
using log4net;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.SecurityModel;
using Sitecore.Diagnostics;
using Sitecore.Configuration;

namespace Sitecore.CDN.AzurePublishing
{
    public class AzureStorageUpload
    {
        private CloudStorageAccount storageAccount;
        private CloudBlobClient blobClient;
        private CloudBlobContainer container;
        private string AzureSyncPath;

        public ILog Logger = LoggerFactory.GetLogger("Sitecore.Diagnostics.cdnUploading");
        public AzureStorageUpload()
        {
            var accountName = Settings.GetSetting("Azure.AccountName");
            var primaryKey = Settings.GetSetting("Azure.AccountPrimaryKey");
            var containerName = Settings.GetSetting("Azure.ContainerName");
             AzureSyncPath = Settings.GetSetting("Azure.SyncFolder");
            var endpointProtocol = Settings.GetSetting("Azure.EndpointsProtocol");
            string connectionString = string.Format("DefaultEndpointsProtocol={0};AccountName={1};AccountKey={2};", endpointProtocol, accountName, primaryKey);
           
            //use ConfigurationManager to retrieve the connection string
            storageAccount = CloudStorageAccount.Parse(connectionString);

            //create a CloudBlobClient object using the storage account to retrieve objects that represent containers and blobs stored within the Blob Storage Service
            blobClient = storageAccount.CreateCloudBlobClient();

            // Retrieve a reference to a container.
            container = blobClient.GetContainerReference(containerName);
                              
            // Create the container if it doesn't already exist.
            container.CreateIfNotExists();

            //By default, the new container is private and you must specify your storage access key to download blobs from this container. If you want to make the files within the container available to everyone, you can set the container to be public
            container.SetPermissions(
                new BlobContainerPermissions
                {
                    PublicAccess = BlobContainerPublicAccessType.Blob
                });
        }

        public void UploadMediaToAzure(MediaItem mediaItem, string extension = "", string language = "")
        {

            CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetMediaPath(mediaItem, extension));
            blockBlob.DeleteIfExists();

            if (string.IsNullOrEmpty(mediaItem.Extension))
                return;

            if (mediaItem.HasMediaStream("Media"))
            {
                using (var fileStream = (System.IO.FileStream)mediaItem.GetMediaStream())
                {
                    blockBlob.Properties.ContentType = mediaItem.MimeType;
                    blockBlob.UploadFromStream(fileStream);
                    SetCacheControl(blockBlob, "public,max-age=691200");
                }
            }
            else
            {
                blockBlob.DeleteIfExists();
            }

            Item item = mediaItem;
            using (new EditContext(item, SecurityCheck.Disable))
            {
                item["CDN file path"] = GetMediaPath(mediaItem, extension);
                item["Uploaded To Cloud CDN"] = "1";
            }

            Logger.Info(string.Format("CDN File Uploaded : {0}", GetMediaPath(mediaItem, extension)));
        }

        public void DeleteMediaFromAzure(MediaItem mediaItem, string extension = "", string language = "")
        {
           CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetMediaPath(mediaItem, extension));
            blockBlob.DeleteIfExists();

            Logger.Info(string.Format(" CDN File Deleted : {0}", GetMediaPath(mediaItem, extension)));
        }

        public void ReplaceMediaFromAzure(MediaItem mediaItem, string extension = "", string language = "")
        {

            //Delete old files from the blob
            Item item = mediaItem;
            if (!string.IsNullOrEmpty(item["CDN file path"]))
            {
                CloudBlockBlob oldblockBlob = container.GetBlockBlobReference(item["CDN file path"]);
                oldblockBlob.DeleteIfExists();
                //Sitecore.Diagnostics.Log.Info(string.Format(" CDN File Deleted : {0}  ", mediaItem.MediaPath.TrimStart('/').Replace(mediaItem.DisplayName, mediaItem.DisplayName + "." + extension).ToLower()), this.ToString());
                Logger.Info(string.Format(" CDN File Deleted : {0}  ", item["CDN file path"].ToLower()));
            }

            // CloudBlockBlob blockBlob = container.GetBlockBlobReference(mediaItem.MediaPath.TCDN File ReplacedrimStart('/').Replace(mediaItem.DisplayName, mediaItem.ID.ToString().Replace("{", "").Replace("}", "").Replace("-", "")) + "-" + language + "." + extension);
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(GetMediaPath(mediaItem, extension));

            if (string.IsNullOrEmpty(mediaItem.Extension))
                return;
            using (var fileStream = (System.IO.FileStream)mediaItem.GetMediaStream())
            {
                blockBlob.Properties.ContentType = mediaItem.MimeType;
                blockBlob.UploadFromStream(fileStream);
                SetCacheControl(blockBlob, "public,max-age=691200");
            }


            using (new EditContext(item, SecurityCheck.Disable))
            {
                item["CDN file path"] = GetMediaPath(mediaItem, extension);
                item["Uploaded To Cloud CDN"] = "1";

            }

            // Sitecore.Diagnostics.Log.Info(string.Format(" CDN File Uploaded : {0} ", mediaItem.MediaPath.TrimStart('/').Replace(mediaItem.DisplayName, mediaItem.DisplayName + "." + extension).ToLower()), this.ToString());
            Logger.Info(string.Format("CDN File Uploaded : {0}", GetMediaPath(mediaItem, extension)));
        }

        public string GetMediaPath(MediaItem mediaItem, string extension = "")
        {
            string newFileName = Sitecore.MainUtil.EncodeName(mediaItem.Name);

            if (!string.IsNullOrEmpty (AzureSyncPath)) return (AzureSyncPath + "\\" + mediaItem.MediaPath.TrimStart('/').Replace(mediaItem.DisplayName, newFileName + "." + extension).ToLower());
            else return ( mediaItem.MediaPath.TrimStart('/').Replace(mediaItem.DisplayName, newFileName + "." + extension).ToLower());

        }

        public void SetCacheControl(CloudBlob blob, string value)
        {
            blob.Properties.CacheControl = value;
            blob.SetProperties();
        }
    }
}
