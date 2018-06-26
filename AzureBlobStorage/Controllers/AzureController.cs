using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;

namespace AzureBlobStorage.Controllers
{
    public class AzureController : ApiController
    {

        CloudBlobContainer containerRef = null;

        void initStorage()
        {
            string accessKey = "[Azure Blob Key Here]";
            string store = "[StorgeName]";
            string containerName = "[ContainerName]";
            StorageCredentials sc = new StorageCredentials(store, accessKey);
            CloudStorageAccount storageAccount = new CloudStorageAccount(sc, true);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            containerRef = blobClient.GetContainerReference(containerName);
            // create container if it does not exist
            containerRef.CreateIfNotExists();
        }

        [HttpGet]
        [Route("api/GetBlobs")]
        public List<object> GetBlobs()
        {
            initStorage();
            List<IListBlobItem> blobs =  containerRef.ListBlobs(null,false).ToList();

            List<object> theblobs = new List<object>();
            foreach (var blob in blobs)
            {
                CloudBlockBlob blobobject = (CloudBlockBlob)blob;
                theblobs.Add(new {name= blobobject.Name ,uri= blobobject.Uri });
            }
            return theblobs;
        }

        [HttpPost]
        [Route("api/PostBlob")]
        public async Task<HttpResponseMessage> PostBlob()
        {
            // Check if the request contains multipart/form-data.
            if (!Request.Content.IsMimeMultipartContent())
            {
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);
            }

            string root = HttpContext.Current.Server.MapPath("~/App_Data");
            var provider = new MultipartFormDataStreamProvider(root);

            try
            {
                // Read the form data.
                await Request.Content.ReadAsMultipartAsync(provider);

                initStorage();
                // Get all files
                foreach (MultipartFileData file in provider.FileData)
                {
                    
                    string filename = Path.GetFileName(file.Headers.ContentDisposition.FileName.Replace("\"", string.Empty));
                    string uploadFilename = file.LocalFileName;
                    // get a placeholder for blob slot
                    CloudBlockBlob blockBlob = containerRef.GetBlockBlobReference(filename);
                    blockBlob.UploadFromFile(uploadFilename);
                    File.Delete(uploadFilename);
                }
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (System.Exception e)
            {
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, e);
            }
        }
    }
}